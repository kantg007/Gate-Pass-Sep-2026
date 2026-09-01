import { NavLink, Outlet, Navigate } from "react-router-dom";
import { homeForRole, useAuth } from "../auth/AuthContext";

export function AppShell() {
  const { user, logout } = useAuth();

  if (!user) return <Navigate to="/login" replace />;

  const links =
    user.role === "PlatformAdmin"
      ? [{ to: "/admin", label: "Clients", end: true }]
      : user.role === "Guard"
        ? [
            { to: "/guard", label: "Guard", end: true },
            { to: "/mock-gate", label: "Mock Gate", end: false },
          ]
        : [
            { to: "/app", label: "My sites", end: true },
            { to: "/guard", label: "Guard", end: false },
            { to: "/mock-gate", label: "Mock Gate", end: false },
          ];

  return (
    <div className="min-h-screen">
      <header className="border-b border-[var(--line)] bg-[color-mix(in_oklab,var(--panel)_88%,transparent)] backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-4 py-4">
          <div>
            <p className="font-[family-name:var(--display)] text-xl font-bold tracking-tight text-[var(--accent)]">
              GateFlow
            </p>
            <p className="text-xs text-[var(--muted)]">
              {user.fullName} · {user.role}
              {user.client?.name ? ` · ${user.client.name}` : ""}
            </p>
          </div>
          <nav className="flex flex-wrap items-center gap-1">
            {links.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.end}
                className={({ isActive }) =>
                  [
                    "rounded-md px-3 py-2 text-sm transition",
                    isActive
                      ? "bg-[var(--panel-2)] text-[var(--text)]"
                      : "text-[var(--muted)] hover:bg-[var(--panel)] hover:text-[var(--text)]",
                  ].join(" ")
                }
              >
                {link.label}
              </NavLink>
            ))}
            <button
              onClick={logout}
              className="ml-2 rounded-md px-3 py-2 text-sm text-[var(--muted)] hover:bg-[var(--panel)] hover:text-[var(--text)]"
            >
              Logout
            </button>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
}

export function RootRedirect() {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return <Navigate to={homeForRole(user.role)} replace />;
}
