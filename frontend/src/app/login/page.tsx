"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import { Alert, Box, Button, Checkbox, FormControlLabel, TextField, Tooltip, Typography } from "@mui/material";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import LockOutlinedIcon from "@mui/icons-material/LockOutlined";
import { useAuth } from "@/lib/session/AuthContext";

export default function LoginPage() {
  const router = useRouter();
  const { login, claim } = useAuth();
  const [mode, setMode] = useState<"login" | "claim">("login");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = username.trim();
    if (!trimmed || !password) return;

    setSubmitting(true);
    setError(null);
    setInfo(null);

    const result = mode === "login" ? await login(trimmed, password) : await claim(trimmed, password);

    if (result.ok) {
      router.push("/");
      return;
    }

    // The claim endpoint returns 409 (as plain text) when the account already has a password —
    // route the user back to Sign in with that explained, rather than a bare "failed" message.
    if (mode === "claim" && result.error.toLowerCase().includes("already has a password")) {
      setMode("login");
      setInfo("This account already has a password — sign in normally.");
    } else {
      setError(result.error || (mode === "login" ? "Invalid username or password." : "Could not claim this account."));
    }
    setSubmitting(false);
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", bgcolor: "var(--color-bg-app)" }}>
      {/* Hero panel — decorative, hidden on narrow screens */}
      <Box
        sx={{
          display: { xs: "none", md: "flex" },
          flexDirection: "column",
          justifyContent: "space-between",
          width: "42%",
          minWidth: 380,
          p: "var(--space-8)",
          color: "#fff",
          backgroundImage:
            "linear-gradient(180deg, rgba(6,20,27,0.35), rgba(6,20,27,0.82)), url(/login-hero.jpg)",
          backgroundSize: "cover",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", gap: "var(--space-2)" }}>
          <Image src="/mdms-mark.png" alt="" width={30} height={25} />
          <Box>
            <Typography sx={{ fontWeight: 700, fontSize: 18, lineHeight: 1.2 }}>MDMS</Typography>
            <Typography sx={{ fontSize: 11, opacity: 0.75 }}>Meter Data Management System</Typography>
          </Box>
        </Box>

        <Box>
          <Typography sx={{ fontSize: 34, fontWeight: 700, lineHeight: 1.15, mb: 2 }}>
            Smarter Energy for a Brighter Tomorrow
          </Typography>
          <Typography sx={{ fontSize: 14, opacity: 0.8, lineHeight: 1.7 }}>
            Reliable data.
            <br />
            Efficient operations.
            <br />
            Sustainable communities.
          </Typography>
        </Box>

        <Typography sx={{ fontSize: 12, opacity: 0.6 }}>Data → Insights → Better Decisions</Typography>
      </Box>

      {/* Form panel */}
      <Box
        component="form"
        onSubmit={handleSubmit}
        sx={{
          flex: 1,
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          alignItems: "center",
          px: "var(--space-6)",
        }}
      >
        <Box sx={{ width: "100%", maxWidth: 360 }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: "var(--space-2)", mb: 5, justifyContent: { xs: "center", md: "flex-start" } }}>
            <Image src="/mdms-mark.png" alt="" width={28} height={23} />
            <Typography sx={{ fontWeight: 700, fontSize: 18 }}>MDMS</Typography>
          </Box>

          <Typography variant="h5" sx={{ fontWeight: 700, mb: 0.5 }}>
            {mode === "login" ? "Welcome back" : "Claim your account"}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
            {mode === "login"
              ? "Sign in to continue to MDMS"
              : "Set a password for the MDMS account an admin already created for you"}
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
              {error}
            </Alert>
          )}
          {info && (
            <Alert severity="info" sx={{ mb: 2 }} onClose={() => setInfo(null)}>
              {info}
            </Alert>
          )}

          <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
            Username
          </Typography>
          <TextField
            fullWidth
            size="small"
            placeholder="Enter your MDMS username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            slotProps={{ input: { startAdornment: <PersonOutlineOutlinedIcon fontSize="small" sx={{ mr: 1, color: "var(--card-muted)" }} /> } }}
            sx={{ mb: 2.5 }}
          />

          <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
            Password
          </Typography>
          <TextField
            fullWidth
            size="small"
            type="password"
            placeholder={mode === "login" ? "Enter your password" : "Choose a password (min 8 characters)"}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            slotProps={{ input: { startAdornment: <LockOutlinedIcon fontSize="small" sx={{ mr: 1, color: "var(--card-muted)" }} /> } }}
            sx={{ mb: 1.5 }}
          />

          {mode === "login" ? (
            <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
              <FormControlLabel control={<Checkbox size="small" defaultChecked />} label={<Typography variant="body2">Remember me</Typography>} />
              <Tooltip title="Not available yet — ask an admin to recreate your account if you're locked out">
                <Typography variant="body2" sx={{ color: "var(--color-accent)", cursor: "not-allowed" }}>
                  Forgot password?
                </Typography>
              </Tooltip>
            </Box>
          ) : (
            <Box sx={{ mb: 3 }} />
          )}

          <Button type="submit" fullWidth variant="contained" size="large" disabled={!username.trim() || !password || submitting}>
            {submitting ? "Please wait…" : mode === "login" ? "Sign in" : "Set password & sign in"}
          </Button>

          <Typography
            variant="caption"
            sx={{ display: "block", textAlign: "center", mt: 2, color: "var(--color-accent)", cursor: "pointer" }}
            onClick={() => {
              setMode((m) => (m === "login" ? "claim" : "login"));
              setError(null);
              setInfo(null);
            }}
          >
            {mode === "login" ? "First time signing in? Claim your account" : "Already have a password? Sign in"}
          </Typography>

          <Typography variant="caption" color="text.secondary" sx={{ display: "block", textAlign: "center", mt: 4 }}>
            Need help? Contact your system administrator.
          </Typography>
        </Box>
      </Box>
    </Box>
  );
}
