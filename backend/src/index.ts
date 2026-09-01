import "dotenv/config";
import { serve } from "@hono/node-server";
import { Hono } from "hono";
import { cors } from "hono/cors";
import { z } from "zod";
import { nanoid } from "nanoid";
import { prisma } from "./lib/prisma.js";
import { parseSettings, stringifySettings } from "./lib/settings.js";
import { checkAccess } from "./services/access.js";

const app = new Hono();

const corsOrigins = (process.env.CORS_ORIGIN ?? "http://127.0.0.1:5173")
  .split(",")
  .map((s) => s.trim());

app.use(
  "*",
  cors({
    origin: corsOrigins,
    allowHeaders: ["Content-Type", "X-Device-Key"],
    allowMethods: ["GET", "POST", "PATCH", "DELETE", "OPTIONS"],
  }),
);

app.get("/health", (c) => c.json({ ok: true, service: "gateflow-api" }));

/** Core IoT + UI access decision */
app.post("/v1/access/check", async (c) => {
  const body = await c.req.json().catch(() => ({}));
  const schema = z.object({
    siteId: z.string().optional(),
    laneId: z.string().optional(),
    credentialType: z.string().min(1),
    code: z.string().min(1),
    meta: z.record(z.string(), z.unknown()).optional(),
  });
  const parsed = schema.safeParse(body);
  if (!parsed.success) {
    return c.json({ error: "INVALID_BODY", details: parsed.error.flatten() }, 400);
  }

  const deviceApiKey = c.req.header("X-Device-Key") ?? undefined;
  const result = await checkAccess({
    ...parsed.data,
    deviceApiKey,
  });

  return c.json(result, result.open ? 200 : 403);
});

app.get("/v1/sites", async (c) => {
  const sites = await prisma.site.findMany({
    orderBy: { createdAt: "asc" },
    include: {
      _count: { select: { vehicles: true, lanes: true, events: true } },
    },
  });
  return c.json(
    sites.map((s) => ({
      ...s,
      settings: parseSettings(s.settings),
    })),
  );
});

app.post("/v1/sites", async (c) => {
  const body = await c.req.json();
  const schema = z.object({
    name: z.string().min(2),
    slug: z.string().min(2).regex(/^[a-z0-9-]+$/),
    settings: z.record(z.string(), z.unknown()).optional(),
  });
  const parsed = schema.safeParse(body);
  if (!parsed.success) {
    return c.json({ error: "INVALID_BODY", details: parsed.error.flatten() }, 400);
  }

  const site = await prisma.site.create({
    data: {
      name: parsed.data.name,
      slug: parsed.data.slug,
      settings: stringifySettings(parsed.data.settings as never),
    },
  });

  const lane = await prisma.lane.create({
    data: {
      siteId: site.id,
      name: "Main Entry",
      direction: "ENTRY",
      deviceApiKey: `dev_${nanoid(16)}`,
    },
  });

  return c.json({ site: { ...site, settings: parseSettings(site.settings) }, lane }, 201);
});

app.get("/v1/sites/:siteId", async (c) => {
  const site = await prisma.site.findUnique({
    where: { id: c.req.param("siteId") },
    include: { lanes: true, units: true },
  });
  if (!site) return c.json({ error: "NOT_FOUND" }, 404);
  return c.json({ ...site, settings: parseSettings(site.settings) });
});

app.patch("/v1/sites/:siteId/settings", async (c) => {
  const body = await c.req.json();
  const site = await prisma.site.findUnique({ where: { id: c.req.param("siteId") } });
  if (!site) return c.json({ error: "NOT_FOUND" }, 404);
  const current = parseSettings(site.settings);
  const merged = {
    ...current,
    ...body,
    features: { ...current.features, ...(body.features ?? {}) },
  };
  const updated = await prisma.site.update({
    where: { id: site.id },
    data: { settings: JSON.stringify(merged) },
  });
  return c.json({ ...updated, settings: parseSettings(updated.settings) });
});

app.get("/v1/sites/:siteId/vehicles", async (c) => {
  const vehicles = await prisma.vehicle.findMany({
    where: { siteId: c.req.param("siteId") },
    include: { unit: true, credentials: true },
    orderBy: { plateNumber: "asc" },
  });
  return c.json(vehicles);
});

app.post("/v1/sites/:siteId/vehicles", async (c) => {
  const siteId = c.req.param("siteId");
  const body = await c.req.json();
  const schema = z.object({
    plateNumber: z.string().min(3),
    label: z.string().optional(),
    unitId: z.string().optional().nullable(),
    rfidCode: z.string().optional(),
    barcodeCode: z.string().optional(),
  });
  const parsed = schema.safeParse(body);
  if (!parsed.success) {
    return c.json({ error: "INVALID_BODY", details: parsed.error.flatten() }, 400);
  }

  const vehicle = await prisma.vehicle.create({
    data: {
      siteId,
      plateNumber: parsed.data.plateNumber.toUpperCase(),
      label: parsed.data.label,
      unitId: parsed.data.unitId || null,
    },
  });

  const creds = [];
  if (parsed.data.rfidCode) {
    creds.push(
      await prisma.accessCredential.create({
        data: {
          siteId,
          type: "RFID",
          code: parsed.data.rfidCode.trim(),
          vehicleId: vehicle.id,
        },
      }),
    );
  }
  if (parsed.data.barcodeCode) {
    creds.push(
      await prisma.accessCredential.create({
        data: {
          siteId,
          type: "BARCODE",
          code: parsed.data.barcodeCode.trim(),
          vehicleId: vehicle.id,
        },
      }),
    );
  }

  return c.json({ vehicle, credentials: creds }, 201);
});

app.post("/v1/sites/:siteId/units", async (c) => {
  const body = await c.req.json();
  const schema = z.object({
    label: z.string().min(1),
    block: z.string().optional(),
    floor: z.string().optional(),
  });
  const parsed = schema.safeParse(body);
  if (!parsed.success) {
    return c.json({ error: "INVALID_BODY", details: parsed.error.flatten() }, 400);
  }
  const unit = await prisma.unit.create({
    data: { siteId: c.req.param("siteId"), ...parsed.data },
  });
  return c.json(unit, 201);
});

app.post("/v1/sites/:siteId/visitors", async (c) => {
  const siteId = c.req.param("siteId");
  const site = await prisma.site.findUnique({ where: { id: siteId } });
  if (!site) return c.json({ error: "NOT_FOUND" }, 404);
  const settings = parseSettings(site.settings);

  const body = await c.req.json();
  const schema = z.object({
    guestName: z.string().min(2),
    unitId: z.string().optional().nullable(),
    purpose: z.string().optional(),
    maxUses: z.number().int().positive().optional(),
    validHours: z.number().positive().optional(),
  });
  const parsed = schema.safeParse(body);
  if (!parsed.success) {
    return c.json({ error: "INVALID_BODY", details: parsed.error.flatten() }, 400);
  }

  const hours = parsed.data.validHours ?? settings.visitorDefaultValidHours;
  const maxUses = parsed.data.maxUses ?? settings.visitorDefaultMaxUses;
  const validUntil = new Date(Date.now() + hours * 60 * 60 * 1000);
  const qrCode = `VIS-${nanoid(12)}`;

  const pass = await prisma.visitorPass.create({
    data: {
      siteId,
      guestName: parsed.data.guestName,
      unitId: parsed.data.unitId || null,
      purpose: parsed.data.purpose,
      maxUses,
      validUntil,
    },
  });

  const credential = await prisma.accessCredential.create({
    data: {
      siteId,
      type: "QR",
      code: qrCode,
      visitorPassId: pass.id,
      expiresAt: validUntil,
    },
  });

  return c.json(
    {
      visitorPass: pass,
      credential,
      /** Guard / mock gate can paste this code; later encode as QR image */
      qrPayload: qrCode,
    },
    201,
  );
});

app.get("/v1/sites/:siteId/events", async (c) => {
  const limit = Number(c.req.query("limit") ?? 50);
  const events = await prisma.accessEvent.findMany({
    where: { siteId: c.req.param("siteId") },
    orderBy: { createdAt: "desc" },
    take: Math.min(limit, 200),
    include: { lane: true },
  });
  return c.json(
    events.map((e) => ({
      ...e,
      meta: safeJson(e.meta),
    })),
  );
});

app.get("/v1/sites/:siteId/lanes", async (c) => {
  const lanes = await prisma.lane.findMany({
    where: { siteId: c.req.param("siteId") },
  });
  return c.json(
    lanes.map((l) => ({ ...l, config: safeJson(l.config) })),
  );
});

function safeJson(raw: string) {
  try {
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

const port = Number(process.env.PORT ?? 8787);
console.log(`GateFlow API listening on http://127.0.0.1:${port}`);
serve({ fetch: app.fetch, port, hostname: "0.0.0.0" });
