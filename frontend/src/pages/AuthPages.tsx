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
          className="w-full rounded-lg bg-[var(--accent)] px-4 py-3 font-semibold text-[#1a1408] disabled:opacity-60"
        >
          {busy ? "Signing in…" : "Sign in"}
        </button>
      </form>
      <p className="mt-4 text-sm text-[var(--muted)]">
        New society / company?{" "}
        <Link className="text-[var(--accent)]" to="/register">
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
          className="w-full rounded-lg bg-[var(--accent)] px-4 py-3 font-semibold text-[#1a1408] disabled:opacity-60"
        >
          {busy ? "Creating…" : "Create account"}
        </button>
      </form>
      <p className="mt-4 text-sm text-[var(--muted)]">
        Already registered?{" "}
        <Link className="text-[var(--accent)]" to="/login">
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
    <div className="mx-auto max-w-md space-y-4 py-10">
      <div>
        <p className="font-[family-name:var(--display)] text-sm font-bold text-[var(--accent)]">
          GateFlow
        </p>
        <h1 className="mt-1 font-[family-name:var(--display)] text-3xl font-bold">{title}</h1>
        <p className="text-[var(--muted)]">{subtitle}</p>
      </div>
      <div className="rounded-xl border border-[var(--line)] bg-[var(--panel)] p-5">{children}</div>
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
      <span className="text-[var(--muted)]">{props.label}</span>
      <input
        name={props.name}
        type={props.type ?? "text"}
        required={props.required}
        defaultValue={props.defaultValue}
        className="mt-1 w-full rounded-lg border border-[var(--line)] bg-[var(--panel-2)] px-3 py-2 outline-none ring-[var(--accent)] focus:ring-1"
      />
    </label>
  );
}

function DemoAccounts() {
  return (
    <div className="mt-4 rounded-lg bg-[var(--panel-2)] p-3 text-xs text-[var(--muted)]">
      <p className="font-medium text-[var(--text)]">Demo logins</p>
      <p>Platform: admin@gateflow.local / Admin@123</p>
      <p>Client: client@greenvalley.local / Client@123</p>
      <p>Guard: guard@greenvalley.local / Guard@123</p>
    </div>
  );
}
