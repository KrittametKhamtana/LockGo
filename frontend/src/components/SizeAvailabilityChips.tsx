import { Chip, Stack, Tooltip } from "@mui/material";
import { SIZE_LABEL, SIZE_ORDER, type CompartmentSizeAvailability } from "../types/locker";

interface SizeAvailabilityChipsProps {
  sizeAvailability: CompartmentSizeAvailability[];
  /** Show sizes the locker doesn't stock at all, greyed out. */
  showMissingSizes?: boolean;
}

export function SizeAvailabilityChips({ sizeAvailability, showMissingSizes = false }: SizeAvailabilityChipsProps) {
  const bySize = new Map(sizeAvailability.map((entry) => [entry.size, entry]));
  const sizes = showMissingSizes ? SIZE_ORDER : SIZE_ORDER.filter((size) => bySize.has(size));

  return (
    <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
      {sizes.map((size) => {
        const entry = bySize.get(size);

        if (!entry) {
          return (
            <Tooltip key={size} title={`This location doesn't have ${SIZE_LABEL[size]} lockers`}>
              <Chip size="small" variant="outlined" label={`${size} —`} sx={{ opacity: 0.45 }} />
            </Tooltip>
          );
        }

        const soldOut = entry.availableCount === 0;

        return (
          <Tooltip
            key={size}
            title={`${SIZE_LABEL[size]} · ฿${entry.price.toFixed(0)} · ${entry.availableCount} of ${entry.totalCount} free`}
          >
            <Chip
              size="small"
              variant={soldOut ? "outlined" : "filled"}
              color={soldOut ? "default" : "success"}
              label={`${size} ${entry.availableCount}/${entry.totalCount}`}
              sx={soldOut ? { opacity: 0.7 } : undefined}
            />
          </Tooltip>
        );
      })}
    </Stack>
  );
}
