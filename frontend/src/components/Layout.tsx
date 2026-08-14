import { AppBar, Container, Stack, Toolbar, Typography } from "@mui/material";
import LockIcon from "@mui/icons-material/Lock";
import { Outlet, useNavigate } from "react-router-dom";

export function Layout() {
  const navigate = useNavigate();

  return (
    <Stack minHeight="100vh">
      <AppBar position="static" color="inherit" elevation={0} sx={{ borderBottom: "1px solid", borderColor: "divider" }}>
        <Toolbar>
          <Stack
            direction="row"
            alignItems="center"
            gap={1}
            sx={{ cursor: "pointer" }}
            onClick={() => navigate("/")}
          >
            <LockIcon color="primary" />
            <Typography variant="h6" fontWeight={800} color="primary">
              LockGo
            </Typography>
          </Stack>
        </Toolbar>
      </AppBar>

      <Container maxWidth="md" sx={{ flex: 1, py: { xs: 3, sm: 4 } }}>
        <Outlet />
      </Container>
    </Stack>
  );
}
