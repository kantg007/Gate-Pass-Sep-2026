import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { homeForRole, useAuth } from "../auth/AuthContext";

export function LoginPage() {
  const { login } = useAuth();
  const nav = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const user = await login(String(fd.get("email")), String(fd.get("password")));
      nav(homeForRole(user.role));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthCard title="Sign in" subtitle="Client, guard, or platform admin">
      <form onSubmit={onSubmit} className="space-y-3">
        <Input name="email" label="Email" type="email" defaultValue="client@greenvalley.local" required />
        <Input name="password" label="Password" type="password" defaultValue="Client@123" required />
        {error && <p className="text-sm text-[var(--bad)]">{error}</p>}
        <button
          disabled={busy}
          className="w-full rounded-xl bg-[var(--accent)] px-4 py-3 font-semibold text-white disabled:opacity-60"
        >
          {busy ? "Signing in…" : "Sign in"}
        </button>
      </form>
      <p className="mt-4 text-sm text-[var(--muted)]">
        New society / company?{" "}
        <Link className="font-semibold text-[var(--accent)]" to="/register">
          Register as client
        </Link>
      </p>
      <DemoAccounts />
    </AuthCard>
  );
}

export function RegisterPage() {
  const { register } = useAuth();
  const nav = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const user = await register({
        companyName: String(fd.get("companyName")),
        fullName: String(fd.get("fullName")),
        email: String(fd.get("email")),
        password: String(fd.get("password")),
        phone: String(fd.get("phone") || "") || undefined,
      });
      nav(homeForRole(user.role));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Register failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthCard title="Client registration" subtitle="Create your company account (Park+ style tenant)">
      <form onSubmit={onSubmit} className="space-y-3">
        <Input name="companyName" label="Company / RWA name" required />
        <Input name="fullName" label="Your name" required />
        <Input name="email" label="Email" type="email" required />
        <Input name="phone" label="Phone" />
        <Input name="password" label="Password" type="password" required />
        {error && <p className="text-sm text-[var(--bad)]">{error}</p>}
        <button
          disabled={busy}
          className="w-full rounded-xl bg-[var(--accent)] px-4 py-3 font-semibold text-white disabled:opacity-60"
        >
          {busy ? "Creating…" : "Create account"}
        </button>
      </form>
      <p className="mt-4 text-sm text-[var(--muted)]">
        Already registered?{" "}
        <Link className="font-semibold text-[var(--accent)]" to="/login">
          Sign in
        </Link>
      </p>
    </AuthCard>
  );
}

function AuthCard({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: React.ReactNode;
}) {
  return (
    <div className="relative min-h-screen overflow-hidden bg-[var(--bg)]">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(900px_500px_at_10%_-10%,#dbeafe_0%,transparent_55%),radial-gradient(700px_400px_at_100%_0%,#e0e7ff_0%,transparent_45%)]" />
      <div className="relative mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-10">
        <div className="mb-6 flex items-center gap-3">
          <div className="grid h-11 w-11 place-items-center rounded-xl bg-[var(--accent)] text-sm font-bold text-white">
            GP
          </div>
          <div>
            <p className="text-sm font-bold tracking-wide text-[var(--text)]">GatePass</p>
            <p className="text-xs text-[var(--muted)]">Access Control System</p>
          </div>
        </div>
        <h1 className="text-3xl font-bold tracking-tight">{title}</h1>
        <p className="mt-1 text-[var(--muted)]">{subtitle}</p>
        <div className="gp-card mt-5 p-5">{children}</div>
      </div>
    </div>
  );
}

function Input(props: {
  name: string;
  label: string;
  type?: string;
  required?: boolean;
  defaultValue?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="font-medium text-[var(--muted)]">{props.label}</span>
      <input
        name={props.name}
        type={props.type ?? "text"}
        required={props.required}
        defaultValue={props.defaultValue}
        className="mt-1 w-full rounded-xl border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2.5 outline-none ring-[var(--accent)] focus:ring-2"
      />
    </label>
  );
}

function DemoAccounts() {
  return (
    <div className="mt-4 rounded-xl bg-[var(--panel-2)] p-3 text-xs text-[var(--muted)]">
      <p className="font-semibold text-[var(--text)]">Demo logins</p>
      <p>Platform: admin@gateflow.local / Admin@123</p>
      <p>Client: client@greenvalley.local / Client@123</p>
      <p>Guard: guard@greenvalley.local / Guard@123</p>
    </div>
  );
}
