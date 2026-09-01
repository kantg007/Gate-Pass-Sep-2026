import { useEffect, useState } from "react";
import { api, type ClientRow } from "../lib/api";

export function AdminDashboardPage() {
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

  async function toggle(client: ClientRow) {
    const next = client.status === "Active" ? "Suspended" : "Active";
    await api.setClientStatus(client.id, next);
    await load();
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-[family-name:var(--display)] text-3xl font-bold">Platform admin</h1>
        <p className="text-[var(--muted)]">
          GateFlow operator view — all clients (like Park+ HQ). Client data stays isolated.
        </p>
      </div>
      {error && <p className="text-[var(--bad)]">{error}</p>}
      <div className="overflow-x-auto rounded-xl border border-[var(--line)] bg-[var(--panel)]">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead className="text-[var(--muted)]">
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
                  <div className="font-medium">{c.name}</div>
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
                  <span
                    className={
                      c.status === "Active" ? "text-[var(--ok)]" : "text-[var(--bad)]"
                    }
                  >
                    {c.status}
                  </span>
                </td>
                <td>
                  <button
                    onClick={() => void toggle(c)}
                    className="rounded-md bg-[var(--panel-2)] px-2 py-1 text-xs ring-1 ring-[var(--line)]"
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
