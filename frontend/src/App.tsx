import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CssBaseline, ThemeProvider } from "@mui/material";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { FindLockerPage } from "./pages/FindLockerPage";
import { LockerDetailPage } from "./pages/LockerDetailPage";
import { ReservationPage } from "./pages/ReservationPage";
import { ConfirmationPage } from "./pages/ConfirmationPage";
import { theme } from "./theme/theme";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 15_000,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          <Routes>
            <Route element={<Layout />}>
              <Route index element={<FindLockerPage />} />
              <Route path="lockers/:id" element={<LockerDetailPage />} />
              <Route path="reservations/new" element={<ReservationPage />} />
              <Route path="reservations/:id" element={<ConfirmationPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
