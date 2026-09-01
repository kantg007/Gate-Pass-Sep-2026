import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { AppShell, RootRedirect } from "./components/AppShell";
import { AdminDashboardPage } from "./pages/AdminDashboardPage";
import { LoginPage, RegisterPage } from "./pages/AuthPages";
import { ClientDashboardPage } from "./pages/ClientDashboardPage";
import { GuardPage } from "./pages/GuardPage";
import { MockGatePage } from "./pages/MockGatePage";
import { SiteDetailPage } from "./pages/SiteDetailPage";

function PublicOnly({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  if (user) return <RootRedirect />;
  return children;
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route
            path="/login"
            element={
              <PublicOnly>
                <LoginPage />
              </PublicOnly>
            }
          />
          <Route
            path="/register"
            element={
              <PublicOnly>
                <RegisterPage />
              </PublicOnly>
            }
          />
          <Route element={<AppShell />}>
            <Route index element={<RootRedirect />} />
            <Route path="admin" element={<AdminDashboardPage />} />
            <Route path="app" element={<ClientDashboardPage />} />
            <Route path="sites/:siteId" element={<SiteDetailPage />} />
            <Route path="guard" element={<GuardPage />} />
            <Route path="mock-gate" element={<MockGatePage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
