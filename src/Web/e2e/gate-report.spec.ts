import { test, expect, type Page, type Route } from "@playwright/test";

/**
 * Creating a stage gate approval report from the Reports tab.
 *
 * Until this existed the report could not be created from the browser at all: the API required a
 * gatewayId and nothing in the UI supplied one, so the only route to the document was the API
 * directly. That was deliberate — a gate report without its gateway is a decision document about
 * no decision — but it left the feature unusable.
 *
 * What these cover is the request the UI builds: that a gateway can be chosen, that it reaches
 * the server in the payload, that the button is held until one is, and that no other report type
 * carries one. The server rejects a mismatched payload either way; the point here is that a user
 * never reaches that rejection.
 *
 * Responses are stubbed at the browser's network layer, as in api-backed.spec.ts — see that file
 * for why, and for what it deliberately does not establish.
 */

const PROJECT_ID = "11111111-1111-1111-1111-111111111111";
const PENDING_GATEWAY = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const DECIDED_GATEWAY = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const PROJECT = {
  id: PROJECT_ID,
  projectRef: "ID00001",
  name: "Bridgeview Primary School",
  status: "Active",
  currentRibaStage: 2,
  currentRibaStageName: "Concept Design",
  approvedBudget: 412_500,
  forecastCost: 418_200,
  ribaStages: [
    {
      id: "s0", stageNumber: 0, stageName: "Strategic Definition", status: "Complete",
      plannedStartDate: null, plannedEndDate: null, actualStartDate: null, actualEndDate: null,
      // Decided, so the "decide" action is gone but a report can still be written against it.
      gatewayId: DECIDED_GATEWAY, pendingGatewayId: null, gatewayStatus: "Approved",
    },
    {
      id: "s1", stageNumber: 1, stageName: "Preparation and Briefing", status: "Complete",
      plannedStartDate: null, plannedEndDate: null, actualStartDate: null, actualEndDate: null,
      // No gateway was ever raised on this stage, so it must not be offered.
      gatewayId: null, pendingGatewayId: null, gatewayStatus: null,
    },
    {
      id: "s2", stageNumber: 2, stageName: "Concept Design", status: "InProgress",
      plannedStartDate: null, plannedEndDate: null, actualStartDate: null, actualEndDate: null,
      gatewayId: PENDING_GATEWAY, pendingGatewayId: PENDING_GATEWAY, gatewayStatus: "Pending",
    },
  ],
};

interface Posted {
  body: Record<string, unknown> | null;
  count: number;
}

async function stub(page: Page, stages = PROJECT.ribaStages): Promise<Posted> {
  const posted: Posted = { body: null, count: 0 };

  await page.route("**/api/**", async (route: Route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;

    if (request.method() === "POST" && path.endsWith("/committee-reports")) {
      posted.body = request.postDataJSON();
      posted.count += 1;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: '"99999999-9999-9999-9999-999999999999"',
      });
      return;
    }

    const body = path.endsWith("/committee-reports")
      ? []
      : path.endsWith(PROJECT_ID)
        ? { ...PROJECT, ribaStages: stages }
        : null;

    if (body === null) {
      await route.fulfill({ status: 501, contentType: "application/json", body: "{}" });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });

  return posted;
}

async function openReportsTab(page: Page) {
  await page.goto(`/projects/${PROJECT_ID}`);
  await page.getByRole("tab", { name: "Reports" }).click();
  await expect(page.getByRole("heading", { name: "New Report" })).toBeVisible();
}

test.describe("Creating a stage gate approval report", () => {
  test("offers the document by name rather than by enum", async ({ page }) => {
    await stub(page);
    await openReportsTab(page);

    // "StageGateApproval" in a dropdown is a leaked identifier, the same defect as printing
    // "ApprovedWithConditions" on a council document.
    await expect(
      page.getByRole("option", { name: "Stage Gate Approval Report" }),
    ).toHaveCount(1);
    await expect(page.getByRole("option", { name: "Project Mandate" })).toHaveCount(1);
  });

  test("shows the gateway picker only for a gate report", async ({ page }) => {
    await stub(page);
    await openReportsTab(page);

    await expect(page.getByLabel("Gateway")).toBeHidden();

    await page.getByLabel("Report type").selectOption("StageGateApproval");
    await expect(page.getByLabel("Gateway")).toBeVisible();

    // Switching away hides it again — and clears it, so a gateway cannot be carried onto a type
    // the server rejects one on.
    await page.getByLabel("Report type").selectOption("CommitteeReport");
    await expect(page.getByLabel("Gateway")).toBeHidden();
  });

  test("lists every stage that has a gateway, decided or not", async ({ page }) => {
    await stub(page);
    await openReportsTab(page);
    await page.getByLabel("Report type").selectOption("StageGateApproval");

    const gateway = page.getByLabel("Gateway");

    // A report is normally written before the decision, and sometimes after it to record one, so
    // a decided gateway stays available. The stage with no gateway at all does not.
    await expect(gateway.getByRole("option")).toHaveCount(3); // placeholder + two gateways
    await expect(
      gateway.getByRole("option", { name: "Stage 2 — Concept Design (Pending)" }),
    ).toHaveCount(1);
    await expect(
      gateway.getByRole("option", { name: "Stage 0 — Strategic Definition (Approved)" }),
    ).toHaveCount(1);
    await expect(
      gateway.getByRole("option", { name: /Preparation and Briefing/ }),
    ).toHaveCount(0);
  });

  test("holds the button until a gateway is chosen", async ({ page }) => {
    await stub(page);
    await openReportsTab(page);

    await page.getByLabel("Report type").selectOption("StageGateApproval");
    await page.getByLabel("Title").fill("Stage 2 Gate");

    // The server would reject this, so the reason is shown here rather than as a failed request.
    await expect(page.getByRole("button", { name: "Generate Draft" })).toBeDisabled();

    await page.getByLabel("Gateway").selectOption(PENDING_GATEWAY);
    await expect(page.getByRole("button", { name: "Generate Draft" })).toBeEnabled();
  });

  test("sends the chosen gateway to the server", async ({ page }) => {
    const posted = await stub(page);
    await openReportsTab(page);

    await page.getByLabel("Report type").selectOption("StageGateApproval");
    await page.getByLabel("Title").fill("Stage 2 Gate");
    await page.getByLabel("Gateway").selectOption(PENDING_GATEWAY);
    await page.getByRole("button", { name: "Generate Draft" }).click();

    await expect.poll(() => posted.count).toBe(1);
    expect(posted.body).toMatchObject({
      reportType: "StageGateApproval",
      title: "Stage 2 Gate",
      gatewayId: PENDING_GATEWAY,
    });
  });

  test("sends no gateway for any other report type", async ({ page }) => {
    const posted = await stub(page);
    await openReportsTab(page);

    await page.getByLabel("Report type").selectOption("ProjectMandate");
    await page.getByLabel("Title").fill("Mandate");
    await page.getByRole("button", { name: "Generate Draft" }).click();

    await expect.poll(() => posted.count).toBe(1);

    // The server rejects a gateway on any other type, so sending one would be a failed request
    // rather than a wrong document — but it would be a failed request a user could not avoid.
    expect(posted.body).not.toHaveProperty("gatewayId", PENDING_GATEWAY);
    expect((posted.body ?? {}).gatewayId ?? null).toBeNull();
  });

  test("explains itself when the project has no gateways", async ({ page }) => {
    await stub(page, [PROJECT.ribaStages[1]]);
    await openReportsTab(page);

    await page.getByLabel("Report type").selectOption("StageGateApproval");

    // A disabled control with no explanation is the worst of both: the user cannot proceed and
    // cannot tell why, nor what would fix it.
    await expect(page.getByLabel("Gateway")).toBeDisabled();
    await expect(page.getByText(/Raise one on the Governance tab first/)).toBeVisible();
    await expect(page.getByRole("button", { name: "Generate Draft" })).toBeDisabled();
  });
});
