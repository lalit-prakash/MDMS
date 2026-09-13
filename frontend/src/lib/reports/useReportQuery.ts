import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/apiClient";
import { ReportResult } from "./types";

/**
 * Shared fetch + pagination state for a report screen: owns page/pageSize, resets to page 1
 * whenever the filter query string changes (a filter change invalidating the current page is
 * exactly the bug this guards against — showing "page 4" of a now much-smaller filtered set).
 */
export function useReportQuery<TRow, TSummary>(reportPath: string, filterQuery: string) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const queryString = `${filterQuery}${filterQuery.includes("?") ? "&" : "?"}page=${page}&pageSize=${pageSize}`;
  const fullPath = `${reportPath}${queryString}`;

  const query = useQuery({
    queryKey: ["report", reportPath, filterQuery, page, pageSize],
    queryFn: () => apiClient.get<ReportResult<TRow, TSummary>>(fullPath),
  });

  const setFilters = () => setPage(1);

  return {
    ...query,
    page,
    pageSize,
    setPage,
    setPageSize: (size: number) => {
      setPageSize(size);
      setPage(1);
    },
    resetPage: setFilters,
    fullPath,
  };
}
