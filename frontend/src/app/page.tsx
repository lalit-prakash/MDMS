"use client";

import Link from "next/link";
import { Card, CardActionArea, CardContent, Typography, Grid, Box } from "@mui/material";

const TILES = [
  {
    href: "/meters",
    title: "Meters",
    description: "Meter master data, installation and replacement history.",
  },
  {
    href: "/meter-data",
    title: "Meter Data",
    description: "Load Survey and Daily Load Profile ingestion, data-quality holds.",
  },
  {
    href: "/vee",
    title: "VEE",
    description: "Out-of-range thresholds, missing-interval estimation, execution audit trail.",
  },
  {
    href: "/config",
    title: "Config",
    description: "Tariff categories, electrical hierarchy, organizational hierarchy.",
  },
  {
    href: "/users",
    title: "Users",
    description: "User records, roles, and org-unit scoping (RBAC data — not yet enforced).",
  },
  {
    href: "/energy-audit",
    title: "Energy Audit",
    description: "Network energy readings and transparent supply-vs-consumption balance.",
  },
];

export default function HomePage() {
  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
        Module dashboard
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        No role-based landing pages yet — this hub shows every module until authentication exists.
      </Typography>
      <Grid container spacing={3}>
        {TILES.map((tile) => (
          <Grid key={tile.href} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card variant="outlined" sx={{ height: "100%" }}>
              <CardActionArea component={Link} href={tile.href} sx={{ height: "100%", p: 1 }}>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {tile.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {tile.description}
                  </Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
