import { useMemo, useRef, useState } from "react";
import { Alert, Button, Stack, ToggleButton, ToggleButtonGroup, Typography } from "@mui/material";
import { useLocation, useNavigate } from "react-router-dom";
import { SummaryCard } from "../components/SummaryCard";
import { useCreateReservation } from "../hooks/useReservation";
import { SIZE_LABEL, type CompartmentSize, type LockerDetail } from "../types/locker";
import { ApiError } from "../api/client";

interface ReservationRouteState {
  locker: LockerDetail;
  size: CompartmentSize;
  price: number;
}

const DURATION_OPTIONS = [
  { hours: 2, label: "2 hours" },
  { hours: 4, label: "4 hours" },
  { hours: 8, label: "8 hours" },
  { hours: 24, label: "1 day" },
  { hours: 48, label: "2 days" },
];

export function ReservationPage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const routeState = state as ReservationRouteState | null;

  // Generated once when the page loads — every Confirm click (including a
  // double-click retry) reuses the SAME key, which is what lets the backend
  // recognize a resend and return the original booking instead of a duplicate.
  const idempotencyKey = useMemo(() => crypto.randomUUID(), []);
  const [durationHours, setDurationHours] = useState(2);
  const mutation = useCreateReservation();

  // Belt-and-suspenders alongside mutation.isPending: guards synchronously,
  // in the same tick as the click, before React even has a chance to
  // re-render the disabled button.
  const submittingRef = useRef(false);

  if (!routeState) {
    return (
      <Alert severity="warning">
        No compartment selected.{" "}
        <Button size="small" onClick={() => navigate("/")}>
          Start over
        </Button>
      </Alert>
    );
  }

  const { locker, size, price } = routeState;

  function handleConfirm() {
    if (submittingRef.current) {
      return;
    }
    submittingRef.current = true;

    mutation.mutate(
      { lockerId: locker.id, size, durationHours, idempotencyKey },
      {
        onSuccess: (reservation) => {
          navigate(`/reservations/${reservation.id}`, { replace: true });
        },
        onSettled: () => {
          submittingRef.current = false;
        },
      },
    );
  }

  const errorMessage =
    mutation.error instanceof ApiError ? mutation.error.message : "Something went wrong. Please try again.";

  return (
    <Stack spacing={3}>
      <Typography variant="h4">Confirm your reservation</Typography>

      <div>
        <Typography variant="subtitle1" sx={{ mb: 1 }}>
          Duration
        </Typography>
        <ToggleButtonGroup
          exclusive
          value={durationHours}
          onChange={(_, value) => value !== null && setDurationHours(value)}
          color="primary"
          sx={{
            flexWrap: "wrap",
            gap: 1,
            "& .MuiToggleButtonGroup-grouped": {
              flex: { xs: "1 1 calc(33.333% - 8px)", sm: "0 0 auto" },
              borderRadius: "8px !important",
              border: "1px solid !important",
            },
          }}
        >
          {DURATION_OPTIONS.map((option) => (
            <ToggleButton key={option.hours} value={option.hours}>
              {option.label}
            </ToggleButton>
          ))}
        </ToggleButtonGroup>
      </div>

      <SummaryCard
        title={locker.name}
        subtitle={locker.address}
        rows={[
          { label: "Compartment size", value: SIZE_LABEL[size] },
          { label: "Duration", value: DURATION_OPTIONS.find((o) => o.hours === durationHours)?.label },
        ]}
        footer={
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="subtitle1">Total</Typography>
            <Typography variant="h5" fontWeight={700}>
              ฿{price.toFixed(0)}
            </Typography>
          </Stack>
        }
      />

      {mutation.isError && <Alert severity="error">{errorMessage}</Alert>}

      <Button variant="contained" size="large" onClick={handleConfirm} disabled={mutation.isPending}>
        {mutation.isPending ? "Confirming…" : "Confirm reservation"}
      </Button>
    </Stack>
  );
}
