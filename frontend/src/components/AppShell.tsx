import { NavLink, Outlet } from "react-router-dom";

const links = [
  { to: "/", label: "Sites", end: true },
  { to: "/guard", label: "Guard" },
  { to: "/mock-gate", label: "Mock Gate" },
];

export function AppShell() {
  return (
    <div className="min-h-screen">
      <header className="border-b border-[var(--line)] bg-[color-mix(in_oklab,var(--panel)_88%,transparent)] backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-4">
          <div>
            <p className="font-[family-name:var(--display)] text-xl font-bold tracking-tight text-[var(--accent)]">
              GateFlow
            </p>
            <p className="text-xs text-[var(--muted)]">
              Boom access · RFID / QR / barcode
            </p>
          </div>
          <nav className="flex flex-wrap gap-1">
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
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
}
