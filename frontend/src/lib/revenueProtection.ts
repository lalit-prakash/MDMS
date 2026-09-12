export type RevenueProtectionLeadStatus =
  | "Detected"
  | "Scored"
  | "Reviewed"
  | "Assigned"
  | "FieldInvestigation"
  | "FindingRecorded"
  | "ActionTaken"
  | "RecoveryRecorded"
  | "Closed";

export type RiskSignalType =
  | "TamperEvent"
  | "RepeatedCoverOpen"
  | "ConsumptionDeviation"
  | "DtrAnomaly"
  | "CommunicationManipulation"
  | "UnbilledMappingAnomaly";

export interface RevenueProtectionLead {
  id: string;
  customerId: string;
  meterId: string | null;
  status: RevenueProtectionLeadStatus;
  riskScore: number;
  assignedToUserId: string | null;
  fieldFindingNote: string | null;
  actionTaken: string | null;
  recoveryAmount: number | null;
  closureReason: string | null;
}
