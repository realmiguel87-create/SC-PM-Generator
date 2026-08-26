import { test, expect, type Page, type Route } from "@playwright/test";

/**
 * Choosing which sanctioned programme the timeline is measured against.
 *
 * The arithmetic is unit-tested in `src/features/programme/baseline.test.ts`. What those cannot
 * establish is that selecting a baseline actually reaches the endpoint and changes what is drawn —
 * a selector wired to nothing renders identically to one wired correctly, and every unit test
 * still passes.
 *
 * The scenario throughout: a project rebaselined once. Against the live programme nothing has
 * slipped; against revision 1 — the dates that were actually approved — it is 92 days adrift.
 * Telling those two apart is the entire feature.
 */

const PROJECT_ID = "11111111-1111-1111-1111-111111111111";
const ORIGINAL_BASELINE_ID = "22222222-2222-2222-2222-222222222222";
const CURRENT_BASELINE_ID = "33333333-3333-3333-3333-333333333333";

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

/** The live programme: rebaselined, so everything reads as on time. */
const MILESTONES = [
  {
    id: "aaaaaaaa-0000-0000-0000-000000000001",
    name: "Start on site",
    description: null,
    status: "InProgress",
    baselineDate: "2026-11-01",
    forecastDate: "2026-11-01",
    actualDate: null,
    isKeyMilestone: true,
    delayDays: 0,
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

const BASELINES = [
  {
    id: CURRENT_BASELINE_ID,
    revision: 2,
    name: "Post-tender programme",
    reason: "Tender returns came in three months later than programmed.",
    approvedBy: "44444444-4444-4444-4444-444444444444",
    approvedDate: "2026-09-01",
    isCurrent: true,
    createdDate: "2026-09-01T00:00:00Z",
    milestoneCount: 2,
  },
  {
    id: ORIGINAL_BASELINE_ID,
    revision: 1,
    name: "Original baseline",
    reason: "Captured automatically when the programme was first rebaselined.",
    approvedBy: null,
    approvedDate: null,
    isCurrent: false,
    createdDate: "2026-09-01T00:00:00Z",
    milestoneCount: 2,
  },
];

/** Against revision 1, Start on site is 92 days adrift and a milestone has since been added. */
const ORIGINAL_COMPARISON = {
  baseline: BASELINES[1],
  milestones: [
    {
      milestoneId: "aaaaaaaa-0000-0000-0000-000000000001",
      name: "Start on site",
      baselineName: "Site commencement",
      baselineDate: "2026-08-01",
      currentDate: "2026-11-01",
      currentDateIsActual: false,
      slipDays: 92,
      isKeyMilestone: true,
      addedSinceBaseline: false,
    },
    {
      milestoneId: "aaaaaaaa-0000-0000-0000-000000000002",
      name: "Practical completion",
      baselineName: "Practical completion",
      baselineDate: null,
      currentDate: "2027-06-01",
      currentDateIsActual: false,
      slipDays: 0,
      isKeyMilestone: false,
      addedSinceBaseline: true,
    },
  ],
  worstSlipDays: 92,
  worstSlipMilestone: "Start on site",
  removedSinceBaseline: ["Enabling works"],
};

async function stubProject(page: Page, baselines: unknown[] = BASELINES) {
  await page.route("**/api/**", async (route: Route) => {
    const url = new URL(route.request().url());
    const path = url.pathname;

    const body = path.endsWith("/milestones")
      ? MILESTONES
      : path.endsWith("/baselines")
        ? baselines
        : path.endsWith("/baseline-comparison")
          ? url.searchParams.get("baselineId") === ORIGINAL_BASELINE_ID
            ? ORIGINAL_COMPARISON
            : null
          : path.endsWith(PROJECT_ID)
            ? PROJECT
            : null;

    if (body === null) {
      // A stub that falls through silently is how a test ends up asserting against an error state
      // it did not intend to create.
      await route.fulfill({ status: 501, contentType: "application/json", body: "{}" });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });
}

async function openProgrammeTab(page: Page) {
  await page.goto(`/projects/${PROJECT_ID}`);
  await page.getByRole("tab", { name: "Programme" }).click();
  await expect(page.getByRole("heading", { name: "Programme Timeline" })).toBeVisible();
}

test.describe("Baseline selector", () => {
  test("lists the sanctioned programmes, newest first, marking the current one", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    const options = await page.getByLabel("Measure against").locator("option").allTextContents();

    expect(options).toEqual([
      "Live programme",
      "Rev 2: Post-tender programme — current",
      "Rev 1: Original baseline",
    ]);
  });

  test("shows no slip against the live programme", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    // The project has been rebaselined, so by its current measure it is bang on programme. This
    // is the reading the next test has to differ from.
    await expect(page.getByText("Nothing has slipped")).toBeVisible();
  });

  test("measures against a superseded baseline when one is chosen", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    await page.getByLabel("Measure against").selectOption(ORIGINAL_BASELINE_ID);

    // 92 days adrift of the programme that was actually approved, while the live figure above
    // reads zero. A selector wired to nothing would leave "Nothing has slipped" on screen.
    //
    // Scoped to the worst-slip tile: the key-milestone tile legitimately reads 92d too, since
    // Start on site is a key milestone, and matching on text alone finds both.
    const worstSlip = page.getByRole("group", { name: "Worst slip" });
    await expect(worstSlip).toContainText("92d");
    // "Site commencement", not "Start on site": the comparison is labelled throughout with the
    // names the milestones carried when the baseline was sanctioned. A headline naming today's
    // milestone against a programme that knew it by another name would be quietly inconsistent
    // with the chart directly below it.
    await expect(worstSlip).toContainText("Site commencement");
    await expect(page.getByText("Nothing has slipped")).toBeHidden();
  });

  test("labels the chart with the names the milestones had when sanctioned", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    await page.getByLabel("Measure against").selectOption(ORIGINAL_BASELINE_ID);

    // The milestone is called "Start on site" today and was "Site commencement" when approved.
    // A chart of the old programme labelled with today's names is a chart of neither.
    const ticks = await page.locator(".recharts-yAxis text").allTextContents();
    expect(ticks.join(" ")).toMatch(/Site\s*commencement/);
  });

  test("names what was added and removed rather than drawing it as slip", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    await page.getByLabel("Measure against").selectOption(ORIGINAL_BASELINE_ID);

    // Matched on the paragraph, not the bold lead-in: getByText resolves to the innermost element
    // containing the string, which here is the <span> holding the label and none of the names.
    await expect(page.locator("p", { hasText: /Added since this baseline:/ }))
      .toContainText("Practical completion");
    await expect(page.locator("p", { hasText: /Removed since this baseline:/ }))
      .toContainText("Enabling works");

    // Added milestones are excluded from the chart: there is no sanctioned date to measure them
    // against, and a zero-length bar would read as "on time".
    const bars = page.locator(".milestone-slip-bar .recharts-bar-rectangle");
    await expect(bars).toHaveCount(1);
  });

  test("shows why the superseded baseline was replaced", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    await page.getByLabel("Measure against").selectOption(ORIGINAL_BASELINE_ID);

    // A reader looking at 92 days of slip needs what was said at the time about why the
    // programme was replaced, not a separate register to go and find it in.
    await expect(page.getByText(/Captured automatically when the programme was first rebaselined/))
      .toBeVisible();
  });

  test("returns to the live programme when the selection is cleared", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    await page.getByLabel("Measure against").selectOption(ORIGINAL_BASELINE_ID);
    await expect(page.getByRole("group", { name: "Worst slip" })).toContainText("92d");

    await page.getByLabel("Measure against").selectOption("");
    await expect(page.getByText("Nothing has slipped")).toBeVisible();
  });

  test("hides the selector entirely on a project that has never been rebaselined", async ({ page }) => {
    await stubProject(page, []);
    await openProgrammeTab(page);

    // An empty dropdown presents a control that cannot do anything as one that has not been used.
    await expect(page.getByLabel("Measure against")).toHaveCount(0);
    await expect(page.getByRole("heading", { name: "Programme Timeline" })).toBeVisible();
  });
});
