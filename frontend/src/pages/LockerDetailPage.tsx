import { useState } from "react";
import { Alert, Box, Button, Chip, CircularProgress, Stack, Typography } from "@mui/material";
import LocationOnIcon from "@mui/icons-material/LocationOn";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useNavigate, useParams } from "react-router-dom";
import { CompartmentSelector } from "../components/CompartmentSelector";
import { useLocker } from "../hooks/useLockers";
import { SIZE_LABEL, type CompartmentSizeAvailability } from "../types/locker";

export function LockerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: locker, isLoading, isError } = useLocker(id);
  const [selected, setSelected] = useState<CompartmentSizeAvailability | null>(null);

  if (isLoading) {
    return (
      <Stack alignItems="center" py={6}>
        <CircularProgress />
      </Stack>
    );
  }

  if (isError || !locker) {
    return <Alert severity="error">Couldn't load this locker. Please go back and try again.</Alert>;
  }

  const isOpen = locker.operatingStatus === "Open";
  const canBook = isOpen && !locker.isFullyBooked;

  // The selection may have gone stale if availability changed while the page
  // was open — never let a full size stay selected.
  const selectedIsStillAvailable =
    selected !== null &&
    (locker.sizeAvailability.find((s) => s.size === selected.size)?.availableCount ?? 0) > 0;

  return (
    <Stack spacing={3}>
      <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(-1)} sx={{ alignSelf: "flex-start" }}>
        Back
      </Button>

      <Box>
        <Stack direction="row" alignItems="center" gap={1.5} flexWrap="wrap">
          <Typography variant="h4">{locker.name}</Typography>
          {!isOpen ? (
            <Chip size="small" label="Closed" variant="outlined" />
          ) : locker.isFullyBooked ? (
            <Chip size="small" label="No availability" color="warning" />
          ) : (
            <Chip size="small" label="Open" color="success" />
          )}
        </Stack>
        <Stack direction="row" alignItems="center" gap={0.5} sx={{ color: "text.secondary", mt: 0.5 }}>
          <LocationOnIcon fontSize="small" />
          <Typography variant="body1">{locker.address}</Typography>
        </Stack>
      </Box>

      {!isOpen && <Alert severity="warning">This locker is currently closed and can't be reserved.</Alert>}
      {isOpen && locker.isFullyBooked && (
        <Alert severity="info">Every compartment here is currently booked. Try another location.</Alert>
      )}

      <div>
        <Typography variant="h6" sx={{ mb: 1.5 }}>
          Available compartments
        </Typography>
        <CompartmentSelector
          sizeAvailability={locker.sizeAvailability}
          selectedSize={selectedIsStillAvailable ? selected!.size : null}
          onSelect={setSelected}
          disabled={!canBook}
        />
      </div>

      <Button
        variant="contained"
        size="large"
        disabled={!selectedIsStillAvailable || !canBook}
        onClick={() =>
          navigate("/reservations/new", { state: { locker, size: selected!.size, price: selected!.price } })
        }
      >
        {selectedIsStillAvailable
          ? `Select ${SIZE_LABEL[selected!.size]} — ฿${selected!.price.toFixed(0)}`
          : "Select a compartment"}
      </Button>
    </Stack>
  );
}
