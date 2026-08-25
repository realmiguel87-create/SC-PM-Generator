import { test, expect, type Page, type ConsoleMessage } from "@playwright/test";

const NAV_ITEMS = [
  { label: "Executive Dashboard", heading: /dashboard/i },
  { label: "Projects", heading: /projects/i },
  { label: "Governance", heading: /governance/i },
  { label: "Reporting Centre", heading: /reporting/i },
];

/** Console noise from every /api/* call failing is expected here and is not itself a bug: no
 * backend is running for this smoke test (frontend-only, see playwright.config.ts), and even a
 * live one would 401 every request since MSAL auth isn't wired into the frontend yet (see
 * src/lib/api-client.ts). Vite's dev/preview proxy reports an unreachable backend as a
 * "Failed to load resource" browser error with a 404/500/502/503 status, not a fetch()
 * rejection, so that's what's actually filtered here — a real broken build asset would show the
 * same message shape but this app has no non-API resources that 404 in a working build, so this
 * stays a meaningful signal rather than a blanket suppression. */
function isExpectedNetworkNoise(text: string): boolean {
  return (
    /failed to fetch|401|unauthorized|networkerror/i.test(text) ||
    /failed to load resource.*(404|500|502|503)/i.test(text)
  );
}

test.describe("App shell smoke test", () => {
  test("boots and every top-level route renders and is navigable without an uncaught error", async ({
    page,
  }) => {
    const pageErrors: Error[] = [];
    const unexpectedConsoleErrors: string[] = [];

    page.on("pageerror", (error) => pageErrors.push(error));
    page.on("console", (message: ConsoleMessage) => {
      if (message.type() === "error" && !isExpectedNetworkNoise(message.text())) {
        unexpectedConsoleErrors.push(message.text());
      }
    });

    await page.goto("/");
    await expect(page.getByText("Stirling Council")).toBeVisible();

    for (const { label } of NAV_ITEMS) {
      await navigateTo(page, label);
    }

    expect(pageErrors, `Uncaught page errors: ${pageErrors.map((e) => e.message).join("; ")}`).toHaveLength(0);
    expect(
      unexpectedConsoleErrors,
      `Unexpected console errors: ${unexpectedConsoleErrors.join("; ")}`,
    ).toHaveLength(0);
  });

  // Client-side validation needs no backend, so unlike the submit path it is genuinely testable
  // here. Worth covering: these rules mirror CreateProjectCommandValidator on the server, and a
  // silent drift between the two turns a helpful inline message into an opaque 400.
  test("the new project form validates required fields before submitting", async ({ page }) => {
    await page.goto("/projects");

    await page.getByRole("button", { name: "New project" }).click();
    await expect(page.getByRole("heading", { name: "New project" })).toBeVisible();

    // Submitting empty should surface required-field errors, not a network call.
    await page.getByRole("button", { name: "Create project" }).click();
    await expect(page.getByText("Project reference is required.")).toBeVisible();
    await expect(page.getByText("Project name is required.")).toBeVisible();

    // Over-length project reference: the server caps this at 20 characters.
    await page.getByLabel("Project reference").fill("X".repeat(21));
    await page.getByLabel("Project name").fill("Test project");
    await page.getByRole("button", { name: "Create project" }).click();
    await expect(page.getByText("Must be 20 characters or fewer.")).toBeVisible();

    // Completion date before start date — the one cross-field rule the server enforces.
    await page.getByLabel("Project reference").fill("CP-2026-001");
    await page.getByLabel("Start date").fill("2026-06-01");
    await page.getByLabel("Target completion").fill("2026-01-01");
    await page.getByRole("button", { name: "Create project" }).click();
    await expect(page.getByText("Must be on or after the start date.")).toBeVisible();

    await page.getByRole("button", { name: "Cancel" }).click();
    await expect(page.getByRole("heading", { name: "New project" })).not.toBeVisible();
  });
});

async function navigateTo(page: Page, label: string) {
  await page.getByRole("link", { name: label }).click();
  // The nav item itself becoming the active link is a route-change signal that doesn't depend
  // on any particular page's content (which varies — some pages 401 into an error state, by
  // design, since there's no auth token yet). That's what actually proves navigation worked.
  await expect(page.getByRole("link", { name: label })).toHaveClass(/text-stirling-purple/);
}
