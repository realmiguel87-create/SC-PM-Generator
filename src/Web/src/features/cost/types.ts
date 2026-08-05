export interface CostPlanLine {
  costCategory: string;
  description?: string | null;
  amount: number;
}

export interface CostPlan {
  id: string;
  name: string;
  versionNumber: number;
  isBaseline: boolean;
  totalAmount: number;
  lines: CostPlanLine[];
}

export interface Forecast {
  id: string;
  forecastDate: string;
  forecastCost: number;
  approvedBudgetAtForecast: number;
  variance: number;
  commentaryNotes: string | null;
}

export interface CostSummary {
  projectId: string;
  approvedBudget: number;
  currentForecastCost: number;
  currentVariance: number;
  baselineCostPlan: CostPlan | null;
  forecastHistory: Forecast[];
}
