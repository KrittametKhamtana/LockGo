import { Box, Card, CardActionArea, CardContent, Chip, Stack, Typography } from "@mui/material";
import LocationOnIcon from "@mui/icons-material/LocationOn";
import LockIcon from "@mui/icons-material/Lock";
import { useNavigate } from "react-router-dom";
import type { LockerListItem } from "../types/locker";

interface LockerCardProps {
  locker: LockerListItem;
}

export function LockerCard({ locker }: LockerCardProps) {
  const navigate = useNavigate();
  const isOpen = locker.operatingStatus === "Open";
  const hasAvailability = locker.availableCompartmentCount > 0;

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
            <Chip
              size="small"
              label={isOpen ? "Open" : "Closed"}
              color={isOpen ? "success" : "default"}
              variant={isOpen ? "filled" : "outlined"}
            />
          </Stack>

          <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mt: 2 }}>
            <Stack direction="row" alignItems="center" gap={0.5}>
              <LockIcon fontSize="small" color={hasAvailability ? "success" : "disabled"} />
              <Typography variant="body2" color={hasAvailability ? "success.main" : "text.disabled"}>
                {hasAvailability ? `${locker.availableCompartmentCount} available` : "No availability"}
              </Typography>
            </Stack>

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
