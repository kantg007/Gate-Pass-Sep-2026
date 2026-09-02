import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { AppShell, RootRedirect } from "./components/AppShell";
import { ActivityPage, ReportsPage, SettingsPage } from "./pages/ActivityReportsSettings";
import { LoginPage, RegisterPage } from "./pages/AuthPages";
import { CompaniesPage, SitesPage } from "./pages/CompaniesPage";
import { DashboardPage } from "./pages/DashboardPage";
import { GuardPage } from "./pages/GuardPage";
import { MockGatePage } from "./pages/MockGatePage";
import { AlertsPage, GatesPage, HardwarePage, UsersRolesPage } from "./pages/OpsPages";
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
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="companies" element={<CompaniesPage />} />
            <Route path="admin" element={<Navigate to="/companies" replace />} />
            <Route path="sites" element={<SitesPage />} />
            <Route path="app" element={<Navigate to="/sites" replace />} />
            <Route path="sites/:siteId" element={<SiteDetailPage />} />
            <Route path="users" element={<UsersRolesPage />} />
            <Route path="hardware" element={<HardwarePage />} />
            <Route path="gates" element={<GatesPage />} />
            <Route path="activity" element={<ActivityPage />} />
            <Route path="reports" element={<ReportsPage />} />
            <Route path="alerts" element={<AlertsPage />} />
            <Route path="settings" element={<SettingsPage />} />
            <Route path="guard" element={<GuardPage />} />
            <Route path="mock-gate" element={<MockGatePage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
