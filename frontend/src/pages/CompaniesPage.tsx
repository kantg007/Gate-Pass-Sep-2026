import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { api, type ClientRow } from "../lib/api";
import { useAuth } from "../auth/AuthContext";

export function CompaniesPage() {
  const { user } = useAuth();
  const [clients, setClients] = useState<ClientRow[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setClients(await api.listClients());
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed");
    }
  }

  useEffect(() => {
    void load();
  }, []);

  if (user?.role !== "PlatformAdmin") {
    return <p className="text-[var(--muted)]">Platform admin only.</p>;
  }

  async function toggle(client: ClientRow) {
    const next = client.status === "Active" ? "Suspended" : "Active";
    await api.setClientStatus(client.id, next);
    await load();
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Companies</h1>
        <p className="text-sm text-[var(--muted)]">All tenant clients on the platform</p>
      </div>
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}
      <div className="gp-card overflow-x-auto">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead className="bg-[var(--panel-2)] text-[var(--muted)]">
            <tr>
              <th className="px-4 py-3 font-medium">Client</th>
              <th className="font-medium">Contact</th>
              <th className="font-medium">Sites</th>
              <th className="font-medium">Status</th>
              <th className="font-medium">Action</th>
            </tr>
          </thead>
          <tbody>
            {clients.map((c) => (
              <tr key={c.id} className="border-t border-[var(--line)]">
                <td className="px-4 py-3">
                  <div className="font-semibold">{c.name}</div>
                  <div className="text-xs text-[var(--muted)]">{c.id.slice(0, 8)}…</div>
                </td>
                <td>
                  <div>{c.contactEmail ?? "—"}</div>
                  <div className="text-xs text-[var(--muted)]">{c.phone ?? ""}</div>
                </td>
                <td>
                  {c.siteCount} sites · {c.userCount} users
                </td>
                <td>
                  <span className={c.status === "Active" ? "text-[var(--ok)] font-semibold" : "text-[var(--bad)] font-semibold"}>
                    {c.status}
                  </span>
                </td>
                <td>
                  <button
                    onClick={() => void toggle(c)}
                    className="rounded-lg border border-[var(--line)] bg-white px-3 py-1.5 text-xs font-semibold hover:bg-[var(--panel-2)]"
                  >
                    {c.status === "Active" ? "Suspend" : "Activate"}
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

/** @deprecated use CompaniesPage — kept for route alias */
export const AdminDashboardPage = CompaniesPage;

export function SitesPage() {
  const { user } = useAuth();
  const [sites, setSites] = useState<Awaited<ReturnType<typeof api.listSites>>>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function load() {
    try {
      setSites(await api.listSites());
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed");
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    setBusy(true);
    setError(null);
    try {
      await api.createSite({ name: String(fd.get("name")), slug: String(fd.get("slug")) });
      e.currentTarget.reset();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Sites</h1>
        <p className="text-sm text-[var(--muted)]">
          {user?.client?.name ?? "Your organization"} — societies / campuses / malls
        </p>
      </div>
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}

      {(user?.role === "ClientAdmin" || user?.role === "PlatformAdmin") && (
        <form onSubmit={onCreate} className="gp-card grid gap-3 p-4 sm:grid-cols-3">
          <input name="name" required placeholder="Site name" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
          <input name="slug" required placeholder="slug-like-this" pattern="[a-z0-9-]+" className="rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2" />
          <button disabled={busy} className="rounded-xl bg-[var(--accent)] px-4 py-2 font-semibold text-white disabled:opacity-60">
            Add site
          </button>
        </form>
      )}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {sites.map((s) => (
          <Link key={s.id} to={`/sites/${s.id}`} className="gp-card block p-5 transition hover:-translate-y-0.5 hover:border-[var(--accent)]/40">
            <p className="text-lg font-bold">{s.name}</p>
            <p className="text-xs text-[var(--muted)]">{s.slug}</p>
            <p className="mt-3 text-sm text-[var(--muted)]">
              {s._count?.lanes ?? 0} gates · {s._count?.vehicles ?? 0} vehicles · {s._count?.events ?? 0} events
            </p>
          </Link>
        ))}
      </div>
    </div>
  );
}

export const ClientDashboardPage = SitesPage;
