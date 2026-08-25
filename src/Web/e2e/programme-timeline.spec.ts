import { test, expect, type Page, type Route } from "@playwright/test";

/**
 * The programme timeline, rendered in a real browser against stubbed milestone data.
 *
 * The arithmetic behind this chart is unit-tested in `src/features/programme/timeline.test.ts`,
 * which is where the things that can be wrong actually live. What that cannot establish is
 * whether Recharts draws anything at all: a chart with correct data and a broken axis domain
 * renders an empty box, and every unit test still passes. These tests exist to catch that — they
 * assert the SVG has the bars and labels it should, not that it looks right.
 *
 * Nobody has looked at this chart. Structure is what is verified here; appearance is not, and
 * saying otherwise would overstate what a DOM assertion can show.
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

const MILESTONES = [
  {
    id: "aaaaaaaa-0000-0000-0000-000000000001",
    name: "Planning consent",
    description: null,
    status: "Complete",
    baselineDate: "2026-06-01",
    forecastDate: "2026-09-01",
    actualDate: "2026-06-15",
    isKeyMilestone: true,
    delayDays: 14,
  },
  {
    id: "aaaaaaaa-0000-0000-0000-000000000002",
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
    id: "aaaaaaaa-0000-0000-0000-000000000003",
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

/** Serves the two endpoints the workspace needs; anything else gets an explicit 501. */
async function stubProject(page: Page, milestones: unknown[] = MILESTONES) {
  await page.route("**/api/**", async (route: Route) => {
    const path = new URL(route.request().url()).pathname;

    const body = path.endsWith("/milestones")
      ? milestones
      : path.endsWith(PROJECT_ID)
        ? PROJECT
        : null;

    if (body === null) {
      // A stub that falls through silently is how a test ends up asserting against an error
      // state it did not intend to create.
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

test.describe("Programme timeline", () => {
  test("draws a row per milestone with its name on the axis", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    const chart = page.locator(".recharts-surface").first();
    await expect(chart).toBeVisible();

    // The axis labels prove the chart received the data and laid out categories — an empty plot
    // with a correct dataset is exactly the failure a unit test cannot see.
    //
    // Matched loosely because Recharts wraps a long tick label across two <tspan>s, so
    // "Practical completion" has no space in the DOM's text content. That is a rendering detail,
    // not something worth asserting on.
    const ticks = await page.locator(".recharts-yAxis text").allTextContents();
    expect(ticks).toHaveLength(3);
    expect(ticks.join(" ")).toContain("Planning consent");
    expect(ticks.join(" ")).toContain("Start on site");
    expect(ticks.join(" ")).toMatch(/Practical\s*completion/);
  });

  test("draws a bar only for the milestones that moved", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    // Two slipped, one is on programme. The on-programme milestone still gets a minimum-size
    // mark so it is visible, so all three slip bars exist — what differs is their width.
    //
    // Scoped to the named series: the transparent spacer is also a bar, and counting both would
    // give 6 and pass no matter what the chart actually drew.
    const bars = page.locator(".milestone-slip-bar .recharts-bar-rectangle");
    await expect(bars).toHaveCount(3);
  });

  test("reports the worst single slip, not the total", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    // Start on site is 92 days late; planning consent completed 14 days late. A sum would read
    // 106 and describe a programme that does not exist.
    //
    // Exact, because the milestone table below renders the same slip as "+92d" — matching loosely
    // finds both. That the two agree is the point; this assertion is about the headline.
    await expect(page.getByText("92d", { exact: true })).toBeVisible();
    await expect(page.getByText("Start on site", { exact: true }).last()).toBeVisible();
  });

  test("uses the actual date for a completed milestone, not its forecast", async ({ page }) => {
    await stubProject(page);
    await openProgrammeTab(page);

    // Planning consent forecast 1 Sep but completed 15 Jun. If the forecast won, the worst slip
    // would be 92 days from this milestone rather than from Start on site — and the headline
    // figure below would still read 92, which is why the tooltip is checked instead.
    // Hovering the bar rather than the axis label: Recharts activates its tooltip from the plot
    // area, and an axis tick is outside it.
    await page.locator(".milestone-slip-bar .recharts-bar-rectangle").first().hover();

    await expect(page.getByText("Actual: 15 Jun 2026")).toBeVisible();
    await expect(page.getByText("14 days late")).toBeVisible();
  });

  test("shows a prompt rather than an empty chart when there are no milestones", async ({ page }) => {
    await stubProject(page, []);
    await openProgrammeTab(page);

    await expect(
      page.getByText("Add milestones to see the programme against its baseline."),
    ).toBeVisible();
    await expect(page.locator(".recharts-surface")).toHaveCount(0);
  });
});
