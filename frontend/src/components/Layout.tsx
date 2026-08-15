import { useState } from "react";
import {
  AppBar,
  Box,
  Button,
  Container,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import LockIcon from "@mui/icons-material/Lock";
import MenuIcon from "@mui/icons-material/Menu";
import { Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export function Layout() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  function closeMenu() {
    setAnchorEl(null);
  }

  function handleSignOut() {
    closeMenu();
    logout();
  }

  return (
    <Stack minHeight="100vh">
      <AppBar position="static" color="inherit" elevation={0} sx={{ borderBottom: "1px solid", borderColor: "divider" }}>
        <Toolbar sx={{ justifyContent: "space-between" }}>
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

          {user ? (
            <>
              <IconButton
                aria-label="Account menu"
                onClick={(e) => setAnchorEl(e.currentTarget)}
              >
                <MenuIcon />
              </IconButton>

              {/*
                disableScrollLock: MUI's default scroll-lock adds its own
                body padding-right to compensate for the scrollbar it hides —
                redundant with (and additive to) the permanent scrollbar
                gutter reserved in index.css, which is what was pushing the
                right edge further in every time this menu opened.
              */}
              <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={closeMenu} disableScrollLock>
                <Box sx={{ px: 2, py: 1, minWidth: 220 }}>
                  <Typography variant="subtitle2" fontWeight={700}>
                    {user.firstName} {user.lastName}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    @{user.username}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {user.email}
                  </Typography>
                </Box>
                <Divider />
                <MenuItem onClick={handleSignOut}>Sign out</MenuItem>
              </Menu>
            </>
          ) : (
            <Stack direction="row" spacing={1}>
              <Button onClick={() => navigate("/signin")}>Sign in</Button>
              <Button variant="contained" onClick={() => navigate("/signup")}>
                Sign up
              </Button>
            </Stack>
          )}
        </Toolbar>
      </AppBar>

      {/*
        display:flex here (not just flex:1 on Container itself) because a
        percentage height ("100%") on a page's centering wrapper can't resolve
        against an ancestor whose own height only comes from flex-grow —
        Chrome doesn't treat that as a definite height for percentage
        children. Making this a flex container lets pages use flex:1 instead,
        which works reliably at any depth.
      */}
      <Container maxWidth="md" sx={{ flex: 1, display: "flex", flexDirection: "column", py: { xs: 3, sm: 4 } }}>
        <Outlet />
      </Container>
    </Stack>
  );
}
