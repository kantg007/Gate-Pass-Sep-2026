import { useEffect, useState } from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  AlertTriangle,
  Building2,
  Cpu,
  DoorOpen,
  MapPin,
  TrendingDown,
  TrendingUp,
  Truck,
} from "lucide-react";
import { api, type DashboardOverview } from "../lib/api";
import { useSiteFilter } from "../lib/SiteFilterContext";

const KPI_ICONS: Record<string, { icon: React.ReactNode; bg: string; color: string }> = {
  companies: { icon: <Building2 size={18} />, bg: "#dbeafe", color: "#2563eb" },
  sites: { icon: <MapPin size={18} />, bg: "#ede9fe", color: "#7c3aed" },
  gates: { icon: <DoorOpen size={18} />, bg: "#dcfce7", color: "#16a34a" },
  devices: { icon: <Cpu size={18} />, bg: "#e0f2fe", color: "#0284c7" },
  vehiclesToday: { icon: <Truck size={18} />, bg: "#ffedd5", color: "#ea580c" },
  alerts: { icon: <AlertTriangle size={18} />, bg: "#fee2e2", color: "#dc2626" },
};

function formatRelative(iso: string) {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.max(0, Math.round(diff / 60000));
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins} min ago`;
  const hrs = Math.round(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.round(hrs / 24)}d ago`;
}

function statusClass(status: string) {
  const s = status.toLowerCase();
  if (s === "open") return "open";
  if (s === "closed") return "closed";
  return "offline";
}

export function DashboardPage() {
  const { siteId } = useSiteFilter();
  const [data, setData] = useState<DashboardOverview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const overview = await api.dashboardOverview(siteId ? { siteId } : undefined);
        if (!cancelled) setData(overview);
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load dashboard");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    const t = window.setInterval(() => void load(), 15000);
    return () => {
      cancelled = true;
      window.clearInterval(t);
    };
  }, [siteId]);

  const pieData = data
    ? [
        { name: "Open", value: data.gateStatus.open, color: "#16a34a" },
        { name: "Closed", value: data.gateStatus.closed, color: "#2563eb" },
        { name: "Offline", value: data.gateStatus.offline, color: "#dc2626" },
      ]
    : [];

  const maxTop = Math.max(1, ...(data?.topSites.map((s) => s.entries) ?? [1]));

  return (
    <div className="space-y-6">
      <div className="gp-animate-in flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight md:text-3xl">Dashboard</h1>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Live access control overview across {siteId ? "selected site" : "all sites"}
          </p>
        </div>
        <div className="flex items-center gap-2 text-xs font-medium text-[var(--muted)]">
          <span className="gp-live-dot" />
          Live · refreshes every 15s
        </div>
      </div>

      {error && (
        <div className="rounded-xl border border-[var(--bad)]/30 bg-[var(--bad-soft)] px-4 py-3 text-sm text-[var(--bad)]">
          {error}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6">
        {(data?.kpis ?? Array.from({ length: 6 }).map(() => null)).map((kpi, idx) => {
          const meta = kpi ? KPI_ICONS[kpi.key] ?? KPI_ICONS.sites : KPI_ICONS.sites;
          const up = (kpi?.changePct ?? 0) >= 0;
          const alertsDownGood = kpi?.key === "alerts";
          const good = alertsDownGood ? !up : up;
          return (
            <div
              key={kpi?.key ?? idx}
              className="gp-card gp-animate-in p-4"
              style={{ animationDelay: `${idx * 40}ms` }}
            >
              <div className="flex items-start justify-between">
                <div
                  className="gp-kpi-icon"
                  style={{ background: meta.bg, color: meta.color }}
                >
                  {meta.icon}
                </div>
                {kpi && kpi.changePct != null && (
                  <span
                    className={[
                      "inline-flex items-center gap-1 text-xs font-semibold",
                      good ? "text-[var(--ok)]" : "text-[var(--bad)]",
                    ].join(" ")}
                  >
                    {up ? <TrendingUp size={12} /> : <TrendingDown size={12} />}
                    {Math.abs(kpi.changePct)}%
                  </span>
                )}
              </div>
              <p className="mt-4 text-2xl font-bold tracking-tight">
                {loading && !kpi ? "—" : Math.round(kpi?.value ?? 0).toLocaleString()}
              </p>
              <p className="mt-1 text-sm text-[var(--muted)]">{kpi?.label ?? "Loading"}</p>
              <p className="mt-0.5 text-[11px] text-[var(--muted)]">{kpi?.compareLabel ?? ""}</p>
            </div>
          );
        })}
      </div>

      <div className="grid gap-4 xl:grid-cols-12">
        <section className="gp-card gp-animate-in p-5 xl:col-span-7" style={{ animationDelay: "120ms" }}>
          <div className="mb-4 flex items-center justify-between">
            <div>
              <h2 className="text-base font-bold">Vehicle Movement</h2>
              <p className="text-xs text-[var(--muted)]">Entered · Exited · Inside (24h)</p>
            </div>
            <div className="flex gap-3 text-xs font-medium">
              <span className="inline-flex items-center gap-1.5"><i className="h-2 w-2 rounded-full bg-[#16a34a]" /> Entered</span>
              <span className="inline-flex items-center gap-1.5"><i className="h-2 w-2 rounded-full bg-[#2563eb]" /> Exited</span>
              <span className="inline-flex items-center gap-1.5"><i className="h-2 w-2 rounded-full bg-[#7c3aed]" /> Inside</span>
            </div>
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={data?.vehicleMovement ?? []}>
                <defs>
                  <linearGradient id="gEnter" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#16a34a" stopOpacity={0.25} />
                    <stop offset="100%" stopColor="#16a34a" stopOpacity={0} />
                  </linearGradient>
                  <linearGradient id="gExit" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#2563eb" stopOpacity={0.2} />
                    <stop offset="100%" stopColor="#2563eb" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis dataKey="hourLabel" tick={{ fontSize: 11, fill: "#64748b" }} interval={3} />
                <YAxis tick={{ fontSize: 11, fill: "#64748b" }} width={32} />
                <Tooltip
                  contentStyle={{ borderRadius: 12, borderColor: "#e2e8f0", fontSize: 12 }}
                />
                <Area type="monotone" dataKey="entered" name="Entered" stroke="#16a34a" fill="url(#gEnter)" strokeWidth={2} />
                <Area type="monotone" dataKey="exited" name="Exited" stroke="#2563eb" fill="url(#gExit)" strokeWidth={2} />
                <Area type="monotone" dataKey="inside" name="Inside" stroke="#7c3aed" fill="transparent" strokeWidth={2} />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </section>

        <section className="gp-card gp-animate-in p-5 xl:col-span-5" style={{ animationDelay: "160ms" }}>
          <h2 className="text-base font-bold">Gate Status Overview</h2>
          <p className="text-xs text-[var(--muted)]">{data?.gateStatus.total ?? 0} gates total</p>
          <div className="mt-2 flex flex-col items-center gap-4 sm:flex-row">
            <div className="h-44 w-44">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={pieData} dataKey="value" innerRadius={48} outerRadius={72} paddingAngle={3}>
                    {pieData.map((d) => (
                      <Cell key={d.name} fill={d.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="w-full space-y-2 text-sm">
              {pieData.map((d) => (
                <div key={d.name} className="flex items-center justify-between rounded-lg bg-[var(--panel-2)] px-3 py-2">
                  <span className="inline-flex items-center gap-2">
                    <i className="h-2.5 w-2.5 rounded-full" style={{ background: d.color }} />
                    {d.name}
                  </span>
                  <span className="font-semibold">
                    {d.value}
                    <span className="ml-1 text-xs font-normal text-[var(--muted)]">
                      ({data && data.gateStatus.total
                        ? ((d.value / Math.max(data.gateStatus.total, 1)) * 100).toFixed(1)
                        : 0}
                      %)
                    </span>
                  </span>
                </div>
              ))}
            </div>
          </div>

          <div className="mt-4 border-t border-[var(--line)] pt-4">
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-sm font-bold">Live Gate Status</h3>
              <span className="gp-live-dot" />
            </div>
            <div className="max-h-48 space-y-2 overflow-y-auto gp-scroll">
              {(data?.liveGates ?? []).map((g) => (
                <div key={g.id} className="flex items-center justify-between gap-2 rounded-lg px-1 py-1.5">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{g.name}</p>
                    <p className="truncate text-xs text-[var(--muted)]">{g.siteName}</p>
                  </div>
                  <span className={`gp-status-pill ${statusClass(g.status)}`}>{g.status}</span>
                </div>
              ))}
              {!loading && (data?.liveGates.length ?? 0) === 0 && (
                <p className="text-sm text-[var(--muted)]">No gates yet</p>
              )}
            </div>
          </div>
        </section>
      </div>

      <div className="grid gap-4 xl:grid-cols-12">
        <section className="gp-card gp-animate-in p-5 xl:col-span-4" style={{ animationDelay: "200ms" }}>
          <h2 className="text-base font-bold">Recent Activity</h2>
          <p className="mb-4 text-xs text-[var(--muted)]">Latest access events</p>
          <div className="space-y-3">
            {(data?.recentActivity ?? []).map((a) => {
              const denied = a.eventType === "FAIL" || a.decision === "DENY";
              return (
                <div key={a.id} className="flex gap-3">
                  <div className="mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full" style={{ background: denied ? "#dc2626" : "#16a34a" }} />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-start justify-between gap-2">
                      <p className={`text-sm font-semibold ${denied ? "text-[var(--bad)]" : ""}`}>{a.title}</p>
                      <span className="shrink-0 text-[11px] text-[var(--muted)]">{formatRelative(a.createdAt)}</span>
                    </div>
                    <p className="truncate text-xs text-[var(--muted)]">
                      {[a.plateNumber, a.gateName, a.siteName].filter(Boolean).join(" · ")}
                    </p>
                  </div>
                </div>
              );
            })}
          </div>
        </section>

        <section className="gp-card gp-animate-in p-5 xl:col-span-4" style={{ animationDelay: "240ms" }}>
          <h2 className="text-base font-bold">Top Sites Today</h2>
          <p className="mb-4 text-xs text-[var(--muted)]">By vehicle entries</p>
          <div className="space-y-4">
            {(data?.topSites ?? []).map((s) => (
              <div key={s.siteId}>
                <div className="mb-1 flex items-center justify-between gap-2 text-sm">
                  <span className="truncate font-medium">{s.siteName}</span>
                  <span className="shrink-0 font-semibold">
                    {s.entries}
                    {s.changePct != null && (
                      <span className={`ml-2 text-xs ${s.changePct >= 0 ? "text-[var(--ok)]" : "text-[var(--bad)]"}`}>
                        {s.changePct >= 0 ? "↑" : "↓"} {Math.abs(s.changePct)}%
                      </span>
                    )}
                  </span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-[var(--bg-soft)]">
                  <div
                    className="h-full rounded-full bg-gradient-to-r from-[#2563eb] to-[#60a5fa] transition-all duration-700"
                    style={{ width: `${(s.entries / maxTop) * 100}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="gp-card gp-animate-in p-5 xl:col-span-4" style={{ animationDelay: "280ms" }}>
          <h2 className="text-base font-bold">Device Health</h2>
          <p className="mb-4 text-xs text-[var(--muted)]">{data?.deviceHealth.total ?? 0} total devices</p>
          <div className="flex items-center gap-5">
            <div className="relative grid h-36 w-36 place-items-center">
              <svg viewBox="0 0 120 120" className="absolute inset-0">
                <circle cx="60" cy="60" r="48" fill="none" stroke="#e2e8f0" strokeWidth="12" />
                <circle
                  cx="60"
                  cy="60"
                  r="48"
                  fill="none"
                  stroke="#16a34a"
                  strokeWidth="12"
                  strokeLinecap="round"
                  strokeDasharray={`${((data?.deviceHealth.healthyPct ?? 0) / 100) * 301} 301`}
                  transform="rotate(-90 60 60)"
                />
              </svg>
              <div className="text-center">
                <p className="text-2xl font-bold">{data?.deviceHealth.healthyPct ?? 0}%</p>
                <p className="text-[11px] text-[var(--muted)]">Healthy</p>
              </div>
            </div>
            <div className="space-y-2 text-sm">
              <p><span className="font-semibold text-[var(--ok)]">{data?.deviceHealth.healthy ?? 0}</span> Healthy</p>
              <p><span className="font-semibold text-[#b45309]">{data?.deviceHealth.warning ?? 0}</span> Warning</p>
              <p><span className="font-semibold text-[var(--bad)]">{data?.deviceHealth.offline ?? 0}</span> Offline</p>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
