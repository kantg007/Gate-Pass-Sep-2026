import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/AppShell";
import { GuardPage } from "./pages/GuardPage";
import { MockGatePage } from "./pages/MockGatePage";
import { SiteDetailPage } from "./pages/SiteDetailPage";
import { SitesPage } from "./pages/SitesPage";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={<SitesPage />} />
          <Route path="sites/:siteId" element={<SiteDetailPage />} />
          <Route path="guard" element={<GuardPage />} />
          <Route path="mock-gate" element={<MockGatePage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
