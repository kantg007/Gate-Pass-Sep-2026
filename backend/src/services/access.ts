import { prisma } from "../lib/prisma.js";
import { parseSettings } from "../lib/settings.js";

export type AccessCheckInput = {
  siteId?: string;
  laneId?: string;
  deviceApiKey?: string;
  /** RFID | QR | BARCODE | MANUAL */
  credentialType: string;
  code: string;
  meta?: Record<string, unknown>;
};

export type AccessCheckResult = {
  open: boolean;
  decision: "ALLOW" | "DENY";
  reason: string;
  siteId: string;
  laneId: string | null;
  plateNumber: string | null;
  vehicleId: string | null;
  guestName: string | null;
  eventId: string;
};

export async function checkAccess(
  input: AccessCheckInput,
): Promise<AccessCheckResult> {
  let siteId = input.siteId;
  let laneId = input.laneId ?? null;

  if (input.deviceApiKey) {
    const lane = await prisma.lane.findUnique({
      where: { deviceApiKey: input.deviceApiKey },
      include: { site: true },
    });
    if (!lane || !lane.isActive || !lane.site.isActive) {
      return denyWithoutSite("UNKNOWN_OR_INACTIVE_DEVICE", input);
    }
    siteId = lane.siteId;
    laneId = lane.id;
  }

  if (!siteId) {
    return denyWithoutSite("SITE_REQUIRED", input);
  }

  const site = await prisma.site.findUnique({ where: { id: siteId } });
  if (!site || !site.isActive) {
    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "DENY",
      reason: "SITE_INACTIVE",
    });
  }

  const settings = parseSettings(site.settings);
  const type = input.credentialType.toUpperCase();
  const code = input.code.trim();

  if (!code) {
    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "DENY",
      reason: "EMPTY_CODE",
    });
  }

  if (type === "MANUAL") {
    if (!settings.allowManualOpen) {
      return persistEvent({
        siteId,
        laneId,
        input,
        decision: "DENY",
        reason: "MANUAL_OPEN_DISABLED",
      });
    }
    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "ALLOW",
      reason: "MANUAL_OPEN",
      metaExtra: { note: code },
    });
  }

  if (settings.features[type.toLowerCase()] === false) {
    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "DENY",
      reason: `TYPE_DISABLED:${type}`,
    });
  }

  const credential = await prisma.accessCredential.findFirst({
    where: {
      siteId,
      code,
      type: { equals: type },
    },
    include: {
      vehicle: true,
      visitorPass: { include: { unit: true } },
    },
  });

  if (!credential || !credential.isActive) {
    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "DENY",
      reason: "UNKNOWN_CREDENTIAL",
    });
  }

  if (
    settings.denyExpiredCredentials &&
    credential.expiresAt &&
    credential.expiresAt.getTime() < Date.now()
  ) {
    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "DENY",
      reason: "CREDENTIAL_EXPIRED",
      vehicleId: credential.vehicleId,
      plateNumber: credential.vehicle?.plateNumber ?? null,
    });
  }

  if (credential.vehicleId && credential.vehicle) {
    if (settings.requireActiveVehicle && !credential.vehicle.isActive) {
      return persistEvent({
        siteId,
        laneId,
        input,
        decision: "DENY",
        reason: "VEHICLE_INACTIVE",
        vehicleId: credential.vehicleId,
        plateNumber: credential.vehicle.plateNumber,
      });
    }
    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "ALLOW",
      reason: "RESIDENT_VEHICLE",
      vehicleId: credential.vehicleId,
      plateNumber: credential.vehicle.plateNumber,
    });
  }

  if (credential.visitorPass) {
    const pass = credential.visitorPass;
    const now = Date.now();
    if (!pass.isActive) {
      return persistEvent({
        siteId,
        laneId,
        input,
        decision: "DENY",
        reason: "VISITOR_INACTIVE",
        guestName: pass.guestName,
      });
    }
    if (pass.validFrom.getTime() > now || pass.validUntil.getTime() < now) {
      return persistEvent({
        siteId,
        laneId,
        input,
        decision: "DENY",
        reason: "VISITOR_OUTSIDE_WINDOW",
        guestName: pass.guestName,
      });
    }
    if (pass.usedCount >= pass.maxUses) {
      return persistEvent({
        siteId,
        laneId,
        input,
        decision: "DENY",
        reason: "VISITOR_MAX_USES",
        guestName: pass.guestName,
      });
    }

    await prisma.visitorPass.update({
      where: { id: pass.id },
      data: { usedCount: { increment: 1 } },
    });

    return persistEvent({
      siteId,
      laneId,
      input,
      decision: "ALLOW",
      reason: "VISITOR_PASS",
      guestName: pass.guestName,
      metaExtra: {
        unitLabel: pass.unit?.label,
        usesLeft: pass.maxUses - pass.usedCount - 1,
      },
    });
  }

  return persistEvent({
    siteId,
    laneId,
    input,
    decision: "DENY",
    reason: "CREDENTIAL_NOT_LINKED",
  });
}

async function denyWithoutSite(
  reason: string,
  input: AccessCheckInput,
): Promise<AccessCheckResult> {
  return {
    open: false,
    decision: "DENY",
    reason,
    siteId: input.siteId ?? "",
    laneId: null,
    plateNumber: null,
    vehicleId: null,
    guestName: null,
    eventId: "",
  };
}

async function persistEvent(args: {
  siteId: string;
  laneId: string | null;
  input: AccessCheckInput;
  decision: "ALLOW" | "DENY";
  reason: string;
  vehicleId?: string | null;
  plateNumber?: string | null;
  guestName?: string | null;
  metaExtra?: Record<string, unknown>;
}): Promise<AccessCheckResult> {
  const site = await prisma.site.findUnique({ where: { id: args.siteId } });
  const settings = parseSettings(site?.settings);
  const shouldLog =
    args.decision === "ALLOW" || settings.logDeniedAttempts;

  let eventId = "";
  if (shouldLog && args.siteId) {
    const event = await prisma.accessEvent.create({
      data: {
        siteId: args.siteId,
        laneId: args.laneId,
        credentialType: args.input.credentialType.toUpperCase(),
        credentialCode: args.input.code,
        decision: args.decision,
        reason: args.reason,
        vehicleId: args.vehicleId ?? null,
        plateNumber: args.plateNumber ?? null,
        meta: JSON.stringify({
          ...(args.input.meta ?? {}),
          ...(args.metaExtra ?? {}),
          guestName: args.guestName ?? undefined,
        }),
      },
    });
    eventId = event.id;
  }

  return {
    open: args.decision === "ALLOW",
    decision: args.decision,
    reason: args.reason,
    siteId: args.siteId,
    laneId: args.laneId,
    plateNumber: args.plateNumber ?? null,
    vehicleId: args.vehicleId ?? null,
    guestName: args.guestName ?? null,
    eventId,
  };
}
