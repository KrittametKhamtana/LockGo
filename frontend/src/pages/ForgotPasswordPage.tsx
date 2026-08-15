import { useState } from "react";
import { Alert, Box, Button, Link as MuiLink, Stack, TextField, Typography } from "@mui/material";
import { Link } from "react-router-dom";

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    // No backend endpoint for this yet — this just shows the UI shape for
    // now. Same message regardless of whether the email is registered, so
    // it never confirms/denies an account's existence once this is wired up.
    setSubmitted(true);
  }

  return (
    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", flex: 1 }}>
      <Stack spacing={3} component="form" onSubmit={handleSubmit} sx={{ width: "100%", maxWidth: 480 }}>
        <div>
          <Typography variant="h4">Reset password</Typography>
          <Typography variant="body1" color="text.secondary">
            Enter your account email and we'll send you a reset link.
          </Typography>
        </div>

        <TextField
          type="email"
          label="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          fullWidth
          autoFocus
        />

        {submitted && (
          <Alert severity="success">
            If an account exists for that email, a reset link is on its way.
          </Alert>
        )}

        <Button type="submit" variant="contained" size="large" disabled={submitted}>
          Send reset link
        </Button>

        <Typography variant="body2" color="text.secondary">
          <MuiLink component={Link} to="/signin">Back to sign in</MuiLink>
        </Typography>
      </Stack>
    </Box>
  );
}
