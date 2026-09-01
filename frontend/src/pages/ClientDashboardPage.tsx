import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { api, type Site } from "../lib/api";

export function ClientDashboardPage() {
  const { user } = useAuth();
  const [sites, setSites] = useState<Site[]>([]);
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
      await api.createSite({
        name: String(fd.get("name")),
        slug: String(fd.get("slug")),
      });
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
        <h1 className="font-[family-name:var(--display)] text-3xl font-bold">Client dashboard</h1>
        <p className="text-[var(--muted)]">
          {user?.client?.name ?? "Your organization"} — only your sites & vehicles (tenant isolated).
        </p>
      </div>

      {error && <p className="text-[var(--bad)]">{error}</p>}

      {user?.role === "ClientAdmin" && (
        <form
          onSubmit={onCreate}
          className="grid gap-3 rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5 sm:grid-cols-3"
        >
          <input
            name="name"
            required
            placeholder="Site name"
            className="rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2"
          />
          <input
            name="slug"
            required
            placeholder="slug-like-this"
            pattern="[a-z0-9-]+"
            className="rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2"
          />
          <button
            disabled={busy}
            className="rounded-lg bg-[var(--accent)] px-4 py-2 font-medium text-[#1a1408] disabled:opacity-60"
          >
            Add site
          </button>
        </form>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {sites.map((site) => (
          <Link
            key={site.id}
            to={`/sites/${site.id}`}
            className="rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5 transition hover:border-[var(--accent)]/50"
          >
            <h2 className="text-lg font-semibold">{site.name}</h2>
            <p className="text-sm text-[var(--muted)]">/{site.slug}</p>
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
        {sites.length === 0 && (
          <p className="text-[var(--muted)]">No sites yet — add your first society / campus.</p>
        )}
      </div>
    </div>
  );
}
