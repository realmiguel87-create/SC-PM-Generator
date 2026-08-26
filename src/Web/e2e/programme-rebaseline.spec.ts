import { test, expect, type Page, type Route } from "@playwright/test";

/**
 * Creating a baseline from the Programme tab.
 *
 * The validation and effect arithmetic are unit-tested in
 * `src/features/programme/baseline.test.ts`. What those cannot establish is that the form actually
 * posts what it claims to, or that the confirmation step stands between a click and a governance
 * record being written — a two-stage flow wired straight through renders identically to one that
 * confirms properly, and every unit test still passes.
 */

const PROJECT_ID = "11111111-1111-1111-1111-111111111111";

const PROJECT = {
  id: PROJECT_ID,
  projectRef: "CP-2026-001",
  name: "Stirling Community Campus",
  status: "Active",
  currentRibaStage: 3,
  currentRibaStageName: "Spatial Coordination",
  approvedBudget: 25_000_000,
  forecastCost: 26_500_000,
  programmeName: "Learning Estate",
  description: null,
  startDate: "2026-01-01",
  targetCompletionDate: "2027-06-01",
  ribaStages: [],
};

/** 92 days adrift — the figure the confirmation step has to name before it is cleared. */
const MILESTONES = [
  {
    id: "aaaaaaaa-0000-0000-0000-000000000001",
    name: "Start on site",
    description: null,
    status: "InProgress",
    baselineDate: "2026-08-01",
    forecastDate: "2026-11-01",
    actualDate: null,
    isKeyMilestone: true,
    delayDays: 92,
  },
  {
    id: "aaaaaaaa-0000-0000-0000-000000000002",
    name: "Practical completion",
    description: null,
    status: "NotStarted",
    baselineDate: "2027-06-01",
    forecastDate: "2027-06-01",
    actualDate: null,
    isKeyMilestone: false,
    delayDays: 0,
  },
];

interface Posted {
  body: unknown;
  count: number;
}

/**
 * Serves the tab and records what the rebaseline endpoint was sent.
 *
 * `rebaselineStatus` lets a test make the server refuse: 403 is a likely real answer here, since
 * the endpoint needs approval rights rather than write rights.
 */
async function stubProject(page: Page, rebaselineStatus = 200): Promise<Posted> {
  const posted: Posted = { body: null, count: 0 };

  await page.route("**/api/**", async (route: Route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;

    if (request.method() === "POST" && path.endsWith("/baselines")) {
      posted.body = request.postDataJSON();
      posted.count += 1;
      await route.fulfill({
        status: rebaselineStatus,
        contentType: "application/json",
        body: rebaselineStatus === 200 ? '"55555555-5555-5555-5555-555555555555"' : "{}",
      });
      return;
    }

    const body = path.endsWith("/milestones")
      ? MILESTONES
      : path.endsWith("/baselines")
        ? []
        : path.endsWith(PROJECT_ID)
          ? PROJECT
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

async function openProgrammeTab(page: Page) {
  await page.goto(`/projects/${PROJECT_ID}`);
  await page.getByRole("tab", { name: "Programme" }).click();
  await expect(page.getByRole("heading", { name: "Rebaseline Programme" })).toBeVisible();
}

async function fillForm(page: Page, reason = "Tender returns came in three months later.") {
  await page.getByLabel("Baseline name").fill("Post-tender programme");
  await page.getByLabel("Reason").fill(reason);
}

test.describe("Rebaselining from the Programme tab", () => {
  test("will not let a rebaseline be reviewed without a real reason", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    await page.getByLabel("Baseline name").fill("Post-tender programme");
    await page.getByLabel("Reason").fill("update");

    // The reason is the entire record of why the sanctioned programme changed. Mirrors the
    // server's validator, so the refusal happens before a round trip rather than as a 400.
    await expect(page.getByRole("button", { name: "Review rebaseline" })).toBeDisabled();
    await expect(page.getByText(/Give a reason for the rebaseline/)).toBeVisible();
  });

  test("names the slip that is about to be cleared before anything is written", async ({ page }) => {
    const posted = await stubProject(page);
    await openProgrammeTab(page);
    await fillForm(page);

    await page.getByRole("button", { name: "Review rebaseline" }).click();

    // "Worst slip: 92d" becoming "Nothing has slipped" is the whole effect of this action, and
    // the reader has to be told before it happens rather than discover it afterwards.
    const confirm = page.getByRole("group", { name: "Confirm rebaseline" });
    await expect(confirm).toContainText("92 days");
    await expect(confirm).toContainText("Start on site");
    await expect(confirm).toContainText("Re-sanction 1 of 2 milestones");

    // Reviewing must not itself write anything.
    expect(posted.count).toBe(0);
  });

  test("posts the baseline only after it is confirmed", async ({ page }) => {
    const posted = await stubProject(page);
    await openProgrammeTab(page);
    await fillForm(page);

    await page.getByRole("button", { name: "Review rebaseline" }).click();
    await page.getByRole("button", { name: "Confirm rebaseline" }).click();

    await expect.poll(() => posted.count).toBe(1);
    expect(posted.body).toEqual({
      name: "Post-tender programme",
      reason: "Tender returns came in three months later.",
      // Null rather than absent: the approver is server-assigned and travels with this date, so
      // an omitted date has to mean "no approval recorded", not "field forgotten".
      approvedDate: null,
    });
  });

  test("sends the approval date when one is given", async ({ page }) => {
    const posted = await stubProject(page);
    await openProgrammeTab(page);
    await fillForm(page);
    await page.getByLabel("Approved on (optional)").fill("2026-09-01");

    // Said plainly before it is recorded: someone entering a committee's date should know the
    // record will name them, not the committee.
    await expect(page.getByText("This will be recorded as approved by you on that date.")).toBeVisible();

    await page.getByRole("button", { name: "Review rebaseline" }).click();
    await page.getByRole("button", { name: "Confirm rebaseline" }).click();

    await expect.poll(() => posted.body).toMatchObject({ approvedDate: "2026-09-01" });
  });

  test("cancelling writes nothing and returns to the form", async ({ page }) => {
    const posted = await stubProject(page);
    await openProgrammeTab(page);
    await fillForm(page);

    await page.getByRole("button", { name: "Review rebaseline" }).click();
    await page.getByRole("button", { name: "Cancel" }).click();

    await expect(page.getByRole("button", { name: "Review rebaseline" })).toBeVisible();
    expect(posted.count).toBe(0);
  });

  test("editing after review takes the confirmation away", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);
    await fillForm(page);

    await page.getByRole("button", { name: "Review rebaseline" }).click();
    await expect(page.getByRole("group", { name: "Confirm rebaseline" })).toBeVisible();

    await page.getByLabel("Reason").fill("A different reason entirely, longer than ten.");

    // The confirmation states figures for a specific set of inputs. Leaving it up after an edit
    // would let someone confirm a summary that no longer describes what they are about to submit.
    await expect(page.getByRole("group", { name: "Confirm rebaseline" })).toBeHidden();
  });

  test("explains a refusal for want of approval rights", async ({ page }) => {
    await stubProject(page, 403);
    await openProgrammeTab(page);
    await fillForm(page);

    await page.getByRole("button", { name: "Review rebaseline" }).click();
    await page.getByRole("button", { name: "Confirm rebaseline" }).click();

    // This endpoint needs CanApprove, not CanWrite, so 403 is a likely real answer — and one a
    // generic "request failed" would leave a user unable to act on.
    await expect(page.getByText(/needs approval rights/)).toBeVisible();
  });

  test("offers no rebaseline on a project with no milestones", async ({ page }) => {
    await page.route("**/api/**", async (route: Route) => {
      const path = new URL(route.request().url()).pathname;
      const body = path.endsWith("/milestones") ? [] : path.endsWith("/baselines") ? [] : PROJECT;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(body),
      });
    });

    await page.goto(`/projects/${PROJECT_ID}`);
    await page.getByRole("tab", { name: "Programme" }).click();

    // A baseline with no dates in it sanctions nothing. Offering the control presents a governance
    // act that cannot be performed as one that simply has not been.
    await expect(page.getByRole("heading", { name: "Rebaseline Programme" })).toHaveCount(0);
  });
});
