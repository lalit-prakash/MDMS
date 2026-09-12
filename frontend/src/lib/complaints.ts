export type ComplaintStatus = "Open" | "Assigned" | "InProgress" | "Resolved" | "Closed";
export type ComplaintSource = "ConsumerPortal" | "MobileApp" | "Helpline1912";

export interface Complaint {
  id: string;
  customerId: string;
  meterId: string | null;
  source: ComplaintSource;
  description: string;
  status: ComplaintStatus;
  slaDueUtc: string;
  assignedToUserId: string | null;
  resolutionNote: string | null;
  resolvedAtUtc: string | null;
  closedAtUtc: string | null;
}
