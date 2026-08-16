import { useMemo, useRef, useState } from "react";
import { Alert, Button, Stack, TextField, ToggleButton, ToggleButtonGroup, Typography } from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useLocation, useNavigate } from "react-router-dom";
import { SummaryCard } from "../components/SummaryCard";
import { useCreateReservation } from "../hooks/useReservation";
import { SIZE_LABEL, type CompartmentSize, type LockerDetail } from "../types/locker";
import { ApiError } from "../api/client";
import {
  DURATION_OPTIONS,
  MAX_ADVANCE_DAYS,
  isValidStartTime,
  parseLocalInputValue,
  toLocalInputValue,
} from "../utils/booking";
import { generateUUID } from "../utils/uuid";

interface ReservationRouteState {
  locker: LockerDetail;
  size: CompartmentSize;
  price: number;
  /** The slot the visitor was browsing, if they picked one. ISO 8601. */
  startTime?: string;
  durationHours?: number;
}

export function ReservationPage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const routeState = state as ReservationRouteState | null;

  // Generated once when the page loads — every Confirm click (including a
  // double-click retry) reuses the SAME key, which is what lets the backend
  // recognize a resend and return the original booking instead of a duplicate.
  const idempotencyKey = useMemo(() => generateUUID(), []);
  // Pre-filled from the slot the visitor browsed, so the availability they saw
  // is the slot they're about to book — still editable here.
  const [durationHours, setDurationHours] = useState(routeState?.durationHours ?? 2);
  const [startTime, setStartTime] = useState(() =>
    toLocalInputValue(routeState?.startTime ? new Date(routeState.startTime) : new Date()),
  );
  const mutation = useCreateReservation();

  // Recomputed on render rather than frozen in state — a bound captured at mount
  // would drift stale while the user sits on this screen.
  const minStartTime = toLocalInputValue(new Date());
  const maxStartTime = toLocalInputValue(
    new Date(Date.now() + MAX_ADVANCE_DAYS * 24 * 60 * 60 * 1000),
  );

  const startTimeDate = parseLocalInputValue(startTime);
  const isStartTimeValid = startTimeDate !== null && isValidStartTime(startTimeDate);

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
    if (submittingRef.current || !isStartTimeValid || startTimeDate === null) {
      return;
    }
    submittingRef.current = true;

    mutation.mutate(
      {
        lockerId: locker.id,
        size,
        durationHours,
        idempotencyKey,
        startTime: startTimeDate.toISOString(),
      },
      {
        onSuccess: (reservation) => {
          navigate(`/reservations/${reservation.bookingNumber}`, { replace: true });
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
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate(-1)}
        disabled={mutation.isPending}
        sx={{ alignSelf: "flex-start" }}
      >
        Back
      </Button>

      <Typography variant="h4">Confirm your reservation</Typography>

      <div>
        <Typography variant="subtitle1" sx={{ mb: 1 }}>
          Start time
        </Typography>
        <TextField
          type="datetime-local"
          value={startTime}
          onChange={(event) => setStartTime(event.target.value)}
          slotProps={{ htmlInput: { min: minStartTime, max: maxStartTime } }}
          error={startTime !== "" && !isStartTimeValid}
          helperText={
            startTime !== "" && !isStartTimeValid
              ? `Pick a time from now up to ${MAX_ADVANCE_DAYS} days ahead.`
              : undefined
          }
          fullWidth
        />
      </div>

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
          {
            label: "Starts",
            value: isStartTimeValid && startTimeDate ? startTimeDate.toLocaleString() : "—",
          },
          {
            label: "Ends",
            value:
              isStartTimeValid && startTimeDate
                ? new Date(startTimeDate.getTime() + durationHours * 60 * 60 * 1000).toLocaleString()
                : "—",
          },
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

      <Button
        variant="contained"
        size="large"
        onClick={handleConfirm}
        disabled={mutation.isPending || !isStartTimeValid}
      >
        {mutation.isPending ? "Confirming…" : "Confirm reservation"}
      </Button>
    </Stack>
  );
}
