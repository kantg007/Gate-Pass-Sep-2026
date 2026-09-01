import "dotenv/config";
import { PrismaClient } from "@prisma/client";
import { nanoid } from "nanoid";
import { stringifySettings } from "../src/lib/settings";

const prisma = new PrismaClient();

async function main() {
  await prisma.accessEvent.deleteMany();
  await prisma.accessCredential.deleteMany();
  await prisma.visitorPass.deleteMany();
  await prisma.vehicle.deleteMany();
  await prisma.unit.deleteMany();
  await prisma.lane.deleteMany();
  await prisma.site.deleteMany();

  const site = await prisma.site.create({
    data: {
      name: "Green Valley Society",
      slug: "green-valley",
      settings: stringifySettings({
        allowManualOpen: true,
        visitorDefaultMaxUses: 2,
        features: { rfid: true, qr: true, barcode: true, mockGate: true },
      }),
    },
  });

  const lane = await prisma.lane.create({
    data: {
      siteId: site.id,
      name: "Main Entry",
      direction: "ENTRY",
      deviceApiKey: "dev_demo_lane_key_001",
    },
  });

  const unitA = await prisma.unit.create({
    data: { siteId: site.id, label: "A-101", block: "A", floor: "1" },
  });
  const unitB = await prisma.unit.create({
    data: { siteId: site.id, label: "B-204", block: "B", floor: "2" },
  });

  const car1 = await prisma.vehicle.create({
    data: {
      siteId: site.id,
      unitId: unitA.id,
      plateNumber: "MH12AB1234",
      label: "Owner car",
    },
  });
  const car2 = await prisma.vehicle.create({
    data: {
      siteId: site.id,
      unitId: unitB.id,
      plateNumber: "MH14CD5678",
      label: "Second car",
    },
  });

  await prisma.accessCredential.create({
    data: {
      siteId: site.id,
      type: "RFID",
      code: "RFID-1001",
      vehicleId: car1.id,
    },
  });
  await prisma.accessCredential.create({
    data: {
      siteId: site.id,
      type: "BARCODE",
      code: "BC-7788",
      vehicleId: car2.id,
    },
  });

  const validUntil = new Date(Date.now() + 24 * 60 * 60 * 1000);
  const pass = await prisma.visitorPass.create({
    data: {
      siteId: site.id,
      unitId: unitA.id,
      guestName: "Ravi Guest",
      purpose: "Family visit",
      maxUses: 2,
      validUntil,
    },
  });
  const qr = "VIS-DEMO-001";
  await prisma.accessCredential.create({
    data: {
      siteId: site.id,
      type: "QR",
      code: qr,
      visitorPassId: pass.id,
      expiresAt: validUntil,
    },
  });

  console.log("Seed complete");
  console.log({
    siteId: site.id,
    laneId: lane.id,
    deviceApiKey: lane.deviceApiKey,
    demoRfid: "RFID-1001",
    demoBarcode: "BC-7788",
    demoVisitorQr: qr,
  });
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
