export interface Variation {
  id: string;
  reference: string;
  description: string;
  valueImpact: number;
  status: "Instructed" | "Priced" | "Agreed";
}

export interface ExtensionOfTime {
  id: string;
  reference: string;
  reason: string;
  daysClaimed: number;
  daysAwarded: number | null;
  status: "Claimed" | "UnderReview" | "Awarded" | "Rejected";
}

export interface LossAndExpenseClaim {
  id: string;
  reference: string;
  description: string;
  claimedAmount: number;
  awardedAmount: number | null;
  status: "Claimed" | "UnderReview" | "Agreed" | "Rejected";
}

export interface ArchitectsInstruction {
  id: string;
  instructionNumber: number;
  description: string;
  issuedDate: string;
  status: "Issued" | "Complied";
}

export interface InterimValuation {
  id: string;
  valuationNumber: number;
  valuationDate: string;
  grossValuation: number;
  netPayment: number;
  status: "Draft" | "Certified" | "Paid";
}
