import { Card, CardContent, Grid, Typography } from "@mui/material";

export interface SummaryCardSpec {
  label: string;
  value: string;
}

/** Summary KPI row above a report table — always computed by the backend over the full filtered
 * population, never the current page, so it can never contradict the table's own totals. */
export function ReportSummaryCards({ cards }: { cards: SummaryCardSpec[] }) {
  return (
    <Grid container spacing={2} sx={{ mb: 3 }}>
      {cards.map((c) => (
        <Grid key={c.label} size={{ xs: 6, sm: 4, md: 12 / Math.min(cards.length, 6) }}>
          <Card variant="outlined">
            <CardContent sx={{ py: "var(--space-3) !important" }}>
              <Typography variant="caption" sx={{ color: "var(--card-muted)", display: "block" }}>
                {c.label}
              </Typography>
              <Typography sx={{ fontSize: 22, fontWeight: 700, color: "var(--card-metric)" }}>{c.value}</Typography>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}
