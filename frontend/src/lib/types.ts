// Mirrors the backend's C# enums/records (MDMS.Domain / MDMS.Application). Kept in sync by hand
// since there's no shared-schema generation set up yet — see the backend README's "Not yet built".

export type MeterPhase = "Single" | "Three";
export type MeterStatus = "InStock" | "Installed" | "Removed" | "Retired";

export interface Meter {
  id: string;
  tenantId: string;
  createdAtUtc: string;
  serialNumber: string;
  phase: MeterPhase;
  status: MeterStatus;
}

export type MeasurementQuality =
  | "Valid"
  | "NegativeConsumption"
  | "OutOfRange"
  | "Missing"
  | "Suspect";

export type MeasurementSource = "Received" | "Estimated" | "Edited" | "Calculated";

export interface LoadSurveyInterval {
  id: string;
  meterId: string;
  intervalStartUtc: string;
  intervalEndUtc: string;
  cumulativeReading: number;
  consumptionKwh: number;
  quality: MeasurementQuality;
  source: MeasurementSource;
}

export interface DailyLoadProfile {
  id: string;
  servicePointId: string;
  meterId: string;
  profileDate: string;
  consumptionKwh: number;
  source: MeasurementSource;
  quality: MeasurementQuality;
}

export interface DataQualityHold {
  id: string;
  meterId: string;
  reason: string;
  raisedAtUtc: string;
  isActive: boolean;
  clearedAtUtc: string | null;
  resolutionNote: string | null;
}

export type MeasurementRangeType = "LoadSurveyInterval" | "DailyLoadProfile";

export interface MeasurementRangeThreshold {
  id: string;
  ruleType: "OutOfRange";
  meterId: string | null;
  isActive: boolean;
  measurementType: MeasurementRangeType;
  minConsumptionKwh: number;
  maxConsumptionKwh: number;
}

export interface VeeExecutionRecord {
  id: string;
  ruleName: string;
  measurementType: MeasurementRangeType;
  meterId: string;
  slotStartUtc: string;
  slotEndUtc: string;
  resultQuality: MeasurementQuality;
  newValue: number | null;
  details: string;
}

export interface TariffCategory {
  id: string;
  code: string;
  name: string;
  description: string | null;
}

export type HierarchyNodeType = "Substation" | "Feeder" | "DistributionTransformer";

export interface HierarchyNode {
  id: string;
  nodeType: HierarchyNodeType;
  code: string;
  name: string;
  parentId: string | null;
}

export type OrgUnitType = "Zone" | "Circle" | "Division" | "SubDivision" | "Section";

export interface OrgUnit {
  id: string;
  unitType: OrgUnitType;
  code: string;
  name: string;
  parentId: string | null;
}

export type UserRole =
  | "Admin"
  | "ItManager"
  | "Nomc"
  | "Supervisor"
  | "QualityIncharge"
  | "OmSupervisor"
  | "Installer"
  | "OmExecutive"
  | "Contractor"
  | "UtilityManager"
  | "ComplaintDesk"
  | "StoreManager";

export interface MdmsUser {
  id: string;
  username: string;
  displayName: string;
  role: UserRole;
  orgUnitId: string | null;
}
