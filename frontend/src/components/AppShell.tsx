import { useEffect, useMemo, useState } from "react";
import { NavLink, Outlet, Navigate, useNavigate } from "react-router-dom";
import {
  Activity,
  AlertTriangle,
  Bell,
  Building2,
  ChevronDown,
  Cpu,
  DoorOpen,
  LayoutDashboard,
  LogOut,
  MapPin,
  Menu,
  Search,
  Settings,
  Shield,
  Users,
  X,
} from "lucide-react";
import { homeForRole, useAuth } from "../auth/AuthContext";
import { api, type SearchHit } from "../lib/api";
import { SiteFilterProvider, useSiteFilter } from "../lib/SiteFilterContext";

type NavItem = { to: string; label: string; icon: React.ReactNode; end?: boolean; roles?: string[] };

const NAV: NavItem[] = [
  { to: "/dashboard", label: "Dashboard", icon: <LayoutDashboard size={18} />, end: true },
  { to: "/companies", label: "Companies", icon: <Building2 size={18} />, roles: ["PlatformAdmin"] },
  { to: "/sites", label: "Sites", icon: <MapPin size={18} /> },
  { to: "/users", label: "Users & Roles", icon: <Users size={18} />, roles: ["PlatformAdmin", "ClientAdmin", "SiteManager"] },
  { to: "/hardware", label: "Hardware", icon: <Cpu size={18} /> },
  { to: "/gates", label: "Gates", icon: <DoorOpen size={18} /> },
  { to: "/activity", label: "Access Activity", icon: <Activity size={18} /> },
  { to: "/reports", label: "Reports", icon: <Shield size={18} /> },
  { to: "/alerts", label: "Alerts", icon: <AlertTriangle size={18} /> },
  { to: "/settings", label: "Settings", icon: <Settings size={18} /> },
];

function ShellInner() {
  const { user, logout } = useAuth();
  const nav = useNavigate();
  const { sites, siteId, setSiteId } = useSiteFilter();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [hits, setHits] = useState<SearchHit[]>([]);
  const [alertCount, setAlertCount] = useState(0);

  const links = useMemo(
    () =>
      !user
        ? []
        : NAV.filter((item) => {
            if (user.role === "Guard") {
              return ["/dashboard", "/gates", "/activity", "/alerts", "/settings"].includes(item.to);
            }
            if (!item.roles) return true;
            return item.roles.includes(user.role);
          }),
    [user],
  );

  useEffect(() => {
    if (!user) return;
    void api.listAlerts({ status: "OPEN" }).then((r) => setAlertCount(r.openCount)).catch(() => undefined);
  }, [user]);

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        setSearchOpen(true);
      }
      if (e.key === "Escape") setSearchOpen(false);
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  useEffect(() => {
    if (!searchOpen || query.trim().length < 2) {
      setHits([]);
      return;
    }
    const t = window.setTimeout(() => {
      void api.search(query).then((r) => setHits(r.hits)).catch(() => setHits([]));
    }, 200);
    return () => window.clearTimeout(t);
  }, [query, searchOpen]);

  if (!user) return <Navigate to="/login" replace />;

  return (
    <div className="flex min-h-screen bg-[var(--bg)] text-[var(--text)]">
      {/* Mobile overlay */}
      {sidebarOpen && (
        <button
          className="fixed inset-0 z-30 bg-black/40 lg:hidden"
          aria-label="Close menu"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <aside
        className={[
          "fixed inset-y-0 left-0 z-40 flex w-[260px] flex-col bg-[var(--sidebar)] text-[var(--sidebar-text)] transition-transform lg:static lg:translate-x-0",
          sidebarOpen ? "translate-x-0" : "-translate-x-full",
        ].join(" ")}
      >
        <div className="flex items-center gap-3 px-5 py-5">
          <div className="grid h-10 w-10 place-items-center rounded-xl bg-[var(--accent)] text-white font-bold">
            GP
          </div>
          <div>
            <p className="text-sm font-bold tracking-wide text-white">GatePass</p>
            <p className="text-[11px] text-[var(--sidebar-muted)]">Access Control</p>
          </div>
          <button className="ml-auto text-white/70 lg:hidden" onClick={() => setSidebarOpen(false)}>
            <X size={18} />
          </button>
        </div>

        <nav className="gp-scroll flex-1 space-y-1 overflow-y-auto px-3 pb-4">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.end}
              onClick={() => setSidebarOpen(false)}
              className={({ isActive }) =>
                [
                  "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition",
                  isActive
                    ? "bg-[var(--sidebar-active)] text-white shadow-sm"
                    : "text-[var(--sidebar-text)] hover:bg-[var(--sidebar-hover)] hover:text-white",
                ].join(" ")
              }
            >
              <span className="opacity-90">{link.icon}</span>
              {link.label}
            </NavLink>
          ))}
          {(user.role === "Guard" || user.role === "ClientAdmin") && (
            <>
              <NavLink
                to="/guard"
                onClick={() => setSidebarOpen(false)}
                className={({ isActive }) =>
                  [
                    "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition",
                    isActive
                      ? "bg-[var(--sidebar-active)] text-white"
                      : "text-[var(--sidebar-text)] hover:bg-[var(--sidebar-hover)] hover:text-white",
                  ].join(" ")
                }
              >
                <Shield size={18} />
                Guard tools
              </NavLink>
              <NavLink
                to="/mock-gate"
                onClick={() => setSidebarOpen(false)}
                className={({ isActive }) =>
                  [
                    "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition",
                    isActive
                      ? "bg-[var(--sidebar-active)] text-white"
                      : "text-[var(--sidebar-text)] hover:bg-[var(--sidebar-hover)] hover:text-white",
                  ].join(" ")
                }
              >
                <DoorOpen size={18} />
                Mock Gate
              </NavLink>
            </>
          )}
        </nav>

        <div className="mt-auto border-t border-white/10 p-4">
          <div className="mb-3 overflow-hidden rounded-xl bg-gradient-to-br from-[#1e3a5f] to-[#0f1c2e] p-3">
            <p className="text-[11px] text-[var(--sidebar-muted)]">Live barrier network</p>
            <p className="mt-1 text-sm font-semibold text-white">Boom gate ops</p>
          </div>
          <div className="flex items-center gap-3">
            <div className="grid h-9 w-9 place-items-center rounded-full bg-[var(--accent)] text-xs font-bold text-white">
              {user.fullName.slice(0, 1).toUpperCase()}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-white">{user.fullName}</p>
              <p className="truncate text-[11px] text-[var(--sidebar-muted)]">
                {user.role === "PlatformAdmin" ? "Platform Admin" : user.role}
              </p>
            </div>
            <button
              onClick={() => {
                logout();
                nav("/login");
              }}
              className="rounded-lg p-2 text-[var(--sidebar-muted)] hover:bg-white/10 hover:text-white"
              title="Logout"
            >
              <LogOut size={16} />
            </button>
          </div>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 flex items-center gap-3 border-b border-[var(--line)] bg-white/90 px-4 py-3 backdrop-blur md:px-6">
          <button
            className="rounded-lg border border-[var(--line)] p-2 text-[var(--muted)] lg:hidden"
            onClick={() => setSidebarOpen(true)}
          >
            <Menu size={18} />
          </button>

          <button
            onClick={() => setSearchOpen(true)}
            className="flex min-w-0 flex-1 items-center gap-2 rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2 text-sm text-[var(--muted)] transition hover:border-[var(--line-strong)] md:max-w-md"
          >
            <Search size={16} />
            <span className="truncate">Search companies, gates, vehicles…</span>
            <kbd className="ml-auto hidden rounded-md border border-[var(--line)] bg-white px-1.5 py-0.5 text-[10px] font-semibold text-[var(--muted)] sm:inline">
              Ctrl+K
            </kbd>
          </button>

          <div className="relative">
            <select
              value={siteId}
              onChange={(e) => setSiteId(e.target.value)}
              className="appearance-none rounded-xl border border-[var(--line)] bg-white py-2 pl-3 pr-8 text-sm font-medium"
            >
              <option value="">All Sites</option>
              {sites.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
            <ChevronDown size={14} className="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 text-[var(--muted)]" />
          </div>

          <button
            onClick={() => nav("/alerts")}
            className="relative rounded-xl border border-[var(--line)] bg-white p-2 text-[var(--muted)] hover:text-[var(--text)]"
          >
            <Bell size={18} />
            {alertCount > 0 && (
              <span className="absolute -right-1 -top-1 grid h-5 min-w-5 place-items-center rounded-full bg-[var(--bad)] px-1 text-[10px] font-bold text-white">
                {alertCount}
              </span>
            )}
          </button>

          <div className="hidden items-center gap-2 sm:flex">
            <div className="grid h-9 w-9 place-items-center rounded-full bg-[var(--accent-soft)] text-sm font-bold text-[var(--accent)]">
              {user.fullName.slice(0, 1).toUpperCase()}
            </div>
          </div>
        </header>

        <main className="flex-1 px-4 py-6 md:px-6">
          <Outlet />
        </main>
      </div>

      {searchOpen && (
        <div className="fixed inset-0 z-50 flex items-start justify-center bg-black/40 p-4 pt-[12vh]">
          <div className="w-full max-w-xl overflow-hidden rounded-2xl bg-white shadow-2xl">
            <div className="flex items-center gap-2 border-b border-[var(--line)] px-4 py-3">
              <Search size={18} className="text-[var(--muted)]" />
              <input
                autoFocus
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search…"
                className="w-full border-0 bg-transparent text-sm outline-none"
              />
              <button onClick={() => setSearchOpen(false)} className="text-[var(--muted)]">
                <X size={18} />
              </button>
            </div>
            <div className="max-h-80 overflow-y-auto p-2">
              {hits.length === 0 && (
                <p className="px-3 py-6 text-center text-sm text-[var(--muted)]">
                  {query.trim().length < 2 ? "Type at least 2 characters" : "No matches"}
                </p>
              )}
              {hits.map((h) => (
                <button
                  key={`${h.type}-${h.id}`}
                  className="flex w-full items-start gap-3 rounded-xl px-3 py-2.5 text-left hover:bg-[var(--panel-2)]"
                  onClick={() => {
                    setSearchOpen(false);
                    if (h.href) nav(h.href);
                  }}
                >
                  <span className="mt-0.5 rounded-md bg-[var(--accent-soft)] px-2 py-0.5 text-[10px] font-bold uppercase text-[var(--accent)]">
                    {h.type}
                  </span>
                  <span>
                    <span className="block text-sm font-semibold">{h.title}</span>
                    {h.subtitle && <span className="text-xs text-[var(--muted)]">{h.subtitle}</span>}
                  </span>
                </button>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export function AppShell() {
  return (
    <SiteFilterProvider>
      <ShellInner />
    </SiteFilterProvider>
  );
}

export function RootRedirect() {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return <Navigate to={homeForRole(user.role)} replace />;
}
