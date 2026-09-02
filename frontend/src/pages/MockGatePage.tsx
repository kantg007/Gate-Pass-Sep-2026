import { useEffect, useState, type FormEvent } from "react";
import { api, type AccessResult, type Lane, type Site } from "../lib/api";

const PRESETS = [
  { label: "Resident RFID", type: "RFID", code: "RFID-1001" },
  { label: "Resident barcode", type: "BARCODE", code: "BC-7788" },
  { label: "Visitor QR", type: "QR", code: "VIS-DEMO-001" },
  { label: "Unknown tag", type: "RFID", code: "RFID-UNKNOWN" },
  { label: "Manual open", type: "MANUAL", code: "guard-override" },
];

export function MockGatePage() {
  const [sites, setSites] = useState<Site[]>([]);
  const [siteId, setSiteId] = useState("");
  const [lanes, setLanes] = useState<Lane[]>([]);
  const [deviceKey, setDeviceKey] = useState("");
  const [type, setType] = useState("RFID");
  const [code, setCode] = useState("RFID-1001");
  const [result, setResult] = useState<AccessResult | null>(null);
  const [armUp, setArmUp] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.listSites().then(async (list) => {
      setSites(list);
      if (list[0]) {
        setSiteId(list[0].id);
        const laneList = await api.listLanes(list[0].id);
        setLanes(laneList);
        if (laneList[0]) setDeviceKey(laneList[0].deviceApiKey);
      }
    });
  }, []);

  useEffect(() => {
    if (!siteId) return;
    api.listLanes(siteId).then((laneList) => {
      setLanes(laneList);
      if (laneList[0]) setDeviceKey(laneList[0].deviceApiKey);
    });
  }, [siteId]);

  async function tap(ev?: FormEvent) {
    ev?.preventDefault();
    setError(null);
    try {
      const res = await api.checkAccess(
        { siteId, credentialType: type, code },
        deviceKey || undefined,
      );
      setResult(res);
      setArmUp(res.open);
      if (res.open) {
        window.setTimeout(() => setArmUp(false), 2500);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    }
  }

  return (
    <div className="grid gap-8 lg:grid-cols-[1.1fr_0.9fr]">
      <div className="space-y-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Mock gate</h1>
          <p className="mt-1 text-[var(--muted)]">
            Simulates the IoT controller: sends credential + device key to the API. No hardware needed yet.
          </p>
        </div>

        <div
          className="relative overflow-hidden rounded-2xl border border-[var(--line)] bg-[var(--panel)] p-8"
          aria-hidden
        >
          <div className="mx-auto h-4 w-40 rounded bg-[var(--panel-2)]" />
          <div className="relative mx-auto mt-2 h-40 w-8 bg-[var(--panel-2)]">
            <div
              className="absolute left-8 top-4 h-5 origin-left rounded-r-full bg-[var(--accent)] shadow-lg transition-transform duration-700"
              style={{
                width: "180px",
                transform: armUp ? "rotate(-75deg)" : "rotate(0deg)",
              }}
            />
          </div>
          <p className="mt-6 text-center text-sm text-[var(--muted)]">
            Barrier arm: {armUp ? "OPEN" : "CLOSED"}
          </p>
        </div>

        {result && (
          <div
            className={`rounded-xl border px-4 py-3 text-sm ${
              result.open
                ? "border-[var(--ok)]/40 text-[var(--ok)]"
                : "border-[var(--bad)]/40 text-[var(--bad)]"
            }`}
          >
            {result.decision}: {result.reason}
            {result.plateNumber ? ` · ${result.plateNumber}` : ""}
            {result.guestName ? ` · ${result.guestName}` : ""}
          </div>
        )}
        {error && <p className="text-[var(--bad)]">{error}</p>}
      </div>

      <form
        onSubmit={tap}
        className="space-y-3 rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5"
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
          <span className="text-[var(--muted)]">Lane device key</span>
          <select
            value={deviceKey}
            onChange={(e) => setDeviceKey(e.target.value)}
            className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2 font-mono text-xs"
          >
            {lanes.map((l) => (
              <option key={l.id} value={l.deviceApiKey}>
                {l.name} · {l.deviceApiKey}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm">
          <span className="text-[var(--muted)]">Credential type</span>
          <select
            value={type}
            onChange={(e) => setType(e.target.value)}
            className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2"
          >
            <option value="RFID">RFID</option>
            <option value="BARCODE">BARCODE</option>
            <option value="QR">QR</option>
            <option value="MANUAL">MANUAL</option>
          </select>
        </label>
        <label className="block text-sm">
          <span className="text-[var(--muted)]">Code</span>
          <input
            value={code}
            onChange={(e) => setCode(e.target.value)}
            className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2 font-mono"
          />
        </label>
        <div className="flex flex-wrap gap-2">
          {PRESETS.map((p) => (
            <button
              key={p.label}
              type="button"
              onClick={() => {
                setType(p.type);
                setCode(p.code);
              }}
              className="rounded-md bg-[var(--panel-2)] px-2 py-1 text-xs text-[var(--muted)] ring-1 ring-[var(--line)] hover:text-[var(--text)]"
            >
              {p.label}
            </button>
          ))}
        </div>
        <button className="w-full rounded-xl bg-[var(--accent)] px-4 py-3 font-semibold text-white">
          Simulate tap
        </button>
      </form>
    </div>
  );
}
