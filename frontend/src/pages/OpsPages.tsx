import { useEffect, useState, type FormEvent } from "react";
import { api, type AlertRow, type GateRow, type HardwareRow, type RoleRow, type UserRow } from "../lib/api";
import { useSiteFilter } from "../lib/SiteFilterContext";
import { useAuth } from "../auth/AuthContext";

function PageHeader({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <div>
      <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
      <p className="text-sm text-[var(--muted)]">{subtitle}</p>
    </div>
  );
}

function statusPill(status: string) {
  const s = status.toLowerCase();
  const cls = s === "open" || s === "online" || s === "active" || s === "healthy"
    ? "open"
    : s === "closed" || s === "acknowledged"
      ? "closed"
      : s.includes("warn") || s === "degraded"
        ? "warning"
        : "offline";
  return <span className={`gp-status-pill ${cls}`}>{status}</span>;
}

export function GatesPage() {
  const { siteId, sites } = useSiteFilter();
  const [rows, setRows] = useState<GateRow[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  async function load() {
    try {
      setRows(await api.listGates(siteId ? { siteId } : undefined));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed");
    }
  }

  useEffect(() => {
    void load();
  }, [siteId]);

  async function command(gateId: string, command: "OPEN" | "CLOSE") {
    setBusyId(gateId);
    try {
      await api.gateCommand(gateId, { command, reasonCode: "REMOTE", reasonNote: "Dashboard action" });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Command failed");
    } finally {
      setBusyId(null);
    }
  }

  async function onCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    try {
      await api.createGate({
        siteId: String(fd.get("siteId")),
        name: String(fd.get("name")),
        code: String(fd.get("code")),
        direction: String(fd.get("direction")),
      });
      e.currentTarget.reset();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Gates" subtitle="Barrier lanes — open, close, and monitor status" />
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}

      <form onSubmit={onCreate} className="gp-card grid gap-3 p-4 md:grid-cols-5">
        <select name="siteId" required defaultValue={siteId || sites[0]?.id || ""} className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2">
          {sites.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <input name="name" required placeholder="Gate name" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
        <input name="code" required placeholder="CODE" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
        <select name="direction" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2">
          <option value="ENTRY">ENTRY</option>
          <option value="EXIT">EXIT</option>
          <option value="BOTH">BOTH</option>
        </select>
        <button className="rounded-xl bg-[var(--accent)] px-4 py-2 font-semibold text-white">Add gate</button>
      </form>

      <div className="gp-card overflow-x-auto">
        <table className="w-full min-w-[760px] text-left text-sm">
          <thead className="bg-[var(--panel-2)] text-[var(--muted)]">
            <tr>
              <th className="px-4 py-3 font-medium">Gate</th>
              <th className="font-medium">Site</th>
              <th className="font-medium">Direction</th>
              <th className="font-medium">Status</th>
              <th className="font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((g) => (
              <tr key={g.id} className="border-t border-[var(--line)]">
                <td className="px-4 py-3">
                  <div className="font-semibold">{g.name}</div>
                  <div className="text-xs text-[var(--muted)]">{g.code}</div>
                </td>
                <td>{g.siteName}</td>
                <td>{g.direction}</td>
                <td>{statusPill(g.status)}</td>
                <td className="space-x-2 py-3">
                  <button
                    disabled={busyId === g.id}
                    onClick={() => void command(g.id, "OPEN")}
                    className="rounded-lg bg-[var(--ok-soft)] px-3 py-1.5 text-xs font-semibold text-[var(--ok)]"
                  >
                    Open
                  </button>
                  <button
                    disabled={busyId === g.id}
                    onClick={() => void command(g.id, "CLOSE")}
                    className="rounded-lg bg-[var(--accent-soft)] px-3 py-1.5 text-xs font-semibold text-[var(--accent)]"
                  >
                    Close
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export function HardwarePage() {
  const { siteId, sites } = useSiteFilter();
  const [rows, setRows] = useState<HardwareRow[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setRows(await api.listHardware(siteId ? { siteId } : undefined));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed");
    }
  }

  useEffect(() => {
    void load();
  }, [siteId]);

  async function onCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    try {
      await api.createHardware({
        siteId: String(fd.get("siteId")),
        name: String(fd.get("name")),
        deviceType: String(fd.get("deviceType")),
        serialNumber: String(fd.get("serial") || "") || undefined,
      });
      e.currentTarget.reset();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Hardware" subtitle="Controllers, readers, cameras, and relays" />
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}
      <form onSubmit={onCreate} className="gp-card grid gap-3 p-4 md:grid-cols-5">
        <select name="siteId" required defaultValue={siteId || sites[0]?.id || ""} className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2">
          {sites.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
        <input name="name" required placeholder="Device name" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
        <select name="deviceType" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2">
          <option>CONTROLLER</option>
          <option>RFID_READER</option>
          <option>QR_READER</option>
          <option>ANPR_CAM</option>
          <option>RELAY</option>
        </select>
        <input name="serial" placeholder="Serial" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
        <button className="rounded-xl bg-[var(--accent)] px-4 py-2 font-semibold text-white">Register</button>
      </form>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {rows.map((d) => (
          <div key={d.id} className="gp-card p-4">
            <div className="flex items-start justify-between gap-2">
              <div>
                <p className="font-bold">{d.name}</p>
                <p className="text-xs text-[var(--muted)]">{d.deviceType} · {d.siteName}</p>
              </div>
              {statusPill(d.connectionStatus)}
            </div>
            <p className="mt-3 text-xs text-[var(--muted)]">Key: {d.deviceApiKey.slice(0, 18)}…</p>
            <p className="text-xs text-[var(--muted)]">
              Last seen: {d.lastSeenAt ? new Date(d.lastSeenAt).toLocaleString() : "never"}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}

export function AlertsPage() {
  const { siteId } = useSiteFilter();
  const [items, setItems] = useState<AlertRow[]>([]);
  const [openCount, setOpenCount] = useState(0);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      const res = await api.listAlerts(siteId ? { siteId } : undefined);
      setItems(res.items);
      setOpenCount(res.openCount);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed");
    }
  }

  useEffect(() => {
    void load();
  }, [siteId]);

  async function setStatus(id: string, status: string) {
    await api.updateAlert(id, status);
    await load();
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Alerts" subtitle={`${openCount} open alerts`} />
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}
      <div className="space-y-3">
        {items.map((a) => (
          <div key={a.id} className="gp-card flex flex-wrap items-start justify-between gap-3 p-4">
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <p className="font-bold">{a.title}</p>
                {statusPill(a.status)}
                <span className="text-xs font-semibold uppercase text-[var(--muted)]">{a.severity}</span>
              </div>
              <p className="mt-1 text-sm text-[var(--muted)]">{a.message}</p>
              <p className="mt-1 text-xs text-[var(--muted)]">
                {[a.siteName, a.type, new Date(a.createdAt).toLocaleString()].filter(Boolean).join(" · ")}
              </p>
            </div>
            <div className="flex gap-2">
              {a.status === "OPEN" && (
                <button onClick={() => void setStatus(a.id, "ACKNOWLEDGED")} className="rounded-lg border border-[var(--line)] px-3 py-1.5 text-xs font-semibold">
                  Acknowledge
                </button>
              )}
              {a.status !== "RESOLVED" && (
                <button onClick={() => void setStatus(a.id, "RESOLVED")} className="rounded-lg bg-[var(--ok-soft)] px-3 py-1.5 text-xs font-semibold text-[var(--ok)]">
                  Resolve
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function UsersRolesPage() {
  const { user } = useAuth();
  const { sites, siteId } = useSiteFilter();
  const [users, setUsers] = useState<UserRow[]>([]);
  const [roles, setRoles] = useState<RoleRow[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      const [u, r] = await Promise.all([
        api.listUsers(siteId ? { siteId } : undefined),
        api.listRoles(),
      ]);
      setUsers(u);
      setRoles(r);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed");
    }
  }

  useEffect(() => {
    void load();
  }, [siteId]);

  async function onCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    try {
      await api.createUser({
        email: String(fd.get("email")),
        fullName: String(fd.get("fullName")),
        password: String(fd.get("password")),
        role: String(fd.get("role")),
        siteId: String(fd.get("siteId") || "") || undefined,
      });
      e.currentTarget.reset();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Users & Roles" subtitle="Tenant operators, guards, and permission templates" />
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}

      {(user?.role === "ClientAdmin" || user?.role === "PlatformAdmin") && (
        <form onSubmit={onCreate} className="gp-card grid gap-3 p-4 md:grid-cols-3 xl:grid-cols-6">
          <input name="fullName" required placeholder="Full name" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
          <input name="email" type="email" required placeholder="Email" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
          <input name="password" type="password" required placeholder="Password" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
          <select name="role" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2">
            <option value="Guard">Guard</option>
            <option value="SiteManager">SiteManager</option>
            <option value="ClientAdmin">ClientAdmin</option>
            <option value="Viewer">Viewer</option>
          </select>
          <select name="siteId" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2">
            <option value="">No site scope</option>
            {sites.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <button className="rounded-xl bg-[var(--accent)] px-4 py-2 font-semibold text-white">Add user</button>
        </form>
      )}

      <div className="gp-card overflow-x-auto">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead className="bg-[var(--panel-2)] text-[var(--muted)]">
            <tr>
              <th className="px-4 py-3 font-medium">User</th>
              <th className="font-medium">Role</th>
              <th className="font-medium">Site</th>
              <th className="font-medium">Status</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="border-t border-[var(--line)]">
                <td className="px-4 py-3">
                  <div className="font-semibold">{u.fullName}</div>
                  <div className="text-xs text-[var(--muted)]">{u.email}</div>
                </td>
                <td>{u.role}</td>
                <td>{u.siteName ?? "—"}</td>
                <td>{statusPill(u.isActive ? "Active" : "Inactive")}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div>
        <h2 className="mb-3 text-lg font-bold">Roles</h2>
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          {roles.map((r) => (
            <div key={r.id} className="gp-card p-4">
              <p className="font-bold">{r.name}</p>
              <p className="text-xs text-[var(--muted)]">{r.code}</p>
              <p className="mt-2 text-sm text-[var(--muted)]">{r.description}</p>
              <p className="mt-2 text-xs text-[var(--muted)]">{r.permissionCount} permissions · {r.userCount} users</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
