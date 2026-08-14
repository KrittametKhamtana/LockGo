import { useState } from "react";
import { Alert, Box, Button, Chip, CircularProgress, Stack, Typography } from "@mui/material";
import LocationOnIcon from "@mui/icons-material/LocationOn";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useNavigate, useParams } from "react-router-dom";
import { CompartmentSelector } from "../components/CompartmentSelector";
import { useLocker } from "../hooks/useLockers";
import type { Compartment } from "../types/locker";

export function LockerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: locker, isLoading, isError } = useLocker(id);
  const [selected, setSelected] = useState<Compartment | null>(null);

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

  return (
    <Stack spacing={3}>
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate(-1)}
        sx={{ alignSelf: "flex-start" }}
      >
        Back
      </Button>

      <Box>
        <Stack direction="row" alignItems="center" gap={1.5}>
          <Typography variant="h4">{locker.name}</Typography>
          <Chip
            size="small"
            label={isOpen ? "Open" : "Closed"}
            color={isOpen ? "success" : "default"}
            variant={isOpen ? "filled" : "outlined"}
          />
        </Stack>
        <Stack direction="row" alignItems="center" gap={0.5} sx={{ color: "text.secondary", mt: 0.5 }}>
          <LocationOnIcon fontSize="small" />
          <Typography variant="body1">{locker.address}</Typography>
        </Stack>
      </Box>

      {!isOpen && <Alert severity="warning">This locker is currently closed and can't be reserved.</Alert>}

      <div>
        <Typography variant="h6" sx={{ mb: 1.5 }}>
          Available compartments
        </Typography>
        <CompartmentSelector
          compartments={locker.compartments}
          selectedId={selected?.id ?? null}
          onSelect={setSelected}
        />
      </div>

      <Button
        variant="contained"
        size="large"
        disabled={!selected || !isOpen}
        onClick={() =>
          navigate("/reservations/new", { state: { locker, compartment: selected } })
        }
      >
        {selected ? `Select ${selected.size} compartment — ฿${selected.price.toFixed(0)}` : "Select a compartment"}
      </Button>
    </Stack>
  );
}
