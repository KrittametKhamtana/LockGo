import { useState } from "react";
import { Alert, Box, Button, Link as MuiLink, Stack, TextField, Typography } from "@mui/material";
import { Link, useNavigate } from "react-router-dom";
import { ApiError } from "../api/client";
import { useSignIn } from "../hooks/useAuthMutations";

export function SignInPage() {
  const navigate = useNavigate();
  const mutation = useSignIn();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    mutation.mutate({ username, password }, { onSuccess: () => navigate("/") });
  }

  const errorMessage =
    mutation.error instanceof ApiError ? mutation.error.message : "Something went wrong. Please try again.";

  return (
    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", flex: 1 }}>
      <Stack spacing={3} component="form" onSubmit={handleSubmit} sx={{ width: "100%", maxWidth: 480 }}>
        <div>
          <Typography variant="h4">Sign in</Typography>
          <Typography variant="body1" color="text.secondary">
            Welcome back to LockGo.
          </Typography>
        </div>

        <TextField
          label="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
          fullWidth
          autoFocus
        />

        <TextField
          type="password"
          label="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          fullWidth
        />

        <MuiLink component={Link} to="/forgot-password" variant="body2" sx={{ alignSelf: "flex-end" }}>
          Forgot password?
        </MuiLink>

        {mutation.isError && <Alert severity="error">{errorMessage}</Alert>}

        <Button type="submit" variant="contained" size="large" disabled={mutation.isPending}>
          {mutation.isPending ? "Signing in…" : "Sign in"}
        </Button>

        <Typography variant="body2" color="text.secondary">
          Don't have an account? <MuiLink component={Link} to="/signup">Sign up</MuiLink>
        </Typography>
      </Stack>
    </Box>
  );
}
