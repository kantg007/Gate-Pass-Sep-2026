export type SiteSettings = {
  allowManualOpen: boolean;
  visitorDefaultMaxUses: number;
  visitorDefaultValidHours: number;
  requireActiveVehicle: boolean;
  denyExpiredCredentials: boolean;
  logDeniedAttempts: boolean;
  /** Free-form feature flags for future without migrations */
  features: Record<string, boolean>;
};

export const DEFAULT_SITE_SETTINGS: SiteSettings = {
  allowManualOpen: true,
  visitorDefaultMaxUses: 2,
  visitorDefaultValidHours: 24,
  requireActiveVehicle: true,
  denyExpiredCredentials: true,
  logDeniedAttempts: true,
  features: {
    rfid: true,
    qr: true,
    barcode: true,
    mockGate: true,
  },
};

export function parseSettings(raw: string | null | undefined): SiteSettings {
  try {
    const parsed = raw ? JSON.parse(raw) : {};
    return {
      ...DEFAULT_SITE_SETTINGS,
      ...parsed,
      features: {
        ...DEFAULT_SITE_SETTINGS.features,
        ...(parsed.features ?? {}),
      },
    };
  } catch {
    return { ...DEFAULT_SITE_SETTINGS };
  }
}

export function stringifySettings(settings: Partial<SiteSettings>): string {
  return JSON.stringify({ ...DEFAULT_SITE_SETTINGS, ...settings });
}
