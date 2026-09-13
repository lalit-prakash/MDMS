import { Box, Typography } from "@mui/material";

/** Icon + title header used at the top of every module page, matching the dashboard's tile icons. */
export function PageHeader({ icon, title }: { icon: React.ReactNode; title: string }) {
  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: "var(--space-3)", mb: 1 }}>
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          width: 40,
          height: 40,
          borderRadius: "var(--radius-md)",
          bgcolor: "var(--color-accent-soft)",
          color: "var(--color-accent)",
          flexShrink: 0,
        }}
      >
        {icon}
      </Box>
      <Typography variant="h4" sx={{ fontWeight: 600 }}>
        {title}
      </Typography>
    </Box>
  );
}
