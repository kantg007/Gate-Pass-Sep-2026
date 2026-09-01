import { useEffect, useState, type FormEvent } from "react";
import { api, type AccessResult, type Site } from "../lib/api";

export function GuardPage() {
  const [sites, setSites] = useState<Site[]>([]);
  const [siteId, setSiteId] = useState("");
  const [code, setCode] = useState("");
  const [result, setResult] = useState<AccessResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.listSites().then((list) => {
      setSites(list);
      if (list[0]) setSiteId(list[0].id);
    });
  }, []);

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    setError(null);
    try {
      const res = await api.checkAccess({
        siteId,
        credentialType: "QR",
        code,
      });
      setResult(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    }
  }

  return (
    <div className="mx-auto max-w-lg space-y-6">
      <div>
        <h1 className="font-[family-name:var(--display)] text-3xl font-bold">Guard desk</h1>
        <p className="mt-1 text-[var(--muted)]">
          Paste visitor QR payload (or scan later with camera). Boom opens only on ALLOW.
        </p>
      </div>
      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5"
      >
        <label className="block text-sm">
          <span className="text-[var(--muted)]">Site</span>
          <select
            value={siteId}
            onChange={(e) => setSiteId(e.target.value)}
            className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2"
          >
            {sites.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm">
          <span className="text-[var(--muted)]">QR / code</span>
          <input
            value={code}
            onChange={(e) => setCode(e.target.value)}
            required
            placeholder="VIS-…"
            className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2 font-mono"
          />
        </label>
        <button className="w-full rounded-lg bg-[var(--accent)] px-4 py-3 font-semibold text-[#1a1408]">
          Check & open
        </button>
      </form>
      {error && <p className="text-[var(--bad)]">{error}</p>}
      {result && <ResultCard result={result} />}
    </div>
  );
}

function ResultCard({ result }: { result: AccessResult }) {
  return (
    <div
      className={`rounded-xl border p-5 ${
        result.open
          ? "border-[var(--ok)]/40 bg-[var(--ok)]/10"
          : "border-[var(--bad)]/40 bg-[var(--bad)]/10"
      }`}
    >
      <p className="font-[family-name:var(--display)] text-2xl font-bold">
        {result.open ? "OPEN BARRIER" : "KEEP CLOSED"}
      </p>
      <p className="mt-1 text-sm opacity-90">{result.reason}</p>
      {result.plateNumber && <p className="mt-2 text-sm">Plate: {result.plateNumber}</p>}
      {result.guestName && <p className="mt-2 text-sm">Guest: {result.guestName}</p>}
    </div>
  );
}
