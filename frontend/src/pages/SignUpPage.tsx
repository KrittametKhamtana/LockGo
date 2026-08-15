import { useState } from "react";
import { Alert, Box, Button, Link as MuiLink, Stack, TextField, Typography } from "@mui/material";
import { Link, useNavigate } from "react-router-dom";
import { ApiError } from "../api/client";
import { useSignUp } from "../hooks/useAuthMutations";

export function SignUpPage() {
  const navigate = useNavigate();
  const mutation = useSignUp();

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const passwordsMismatch = confirmPassword.length > 0 && password !== confirmPassword;

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (passwordsMismatch) {
      return;
    }

    mutation.mutate(
      { firstName, lastName, email, username, password, confirmPassword },
      { onSuccess: () => navigate("/") },
    );
  }

  const errorMessage =
    mutation.error instanceof ApiError ? mutation.error.message : "Something went wrong. Please try again.";

  return (
    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", flex: 1 }}>
      <Stack spacing={3} component="form" onSubmit={handleSubmit} sx={{ width: "100%", maxWidth: 480 }}>
        <div>
          <Typography variant="h4">Create an account</Typography>
          <Typography variant="body1" color="text.secondary">
            Sign up to manage your reservations.
          </Typography>
        </div>

        <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
          <TextField
            label="First name"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Last name"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            required
            fullWidth
          />
        </Stack>

        <TextField
          type="email"
          label="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          fullWidth
        />

        <TextField
          label="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
          fullWidth
        />

        <TextField
          type="password"
          label="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          helperText="At least 8 characters."
          required
          fullWidth
        />

        <TextField
          type="password"
          label="Confirm password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          error={passwordsMismatch}
          helperText={passwordsMismatch ? "Passwords do not match." : " "}
          required
          fullWidth
        />

        {mutation.isError && <Alert severity="error">{errorMessage}</Alert>}

        <Button type="submit" variant="contained" size="large" disabled={mutation.isPending}>
          {mutation.isPending ? "Creating account…" : "Sign up"}
        </Button>

        <Typography variant="body2" color="text.secondary">
          Already have an account? <MuiLink component={Link} to="/signin">Sign in</MuiLink>
        </Typography>
      </Stack>
    </Box>
  );
}
