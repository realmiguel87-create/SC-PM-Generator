import { test, expect, type Page, type Route } from "@playwright/test";

/**
 * What the app does once the API answers.
 *
 * The existing smoke test (smoke.spec.ts) proves the app boots and navigates while every data
 * fetch is failing — the condition it is in with no backend. That leaves the more interesting
 * half untested: whether a successful response actually renders correctly, whether the derived
 * figures on the dashboard are derived correctly, whether the create-project request carries the
 * payload the API expects, and whether a 401 and a 403 produce different messages.
 *
 * Responses are stubbed at the browser's network layer rather than served by a running API. That
 * is a deliberate trade and worth being explicit about, because it is not what "authenticated
 * end-to-end" would usually mean:
 *
 *   - It does NOT exercise a real Entra ID token, the API's JWT validation, EntraClaimsTransformation,
 *     or any RequireRole policy. Those are covered by SCPM.IntegrationTests, which drives the real
 *     ASP.NET Core pipeline against a real SQL Server with a test auth handler — a better place to
 *     test them than a browser, and already done.
 *   - It DOES exercise everything from the fetch response inwards: TanStack Query, the api-client's
 *     error translation, every component's rendering, and the derived values.
 *
 * The two ways to make this genuinely token-authenticated were both rejected. Scripting a real
 * interactive Microsoft login is slow and breaks whenever Microsoft changes their login page.
 * Adding a build-time auth bypass to api-client puts a switch in production code whose only job
 * is to skip authentication — not something to carry in a governance system for the sake of a
 * test. See docs/roadmap.md for what that leaves uncovered.
 */

const PROJECTS = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    projectRef: "CP-2026-001",
    name: "Stirling Community Campus",
    status: "Active",
    currentRibaStage: 3,
    currentRibaStageName: "Spatial Coordination",
    approvedBudget: 25_000_000,
    forecastCost: 26_500_000,
    programmeName: "Learning Estate",
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    projectRef: "CP-2026-002",
    name: "Bridge Refurbishment",
    status: "OnHold",
    currentRibaStage: 5,
    currentRibaStageName: "Manufacturing and Construction",
    approvedBudget: 8_500_000,
    forecastCost: 8_000_000,
    programmeName: null,
  },
];

/**
 * Intercepts every /api/* call. Unmatched paths get an explicit 501 rather than being allowed
 * through to the preview server, which would 404 them — a stub that silently falls through is
 * how a test ends up asserting against an error state it did not intend to create.
 */
async function stubApi(
  page: Page,
  handlers: Array<{ method?: string; path: RegExp; handle: (route: Route) => Promise<void> }>,
) {
  await page.route("**/api/**", async (route) => {
    const url = new URL(route.request().url());
    const method = route.request().method();

    const match = handlers.find(
      (h) => h.path.test(url.pathname) && (!h.method || h.method === method),
    );

    if (!match) {
      await route.fulfill({
        status: 501,
        contentType: "application/json",
        body: JSON.stringify({ title: `No stub for ${method} ${url.pathname}` }),
      });
      return;
    }

    await match.handle(route);
  });
}

function json(body: unknown, status = 200) {
  return async (route: Route) =>
    route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}

/** A ProblemDetails-shaped error, which is what the API actually returns and what api-client reads. */
function problem(status: number, title: string) {
  return async (route: Route) =>
    route.fulfill({
      status,
      contentType: "application/problem+json",
      body: JSON.stringify({ status, title }),
    });
}

/**
 * The value inside a StatTile, located via its label. The tile's label is a CardTitle (an h3),
 * and the value is the only <p> in the same Card — going through the heading rather than
 * matching the value text means a wrong value fails the assertion instead of finding nothing.
 */
function statTile(page: Page, label: string) {
  return page
    .locator("div.rounded-lg")
    .filter({ has: page.getByRole("heading", { name: label, exact: true }) })
    .locator("p");
}

test.describe("Rendering real API responses", () => {
  test("the dashboard derives its totals from the project list", async ({ page }) => {
    await stubApi(page, [{ path: /\/api\/projects$/, handle: json(PROJECTS) }]);

    await page.goto("/");

    await expect(statTile(page, "Total Projects")).toHaveText("2");
    // 25,000,000 + 8,500,000. Asserting the sum rather than either input is the point: this is
    // the one figure on the page the frontend computes rather than displays.
    await expect(statTile(page, "Capital Value")).toHaveText("£33,500,000");
    await expect(statTile(page, "Forecast Cost")).toHaveText("£34,500,000");

    // No error notice when the request succeeded.
    await expect(page.getByText(/could not reach the api/i)).toHaveCount(0);
  });

  test("the projects list renders a card per project", async ({ page }) => {
    await stubApi(page, [{ path: /\/api\/projects$/, handle: json(PROJECTS) }]);

    await page.goto("/projects");

    await expect(page.getByText("CP-2026-001")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Stirling Community Campus" })).toBeVisible();
    await expect(page.getByText("Stage 3 — Spatial Coordination")).toBeVisible();
    await expect(page.getByText("£25,000,000")).toBeVisible();

    await expect(page.getByText("CP-2026-002")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Bridge Refurbishment" })).toBeVisible();

    // The empty-state copy must not appear alongside actual projects.
    await expect(page.getByText(/no projects yet/i)).toHaveCount(0);
  });

  test("an empty list shows the empty state rather than an error", async ({ page }) => {
    await stubApi(page, [{ path: /\/api\/projects$/, handle: json([]) }]);

    await page.goto("/projects");

    // Zero projects is a successful response, not a failure — a regression that conflated the
    // two would show an error notice on a brand-new installation.
    await expect(page.getByText(/no projects yet/i)).toBeVisible();
    await expect(page.getByText(/could not reach the api/i)).toHaveCount(0);
  });
});

test.describe("Error states are distinguished", () => {
  test("401 reports an expired session, not an unreachable API", async ({ page }) => {
    await stubApi(page, [{ path: /\/api\/projects$/, handle: problem(401, "Unauthorized") }]);

    await page.goto("/projects");

    // The specific regression this guards: the previous notice claimed the API was down on any
    // failure, and did exactly this during a real setup session while the API was running fine.
    await expect(page.getByText(/not signed in, or your session has expired/i)).toBeVisible();
    await expect(page.getByText(/could not reach the api/i)).toHaveCount(0);
  });

  test("403 reports a missing role, not a sign-in problem", async ({ page }) => {
    await stubApi(page, [{ path: /\/api\/projects$/, handle: problem(403, "Forbidden") }]);

    await page.goto("/projects");

    await expect(page.getByText(/does not have a role permitting this/i)).toBeVisible();
    await expect(page.getByText(/not signed in/i)).toHaveCount(0);
  });

  test("an unreachable API reports exactly that", async ({ page }) => {
    // route.abort() makes fetch() reject with a TypeError, which is how a genuinely unreachable
    // server presents — distinct from any HTTP status, and the only case where the notice's
    // "check the API is running" advice is actually correct.
    await stubApi(page, [{ path: /\/api\/projects$/, handle: async (route) => route.abort("failed") }]);

    await page.goto("/projects");

    await expect(page.getByText(/could not reach the api/i)).toBeVisible();
  });
});

test.describe("Creating a project", () => {
  test("sends the validated payload and shows the new project on success", async ({ page }) => {
    let createdBody: Record<string, unknown> | null = null;
    let listCallCount = 0;

    await stubApi(page, [
      {
        method: "POST",
        path: /\/api\/projects$/,
        handle: async (route) => {
          createdBody = route.request().postDataJSON();
          await route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify("33333333-3333-3333-3333-333333333333"),
          });
        },
      },
      {
        method: "GET",
        path: /\/api\/projects$/,
        handle: async (route) => {
          listCallCount += 1;
          // Empty first, populated after the create — so the assertion below proves the list was
          // actually refetched rather than that the fixture always contained the project.
          const body = listCallCount === 1 ? [] : [PROJECTS[0]];
          await route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify(body),
          });
        },
      },
    ]);

    await page.goto("/projects");
    await expect(page.getByText(/no projects yet/i)).toBeVisible();

    await page.getByRole("button", { name: "New project" }).click();
    await page.getByLabel("Project reference").fill("CP-2026-001");
    await page.getByLabel("Project name").fill("Stirling Community Campus");
    await page.getByLabel("Approved budget (£)").fill("25000000");
    await page.getByRole("button", { name: "Create project" }).click();

    // The form closes on success, and the invalidated query refetches.
    await expect(page.getByRole("heading", { name: "New project" })).toBeHidden();
    await expect(page.getByRole("heading", { name: "Stirling Community Campus" })).toBeVisible();

    expect(createdBody).not.toBeNull();
    expect(createdBody).toMatchObject({
      projectRef: "CP-2026-001",
      name: "Stirling Community Campus",
      approvedBudget: 25_000_000,
    });

    // Optional fields left blank must be omitted, not sent as "". The API binds an empty string
    // for a date to a validation failure rather than to null, so this is the difference between
    // a working create and an opaque 400 — and it is invisible in the UI either way.
    expect(createdBody).not.toHaveProperty("startDate");
    expect(createdBody).not.toHaveProperty("targetCompletionDate");
    expect(createdBody).not.toHaveProperty("description");
  });

  test("a rejected create surfaces the API's message and keeps the form open", async ({ page }) => {
    await stubApi(page, [
      { method: "GET", path: /\/api\/projects$/, handle: json([]) },
      {
        method: "POST",
        path: /\/api\/projects$/,
        handle: problem(403, "Forbidden"),
      },
    ]);

    await page.goto("/projects");
    await page.getByRole("button", { name: "New project" }).click();
    await page.getByLabel("Project reference").fill("CP-2026-009");
    await page.getByLabel("Project name").fill("Rejected project");
    await page.getByRole("button", { name: "Create project" }).click();

    // A 403 on create is a permissions problem specifically, and the form says so rather than
    // repeating the generic failure text — the same distinction ApiErrorNotice draws for reads.
    await expect(
      page.getByText(/does not have permission to create projects/i),
    ).toBeVisible();

    // Losing the typed values on a permissions failure would mean re-entering the whole form
    // for a problem the user cannot fix by retrying.
    await expect(page.getByRole("heading", { name: "New project" })).toBeVisible();
    await expect(page.getByLabel("Project reference")).toHaveValue("CP-2026-009");
  });
});
