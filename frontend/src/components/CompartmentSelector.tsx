import { Card, CardActionArea, CardContent, Chip, Stack, Typography } from "@mui/material";
import { SIZE_LABEL, type CompartmentSize, type CompartmentSizeAvailability } from "../types/locker";

interface CompartmentSelectorProps {
  sizeAvailability: CompartmentSizeAvailability[];
  selectedSize: CompartmentSize | null;
  onSelect: (entry: CompartmentSizeAvailability) => void;
  /** Blocks selection entirely (e.g. the locker itself is closed). */
  disabled?: boolean;
}

export function CompartmentSelector({
  sizeAvailability,
  selectedSize,
  onSelect,
  disabled = false,
}: CompartmentSelectorProps) {
  return (
    <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
      {sizeAvailability.map((entry) => {
        // A size can only be booked while at least one of its compartments is
        // free — the count, not a per-compartment status, is what gates this.
        const selectable = !disabled && entry.availableCount > 0;
        const isSelected = entry.size === selectedSize;

        return (
          <Card
            key={entry.size}
            variant="outlined"
            sx={{
              flex: 1,
              borderColor: isSelected ? "primary.main" : undefined,
              borderWidth: isSelected ? 2 : 1,
              opacity: selectable ? 1 : 0.55,
            }}
          >
            <CardActionArea disabled={!selectable} onClick={() => onSelect(entry)} sx={{ p: 1 }}>
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center" gap={1}>
                  <Typography variant="subtitle1" fontWeight={700}>
                    {SIZE_LABEL[entry.size]}
                  </Typography>
                  <Chip
                    size="small"
                    label={entry.availableCount > 0 ? `${entry.availableCount} left` : "Full"}
                    color={entry.availableCount > 0 ? "success" : "default"}
                  />
                </Stack>

                <Typography variant="h6" sx={{ mt: 1 }}>
                  ฿{entry.price.toFixed(0)}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {entry.availableCount} of {entry.totalCount} free
                </Typography>
              </CardContent>
            </CardActionArea>
          </Card>
        );
      })}
    </Stack>
  );
}
