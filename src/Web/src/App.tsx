import { Route, Routes } from "react-router-dom";
import { AppShell } from "@/app/AppShell";
import { DashboardPage } from "@/pages/DashboardPage";
import { ProjectsListPage } from "@/pages/ProjectsListPage";
import { ProjectWorkspacePage } from "@/pages/ProjectWorkspacePage";
import { PlaceholderPage } from "@/pages/PlaceholderPage";
import { ReportingCentrePage } from "@/pages/ReportingCentrePage";

export function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<DashboardPage />} />
        <Route path="projects" element={<ProjectsListPage />} />
        <Route path="projects/:projectId" element={<ProjectWorkspacePage />} />
        <Route
          path="governance"
          element={<PlaceholderPage title="Governance" phase="Phase 2" />}
        />
        <Route path="reporting" element={<ReportingCentrePage />} />
      </Route>
    </Routes>
  );
}
