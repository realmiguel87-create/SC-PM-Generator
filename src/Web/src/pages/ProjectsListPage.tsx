import { useState } from "react";
import { Link } from "react-router-dom";
import { Plus } from "lucide-react";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { ApiErrorNotice } from "@/components/ApiErrorNotice";
import { NewProjectForm } from "@/features/projects/NewProjectForm";
import { useProjects } from "@/features/projects/api";
import { formatCurrency } from "@/lib/utils";

export function ProjectsListPage() {
  const { data: projects, isLoading, isError, error } = useProjects();
  const [isCreating, setIsCreating] = useState(false);

  return (
    <div className="flex flex-col gap-6 p-6">
      <header className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold">Projects</h1>
          <p className="text-sm text-text-secondary">All active and closed capital projects.</p>
        </div>
        {!isCreating && (
          <Button size="sm" onClick={() => setIsCreating(true)}>
            <Plus size={14} />
            New project
          </Button>
        )}
      </header>

      {isCreating && <NewProjectForm onClose={() => setIsCreating(false)} />}

      {isLoading && <p className="text-sm text-text-secondary">Loading projects…</p>}
      {isError && <ApiErrorNotice error={error} />}

      {projects && projects.length === 0 && !isCreating && (
        <Card>
          <CardContent className="pt-5 text-sm text-text-secondary">
            No projects yet. Use <strong>New project</strong> to add the first one.
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        {projects?.map((project) => (
          <Link key={project.id} to={`/projects/${project.id}`}>
            <Card className="transition-shadow hover:shadow-md">
              <CardContent className="flex flex-col gap-2 pt-5">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-medium text-text-secondary">{project.projectRef}</span>
                  <Badge variant={statusToBadgeVariant(project.status)}>{project.status}</Badge>
                </div>
                <h3 className="text-base font-semibold">{project.name}</h3>
                <div className="flex items-center justify-between text-sm text-text-secondary">
                  <span>
                    Stage {project.currentRibaStage} — {project.currentRibaStageName}
                  </span>
                  <span>{formatCurrency(project.approvedBudget)}</span>
                </div>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
