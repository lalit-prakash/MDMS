"use client";

import { Box, Button, Card, CardContent, Divider, Stack, Switch, Typography } from "@mui/material";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import { PageHeader } from "@/components/PageHeader";
import { useAuth } from "@/lib/session/AuthContext";
import { useThemeMode } from "@/lib/theme/ThemeModeContext";

export default function SettingsPage() {
  const { user, logout } = useAuth();
  const { mode, toggleMode } = useThemeMode();

  return (
    <Box>
      <PageHeader icon={<TuneOutlinedIcon fontSize="small" />} title="Settings" />
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Only settings this session actually has are shown here — no placeholder toggles for features that don&rsquo;t exist yet.
      </Typography>

      <Card variant="outlined" sx={{ mb: 3, maxWidth: 480 }}>
        <CardContent>
          <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
            Account
          </Typography>
          <Stack spacing={1.5}>
            <Box>
              <Typography variant="caption" color="text.secondary">Signed in as</Typography>
              <Typography>{user?.displayName}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Username</Typography>
              <Typography sx={{ fontFamily: "var(--font-mono)" }}>{user?.username}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Role</Typography>
              <Typography>{user?.role}</Typography>
            </Box>
          </Stack>
          <Divider sx={{ my: 2 }} />
          <Button variant="outlined" color="error" size="small" onClick={() => logout()}>
            Sign out
          </Button>
        </CardContent>
      </Card>

      <Card variant="outlined" sx={{ maxWidth: 480 }}>
        <CardContent>
          <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
            Appearance
          </Typography>
          <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
            <Typography variant="body2">Dark mode</Typography>
            <Switch checked={mode === "dark"} onChange={toggleMode} />
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
