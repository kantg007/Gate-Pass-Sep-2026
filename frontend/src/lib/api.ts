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

function qs(params: Record<string, string | number | undefined | null>) {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== "") sp.set(k, String(v));
  }
  const s = sp.toString();
  return s ? `?${s}` : "";
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

export type DashboardOverview = {
  siteId: string | null;
  clientId: string | null;
  kpis: {
    key: string;
    label: string;
    value: number;
    changePct: number | null;
    compareLabel: string;
  }[];
  vehicleMovement: {
    hourLabel: string;
    hourStart: string;
    entered: number;
    exited: number;
    inside: number;
  }[];
  gateStatus: { open: number; closed: number; offline: number; total: number };
  liveGates: {
    id: string;
    name: string;
    code: string;
    siteId: string;
    siteName: string;
    direction: string;
    status: string;
    barrierState: string;
    deviceOnline: boolean;
    lastSeenAt: string | null;
  }[];
  recentActivity: {
    id: string;
    kind: string;
    title: string;
    detail: string | null;
    plateNumber: string | null;
    siteName: string | null;
    gateName: string | null;
    decision: string;
    eventType: string;
    createdAt: string;
  }[];
  topSites: {
    siteId: string;
    siteName: string;
    entries: number;
    changePct: number | null;
  }[];
  deviceHealth: {
    total: number;
    healthy: number;
    warning: number;
    offline: number;
    healthyPct: number;
  };
  openAlerts: number;
};

export type GateRow = {
  id: string;
  siteId: string;
  siteName: string;
  clientId: string | null;
  name: string;
  code: string;
  direction: string;
  barrierState: string;
  status: string;
  isActive: boolean;
  deviceOnline: boolean;
  lastSeenAt: string | null;
  createdAt: string;
};

export type HardwareRow = {
  id: string;
  clientId: string;
  siteId: string;
  siteName: string;
  gateId: string | null;
  gateName: string | null;
  name: string;
  deviceType: string;
  serialNumber: string | null;
  deviceApiKey: string;
  firmwareVersion: string | null;
  connectionStatus: string;
  lastSeenAt: string | null;
  isActive: boolean;
};

export type AlertRow = {
  id: string;
  clientId: string | null;
  siteId: string | null;
  siteName: string | null;
  gateId: string | null;
  deviceId: string | null;
  severity: string;
  type: string;
  title: string;
  message: string;
  status: string;
  createdAt: string;
  acknowledgedAt: string | null;
  resolvedAt: string | null;
};

export type UserRow = {
  id: string;
  email: string;
  fullName: string;
  role: string;
  clientId: string | null;
  siteId: string | null;
  siteName: string | null;
  phone: string | null;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
};

export type RoleRow = {
  id: string;
  clientId: string | null;
  name: string;
  code: string;
  description: string | null;
  isSystem: boolean;
  isActive: boolean;
  permissionCount: number;
  userCount: number;
};

export type SearchHit = {
  type: string;
  id: string;
  title: string;
  subtitle: string | null;
  href: string | null;
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

  dashboardOverview: (params?: { siteId?: string; clientId?: string }) =>
    request<DashboardOverview>(`/v1/dashboard/overview${qs(params ?? {})}`),

  listGates: (params?: { siteId?: string }) =>
    request<GateRow[]>(`/v1/gates${qs(params ?? {})}`),
  createGate: (body: { siteId: string; name: string; code: string; direction: string }) =>
    request<GateRow>("/v1/gates", { method: "POST", body: JSON.stringify(body) }),
  gateCommand: (
    gateId: string,
    body: { command: string; reasonCode?: string; reasonNote?: string; method?: string },
  ) =>
    request(`/v1/gates/${gateId}/commands`, {
      method: "POST",
      body: JSON.stringify(body),
    }),

  listHardware: (params?: { siteId?: string }) =>
    request<HardwareRow[]>(`/v1/hardware${qs(params ?? {})}`),
  createHardware: (body: {
    siteId: string;
    gateId?: string;
    name: string;
    deviceType: string;
    serialNumber?: string;
  }) => request<HardwareRow>("/v1/hardware", { method: "POST", body: JSON.stringify(body) }),

  listAlerts: (params?: { siteId?: string; status?: string }) =>
    request<{ items: AlertRow[]; openCount: number }>(`/v1/alerts${qs(params ?? {})}`),
  updateAlert: (alertId: string, status: string) =>
    request<AlertRow>(`/v1/alerts/${alertId}`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    }),

  listUsers: (params?: { siteId?: string }) =>
    request<UserRow[]>(`/v1/users${qs(params ?? {})}`),
  createUser: (body: {
    email: string;
    fullName: string;
    password: string;
    role: string;
    siteId?: string;
    phone?: string;
  }) => request<UserRow>("/v1/users", { method: "POST", body: JSON.stringify(body) }),
  updateUser: (
    userId: string,
    body: { fullName?: string; role?: string; siteId?: string; phone?: string; isActive?: boolean },
  ) =>
    request<UserRow>(`/v1/users/${userId}`, {
      method: "PATCH",
      body: JSON.stringify(body),
    }),
  listRoles: () => request<RoleRow[]>("/v1/roles"),

  search: (q: string) => request<{ query: string; hits: SearchHit[] }>(`/v1/search${qs({ q })}`),

  accessSummary: (params?: { siteId?: string; from?: string; to?: string }) =>
    request<{ from: string; to: string; rows: { siteId: string; gateId: string | null; eventType: string; count: number }[] }>(
      `/v1/reports/access-summary${qs(params ?? {})}`,
    ),
  topSites: () =>
    request<{ from: string; to: string; sites: { siteId: string; siteName: string; entries: number; exits: number; changePct: number | null }[] }>(
      "/v1/reports/top-sites",
    ),
};

export { API_BASE };
