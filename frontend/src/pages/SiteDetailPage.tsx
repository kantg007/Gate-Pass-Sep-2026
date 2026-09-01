import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { api, type AccessEvent, type Site, type Vehicle } from "../lib/api";

export function SiteDetailPage() {
  const { siteId = "" } = useParams();
  const [site, setSite] = useState<(Site & { units: { id: string; label: string }[] }) | null>(null);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [events, setEvents] = useState<AccessEvent[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [visitorQr, setVisitorQr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async () => {
    const [s, v, e] = await Promise.all([
      api.getSite(siteId),
      api.listVehicles(siteId),
      api.listEvents(siteId),
    ]);
    setSite(s);
    setVehicles(v);
    setEvents(e);
  }, [siteId]);

  useEffect(() => {
    refresh().catch((err: Error) => setError(err.message));
  }, [refresh]);

  async function onAddVehicle(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault();
    const fd = new FormData(ev.currentTarget);
    setBusy(true);
    setError(null);
    try {
      await api.createVehicle(siteId, {
        plateNumber: String(fd.get("plateNumber") ?? ""),
        label: String(fd.get("label") ?? "") || undefined,
        unitId: String(fd.get("unitId") ?? "") || undefined,
        rfidCode: String(fd.get("rfidCode") ?? "") || undefined,
        barcodeCode: String(fd.get("barcodeCode") ?? "") || undefined,
      });
      ev.currentTarget.reset();
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    } finally {
      setBusy(false);
    }
  }

  async function onAddVisitor(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault();
    const fd = new FormData(ev.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const res = await api.createVisitor(siteId, {
        guestName: String(fd.get("guestName") ?? ""),
        unitId: String(fd.get("unitId") ?? "") || undefined,
        purpose: String(fd.get("purpose") ?? "") || undefined,
      });
      setVisitorQr(res.qrPayload);
      ev.currentTarget.reset();
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    } finally {
      setBusy(false);
    }
  }

  if (!site && !error) {
    return <p className="text-[var(--muted)]">Loading…</p>;
  }

  if (!site) {
    return <p className="text-[var(--bad)]">{error}</p>;
  }

  return (
    <div className="space-y-8">
      <div>
        <Link to="/" className="text-sm text-[var(--muted)] hover:text-[var(--accent)]">
          ← Sites
        </Link>
        <h1 className="mt-2 font-[family-name:var(--display)] text-3xl font-bold">{site.name}</h1>
        <p className="text-[var(--muted)]">Admin · vehicles, visitors, live logs</p>
      </div>

      {error && (
        <p className="rounded-lg border border-[var(--bad)]/40 bg-[var(--panel)] px-3 py-2 text-sm text-[var(--bad)]">
          {error}
        </p>
      )}

      <section className="grid gap-6 lg:grid-cols-2">
        <form
          onSubmit={onAddVehicle}
          className="space-y-3 rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5"
        >
          <h2 className="text-lg font-semibold">Add vehicle + credential</h2>
          <Field name="plateNumber" label="Plate number" placeholder="MH12AB9999" required />
          <Field name="label" label="Label" placeholder="Owner car" />
          <label className="block text-sm">
            <span className="text-[var(--muted)]">Unit</span>
            <select
              name="unitId"
              className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2"
            >
              <option value="">—</option>
              {site.units.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.label}
                </option>
              ))}
            </select>
          </label>
          <Field name="rfidCode" label="RFID code (optional)" placeholder="RFID-2002" />
          <Field name="barcodeCode" label="Barcode (optional)" placeholder="BC-9999" />
          <button
            disabled={busy}
            className="rounded-lg bg-[var(--accent)] px-4 py-2 font-medium text-[#1a1408] disabled:opacity-60"
          >
            Save vehicle
          </button>
        </form>

        <form
          onSubmit={onAddVisitor}
          className="space-y-3 rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5"
        >
          <h2 className="text-lg font-semibold">Create visitor QR</h2>
          <Field name="guestName" label="Guest name" placeholder="Amit" required />
          <label className="block text-sm">
            <span className="text-[var(--muted)]">Visiting unit</span>
            <select
              name="unitId"
              className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2"
            >
              <option value="">—</option>
              {site.units.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.label}
                </option>
              ))}
            </select>
          </label>
          <Field name="purpose" label="Purpose" placeholder="Delivery / family" />
          <button
            disabled={busy}
            className="rounded-lg bg-[var(--panel-2)] px-4 py-2 font-medium ring-1 ring-[var(--line)] disabled:opacity-60"
          >
            Generate QR code
          </button>
          {visitorQr && (
            <div className="rounded-lg bg-[var(--panel-2)] p-3 text-sm">
              <p className="text-[var(--muted)]">Share / scan this payload:</p>
              <p className="mt-1 break-all font-mono text-[var(--accent)]">{visitorQr}</p>
            </div>
          )}
        </form>
      </section>

      <section className="rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5">
        <h2 className="text-lg font-semibold">Vehicles</h2>
        <div className="mt-3 overflow-x-auto">
          <table className="w-full min-w-[520px] text-left text-sm">
            <thead className="text-[var(--muted)]">
              <tr>
                <th className="py-2 font-medium">Plate</th>
                <th className="font-medium">Unit</th>
                <th className="font-medium">Credentials</th>
              </tr>
            </thead>
            <tbody>
              {vehicles.map((v) => (
                <tr key={v.id} className="border-t border-[var(--line)]">
                  <td className="py-2 font-medium">{v.plateNumber}</td>
                  <td>{v.unit?.label ?? "—"}</td>
                  <td className="text-[var(--muted)]">
                    {v.credentials.map((c) => `${c.type}:${c.code}`).join(" · ") || "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5">
        <h2 className="text-lg font-semibold">Recent access events</h2>
        <ul className="mt-3 space-y-2">
          {events.map((e) => (
            <li
              key={e.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-[var(--panel-2)] px-3 py-2 text-sm"
            >
              <div>
                <span
                  className={
                    e.decision === "ALLOW" ? "text-[var(--ok)]" : "text-[var(--bad)]"
                  }
                >
                  {e.decision}
                </span>
                <span className="text-[var(--muted)]"> · {e.reason}</span>
                <div className="text-[var(--muted)]">
                  {e.credentialType}:{e.credentialCode}
                  {e.plateNumber ? ` · ${e.plateNumber}` : ""}
                </div>
              </div>
              <time className="text-xs text-[var(--muted)]">
                {new Date(e.createdAt).toLocaleString()}
              </time>
            </li>
          ))}
          {events.length === 0 && (
            <li className="text-sm text-[var(--muted)]">No events yet — try Mock Gate.</li>
          )}
        </ul>
      </section>
    </div>
  );
}

function Field(props: {
  name: string;
  label: string;
  placeholder?: string;
  required?: boolean;
}) {
  return (
    <label className="block text-sm">
      <span className="text-[var(--muted)]">{props.label}</span>
      <input
        name={props.name}
        required={props.required}
        placeholder={props.placeholder}
        className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2 outline-none ring-[var(--accent)] focus:ring-1"
      />
    </label>
  );
}
