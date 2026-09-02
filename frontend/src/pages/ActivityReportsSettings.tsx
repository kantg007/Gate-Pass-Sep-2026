import { useEffect, useState } from "react";
import { api } from "../lib/api";
import { useSiteFilter } from "../lib/SiteFilterContext";
import { useAuth } from "../auth/AuthContext";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

export function ActivityPage() {
  const { siteId, sites } = useSiteFilter();
  const [events, setEvents] = useState<
    { id: string; decision: string; reason: string; plateNumber: string | null; createdAt: string; lane: { name: string } | null; siteName?: string }[]
  >([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const targetSites = siteId ? sites.filter((s) => s.id === siteId) : sites;
        const batches = await Promise.all(
          targetSites.slice(0, 8).map(async (s) => {
            const rows = await api.listEvents(s.id);
            return rows.map((r) => ({ ...r, siteName: s.name }));
          }),
        );
        setEvents(
          batches.flat().sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt)).slice(0, 60),
        );
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed");
      }
    })();
  }, [siteId, sites]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Access Activity</h1>
        <p className="text-sm text-[var(--muted)]">Recent passes, denies, and manual opens</p>
      </div>
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}
      <div className="gp-card overflow-x-auto">
        <table className="w-full min-w-[720px] text-left text-sm">
          <thead className="bg-[var(--panel-2)] text-[var(--muted)]">
            <tr>
              <th className="px-4 py-3 font-medium">When</th>
              <th className="font-medium">Site / Gate</th>
              <th className="font-medium">Plate</th>
              <th className="font-medium">Decision</th>
              <th className="font-medium">Reason</th>
            </tr>
          </thead>
          <tbody>
            {events.map((e) => (
              <tr key={e.id} className="border-t border-[var(--line)]">
                <td className="px-4 py-3 text-[var(--muted)]">{new Date(e.createdAt).toLocaleString()}</td>
                <td>
                  <div className="font-medium">{e.siteName}</div>
                  <div className="text-xs text-[var(--muted)]">{e.lane?.name ?? "—"}</div>
                </td>
                <td className="font-semibold">{e.plateNumber ?? "—"}</td>
                <td>
                  <span className={e.decision === "ALLOW" ? "text-[var(--ok)] font-semibold" : "text-[var(--bad)] font-semibold"}>
                    {e.decision}
                  </span>
                </td>
                <td className="text-[var(--muted)]">{e.reason}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export function ReportsPage() {
  const { siteId } = useSiteFilter();
  const [summary, setSummary] = useState<{ eventType: string; count: number }[]>([]);
  const [top, setTop] = useState<{ siteName: string; entries: number; exits: number }[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const [s, t] = await Promise.all([
          api.accessSummary(siteId ? { siteId } : undefined),
          api.topSites(),
        ]);
        const rolled = new Map<string, number>();
        for (const row of s.rows) {
          rolled.set(row.eventType, (rolled.get(row.eventType) ?? 0) + row.count);
        }
        setSummary([...rolled.entries()].map(([eventType, count]) => ({ eventType, count })));
        setTop(t.sites.map((x) => ({ siteName: x.siteName, entries: x.entries, exits: x.exits })));
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed");
      }
    })();
  }, [siteId]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Reports</h1>
        <p className="text-sm text-[var(--muted)]">Access summary and site rankings</p>
      </div>
      {error && <p className="text-sm text-[var(--bad)]">{error}</p>}
      <div className="grid gap-4 xl:grid-cols-2">
        <section className="gp-card p-5">
          <h2 className="mb-4 font-bold">Event types (selected range)</h2>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={summary}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis dataKey="eventType" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} width={36} />
                <Tooltip />
                <Bar dataKey="count" fill="#2563eb" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </section>
        <section className="gp-card p-5">
          <h2 className="mb-4 font-bold">Top sites today</h2>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={top} layout="vertical" margin={{ left: 24 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis type="number" tick={{ fontSize: 11 }} />
                <YAxis type="category" dataKey="siteName" width={120} tick={{ fontSize: 11 }} />
                <Tooltip />
                <Bar dataKey="entries" fill="#16a34a" radius={[0, 8, 8, 0]} name="Entries" />
                <Bar dataKey="exits" fill="#2563eb" radius={[0, 8, 8, 0]} name="Exits" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </section>
      </div>
    </div>
  );
}

export function SettingsPage() {
  const { user } = useAuth();
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Settings</h1>
        <p className="text-sm text-[var(--muted)]">Account and workspace preferences</p>
      </div>
      <div className="gp-card max-w-xl space-y-4 p-5">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">Signed in as</p>
          <p className="text-lg font-bold">{user?.fullName}</p>
          <p className="text-sm text-[var(--muted)]">{user?.email}</p>
        </div>
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div className="rounded-xl bg-[var(--panel-2)] p-3">
            <p className="text-[var(--muted)]">Role</p>
            <p className="font-semibold">{user?.role}</p>
          </div>
          <div className="rounded-xl bg-[var(--panel-2)] p-3">
            <p className="text-[var(--muted)]">Client</p>
            <p className="font-semibold">{user?.client?.name ?? "Platform"}</p>
          </div>
        </div>
        <p className="text-sm text-[var(--muted)]">
          Theme is locked to the GatePass operations light shell across the app. Site scope uses the header “All Sites” filter.
        </p>
      </div>
    </div>
  );
}
