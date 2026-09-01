const API_BASE =
  import.meta.env.VITE_API_URL?.replace(/\/$/, "") ?? "http://127.0.0.1:8787";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok && res.status !== 403) {
    throw new Error(data.error ?? `Request failed (${res.status})`);
  }
  return data as T;
}

export type Site = {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  settings: Record<string, unknown>;
  _count?: { vehicles: number; lanes: number; events: number };
};

export type Vehicle = {
  id: string;
  plateNumber: string;
  label: string | null;
  isActive: boolean;
  unit: { label: string } | null;
  credentials: { id: string; type: string; code: string }[];
};

export type AccessEvent = {
  id: string;
  decision: string;
  reason: string;
  credentialType: string | null;
  credentialCode: string | null;
  plateNumber: string | null;
  createdAt: string;
  lane: { name: string } | null;
  meta: Record<string, unknown>;
};

export type AccessResult = {
  open: boolean;
  decision: "ALLOW" | "DENY";
  reason: string;
  plateNumber: string | null;
  guestName: string | null;
  eventId: string;
};

export type Lane = {
  id: string;
  name: string;
  direction: string;
  deviceApiKey: string;
  isActive: boolean;
};

export const api = {
  health: () => request<{ ok: boolean }>("/health"),
  listSites: () => request<Site[]>("/v1/sites"),
  getSite: (id: string) => request<Site & { lanes: Lane[]; units: { id: string; label: string }[] }>(`/v1/sites/${id}`),
  listVehicles: (siteId: string) => request<Vehicle[]>(`/v1/sites/${siteId}/vehicles`),
  listEvents: (siteId: string) => request<AccessEvent[]>(`/v1/sites/${siteId}/events?limit=40`),
  listLanes: (siteId: string) => request<Lane[]>(`/v1/sites/${siteId}/lanes`),
  createVehicle: (
    siteId: string,
    body: {
      plateNumber: string;
      label?: string;
      unitId?: string;
      rfidCode?: string;
      barcodeCode?: string;
    },
  ) =>
    request(`/v1/sites/${siteId}/vehicles`, {
      method: "POST",
      body: JSON.stringify(body),
    }),
  createVisitor: (
    siteId: string,
    body: { guestName: string; unitId?: string; purpose?: string },
  ) =>
    request<{ qrPayload: string; visitorPass: { guestName: string; maxUses: number; validUntil: string } }>(
      `/v1/sites/${siteId}/visitors`,
      { method: "POST", body: JSON.stringify(body) },
    ),
  checkAccess: (body: {
    siteId: string;
    credentialType: string;
    code: string;
  }, deviceKey?: string) =>
    request<AccessResult>("/v1/access/check", {
      method: "POST",
      body: JSON.stringify(body),
      headers: deviceKey ? { "X-Device-Key": deviceKey } : undefined,
    }),
};

export { API_BASE };
