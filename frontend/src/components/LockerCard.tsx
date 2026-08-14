import { Box, Card, CardActionArea, CardContent, Chip, Stack, Typography } from "@mui/material";
import LocationOnIcon from "@mui/icons-material/LocationOn";
import { useNavigate } from "react-router-dom";
import { SizeAvailabilityChips } from "./SizeAvailabilityChips";
import type { LockerListItem } from "../types/locker";

interface LockerCardProps {
  locker: LockerListItem;
}

/**
 * Operating status and availability are separate concerns: "Closed" means the
 * site isn't operating at all, while "Fully booked" means it's open but
 * every compartment is taken. Only one badge is shown, with Closed winning —
 * if the site is shut, its availability isn't the useful information.
 */
function statusChip(locker: LockerListItem) {
  if (locker.operatingStatus === "Closed") {
    return { label: "Closed", color: "default" as const, variant: "outlined" as const };
  }
  if (locker.isFullyBooked) {
    return { label: "Fully booked", color: "warning" as const, variant: "filled" as const };
  }
  return { label: "Open", color: "success" as const, variant: "filled" as const };
}

export function LockerCard({ locker }: LockerCardProps) {
  const navigate = useNavigate();
  const status = statusChip(locker);

  return (
    <Card variant="outlined">
      <CardActionArea onClick={() => navigate(`/lockers/${locker.id}`)} sx={{ p: 1 }}>
        <CardContent>
          <Stack direction="row" justifyContent="space-between" alignItems="flex-start" gap={2}>
            <Box>
              <Typography variant="h6">{locker.name}</Typography>
              <Stack direction="row" alignItems="center" gap={0.5} sx={{ color: "text.secondary", mt: 0.5 }}>
                <LocationOnIcon fontSize="small" />
                <Typography variant="body2">{locker.address}</Typography>
              </Stack>
            </Box>
            <Chip size="small" label={status.label} color={status.color} variant={status.variant} />
          </Stack>

          <Box sx={{ mt: 2 }}>
            <SizeAvailabilityChips sizeAvailability={locker.sizeAvailability} showMissingSizes />
          </Box>

          <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mt: 2 }}>
            <Typography variant="body2" color={locker.isFullyBooked ? "text.disabled" : "success.main"}>
              {locker.isFullyBooked
                ? "Fully booked"
                : `${locker.availableCompartmentCount} compartment${locker.availableCompartmentCount === 1 ? "" : "s"} available`}
            </Typography>

            <Stack direction="row" alignItems="center" gap={2}>
              {locker.distanceKm !== null && (
                <Typography variant="body2" color="text.secondary">
                  {locker.distanceKm.toFixed(1)} km
                </Typography>
              )}
              <Typography variant="subtitle1" fontWeight={700}>
                from ฿{locker.minPrice.toFixed(0)}
              </Typography>
            </Stack>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
