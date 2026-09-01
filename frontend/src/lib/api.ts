const API_BASE =
  import.meta.env.VITE_API_URL?.replace(/\/$/, "") ?? "http://127.0.0.1:8787";

const TOKEN_KEY = "gateflow_token";
const USER_KEY = "gateflow_user";

export type AuthUser = {
  id: string;
  email: string;
  fullName: string;
  role: "PlatformAdmin" | "ClientAdmin" | "Guard" | string;
  clientId: string | null;
  siteId: string | null;
  client?: { id: string; name: string; status: string } | null;
};

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUser(): AuthUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function setSession(token: string, user: AuthUser) {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

async function request<T>(path: string, init?: RequestInit & { auth?: boolean }): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(init?.headers as Record<string, string> | undefined),
  };
  if (init?.auth !== false) {
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, { ...init, headers });
  const data = await res.json().catch(() => ({}));
  if (res.status === 401) {
    clearSession();
    throw new Error(data.error ?? "UNAUTHORIZED");
  }
  if (!res.ok && res.status !== 403) {
    throw new Error(data.error ?? `Request failed (${res.status})`);
  }
  if (res.status === 403 && data.decision) {
    return data as T;
  }
  if (res.status === 403) {
    throw new Error(data.error ?? "FORBIDDEN");
  }
  return data as T;
}

export type Site = {
  id: string;
  clientId?: string;
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

export type ClientRow = {
  id: string;
  name: string;
  contactEmail: string | null;
  phone: string | null;
  status: string;
  createdAt: string;
  siteCount: number;
  userCount: number;
};

export const api = {
  health: () => request<{ ok: boolean }>("/health", { auth: false }),
  register: (body: {
    companyName: string;
    fullName: string;
    email: string;
    password: string;
    phone?: string;
  }) =>
    request<{ token: string; user: AuthUser }>("/v1/auth/register", {
      method: "POST",
      body: JSON.stringify(body),
      auth: false,
    }),
  login: (body: { email: string; password: string }) =>
    request<{ token: string; user: AuthUser }>("/v1/auth/login", {
      method: "POST",
      body: JSON.stringify(body),
      auth: false,
    }),
  me: () => request<AuthUser>("/v1/auth/me"),
  listClients: () => request<ClientRow[]>("/v1/admin/clients"),
  setClientStatus: (clientId: string, status: string) =>
    request(`/v1/admin/clients/${clientId}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    }),
  listSites: () => request<Site[]>("/v1/sites"),
  createSite: (body: { name: string; slug: string }) =>
    request("/v1/sites", { method: "POST", body: JSON.stringify(body) }),
  getSite: (id: string) =>
    request<Site & { lanes: Lane[]; units: { id: string; label: string }[] }>(`/v1/sites/${id}`),
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
  checkAccess: (
    body: { siteId?: string; credentialType: string; code: string },
    deviceKey?: string,
  ) =>
    request<AccessResult>("/v1/access/check", {
      method: "POST",
      body: JSON.stringify(body),
      auth: false,
      headers: deviceKey ? { "X-Device-Key": deviceKey } : undefined,
    }),
};

export { API_BASE };
