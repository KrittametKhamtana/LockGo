import {
  Button,
  Checkbox,
  FormControlLabel,
  MenuItem,
  Paper,
  Slider,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import MyLocationIcon from "@mui/icons-material/MyLocation";
import type { CompartmentSize } from "../types/locker";

export interface Filters {
  location: string;
  distanceKm: number;
  size: CompartmentSize | "";
  availableOnly: boolean;
}

interface FilterBarProps {
  filters: Filters;
  onChange: (patch: Partial<Filters>) => void;
  onUseMyLocation: () => void;
  locating: boolean;
}

const SIZE_OPTIONS: { value: CompartmentSize | ""; label: string }[] = [
  { value: "", label: "Any size" },
  { value: "S", label: "Small" },
  { value: "M", label: "Medium" },
  { value: "L", label: "Large" },
];

export function FilterBar({ filters, onChange, onUseMyLocation, locating }: FilterBarProps) {
  const hasLocation = filters.location.length > 0;

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}>
          <Button
            variant="outlined"
            startIcon={<MyLocationIcon />}
            onClick={onUseMyLocation}
            loading={locating}
            sx={{ whiteSpace: "nowrap" }}
          >
            {hasLocation ? "Location set" : "Use my location"}
          </Button>

          <TextField select label="Compartment size" size="small" value={filters.size} onChange={(e) => onChange({ size: e.target.value as CompartmentSize | "" })} sx={{ minWidth: 160 }}>
            {SIZE_OPTIONS.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </TextField>

          <FormControlLabel
            control={
              <Checkbox
                checked={filters.availableOnly}
                onChange={(e) => onChange({ availableOnly: e.target.checked })}
              />
            }
            label="Available only"
          />
        </Stack>

        {hasLocation && (
          <Stack direction="row" spacing={2} alignItems="center">
            <Typography variant="body2" sx={{ whiteSpace: "nowrap" }} color="text.secondary">
              Within {filters.distanceKm} km
            </Typography>
            <Slider
              value={filters.distanceKm}
              onChange={(_, value) => onChange({ distanceKm: value as number })}
              min={1}
              max={50}
              sx={{ maxWidth: 320 }}
            />
          </Stack>
        )}
      </Stack>
    </Paper>
  );
}
