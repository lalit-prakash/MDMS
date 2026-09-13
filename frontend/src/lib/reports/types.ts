export interface PaginationInfo {
  page: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

export interface ReportResult<TRow, TSummary> {
  data: TRow[];
  pagination: PaginationInfo;
  summary: TSummary;
  generatedAtUtc: string;
}

/** Page sizes the reporting spec allows — never 500/1000/"All". */
export const REPORT_PAGE_SIZES = [25, 50, 100] as const;
