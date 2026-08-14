import { Alert, Button, Chip, CircularProgress, Stack, Typography } from "@mui/material";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import { useNavigate, useParams } from "react-router-dom";
import { SummaryCard } from "../components/SummaryCard";
import { useReservationQuery } from "../hooks/useReservation";

const STATUS_COLOR: Record<string, "success" | "default" | "error"> = {
  Active: "success",
  Completed: "default",
  Cancelled: "error",
};

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export function ConfirmationPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: reservation, isLoading, isError } = useReservationQuery(id);

  if (isLoading) {
    return (
      <Stack alignItems="center" py={6}>
        <CircularProgress />
      </Stack>
    );
  }

  if (isError || !reservation) {
    return <Alert severity="error">Couldn't find this reservation.</Alert>;
  }

  const displayStatus = reservation.isActive ? "Active" : reservation.status;

  return (
    <Stack spacing={3}>
      <Stack alignItems="center" spacing={1} textAlign="center">
        <CheckCircleIcon color="success" sx={{ fontSize: 56 }} />
        <Typography variant="h4">Booking confirmed</Typography>
        <Typography variant="body1" color="text.secondary">
          Show your booking number at the locker to unlock your compartment.
        </Typography>
      </Stack>

      <SummaryCard
        title={reservation.bookingNumber}
        subtitle={`${reservation.lockerName} · ${reservation.lockerAddress}`}
        rows={[
          { label: "Compartment", value: reservation.compartmentSize },
          { label: "Price", value: `฿${reservation.price.toFixed(0)}` },
          { label: "Start time", value: formatDateTime(reservation.startTime) },
          { label: "Expires", value: formatDateTime(reservation.endTime) },
        ]}
        footer={
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="subtitle1">Status</Typography>
            <Chip label={displayStatus} color={STATUS_COLOR[displayStatus] ?? "default"} />
          </Stack>
        }
      />

      <Button variant="outlined" onClick={() => navigate("/")}>
        Back to Find Locker
      </Button>
    </Stack>
  );
}
