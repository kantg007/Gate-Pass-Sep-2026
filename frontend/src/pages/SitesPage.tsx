import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, type Site } from "../lib/api";

export function SitesPage() {
  const [sites, setSites] = useState<Site[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .listSites()
      .then(setSites)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <p className="text-[var(--muted)]">Loading sites…</p>;
  }

  if (error) {
    return (
      <div className="rounded-xl border border-[var(--bad)]/40 bg-[var(--panel)] p-6">
        <h1 className="font-[family-name:var(--display)] text-2xl">API not reachable</h1>
        <p className="mt-2 text-[var(--muted)]">{error}</p>
        <p className="mt-4 text-sm text-[var(--muted)]">
          Start backend: <code className="text-[var(--accent)]">cd backend && npm run dev</code>
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-[family-name:var(--display)] text-3xl font-bold">Sites</h1>
        <p className="mt-1 text-[var(--muted)]">
          Each site is a society / office. Settings are JSON-driven so rules change without redeploy.
        </p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        {sites.map((site) => (
          <Link
            key={site.id}
            to={`/sites/${site.id}`}
            className="rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5 transition hover:border-[var(--accent)]/50"
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <h2 className="text-lg font-semibold">{site.name}</h2>
                <p className="text-sm text-[var(--muted)]">/{site.slug}</p>
              </div>
              <span
                className={`rounded-full px-2 py-0.5 text-xs ${
                  site.isActive
                    ? "bg-[var(--ok)]/15 text-[var(--ok)]"
                    : "bg-[var(--bad)]/15 text-[var(--bad)]"
                }`}
              >
                {site.isActive ? "Active" : "Off"}
              </span>
            </div>
            <dl className="mt-4 grid grid-cols-3 gap-2 text-center text-sm">
              <div className="rounded-lg bg-[var(--panel-2)] p-2">
                <dt className="text-[var(--muted)]">Vehicles</dt>
                <dd className="font-semibold">{site._count?.vehicles ?? 0}</dd>
              </div>
              <div className="rounded-lg bg-[var(--panel-2)] p-2">
                <dt className="text-[var(--muted)]">Lanes</dt>
                <dd className="font-semibold">{site._count?.lanes ?? 0}</dd>
              </div>
              <div className="rounded-lg bg-[var(--panel-2)] p-2">
                <dt className="text-[var(--muted)]">Events</dt>
                <dd className="font-semibold">{site._count?.events ?? 0}</dd>
              </div>
            </dl>
          </Link>
        ))}
      </div>
    </div>
  );
}
