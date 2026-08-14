import {
  Button,
  Checkbox,
  FormControlLabel,
  InputAdornment,
  MenuItem,
  Paper,
  Slider,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import MyLocationIcon from "@mui/icons-material/MyLocation";
import LocationOffIcon from "@mui/icons-material/LocationOff";
import SearchIcon from "@mui/icons-material/Search";
import { SIZE_LABEL, SIZE_ORDER, type CompartmentSize } from "../types/locker";

export interface Filters {
  search: string;
  location: string;
  distanceKm: number;
  size: CompartmentSize | "";
  availableOnly: boolean;
}

interface FilterBarProps {
  filters: Filters;
  onChange: (patch: Partial<Filters>) => void;
  onToggleLocation: () => void;
  locating: boolean;
}

export function FilterBar({ filters, onChange, onToggleLocation, locating }: FilterBarProps) {
  const usingLocation = filters.location.length > 0;

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack spacing={2}>
        <TextField
          fullWidth
          size="small"
          label="Search by name or address"
          placeholder="e.g. Silom, Riverside, Airport"
          value={filters.search}
          onChange={(e) => onChange({ search: e.target.value })}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            },
          }}
        />

        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}>
          <Button
            variant={usingLocation ? "contained" : "outlined"}
            startIcon={usingLocation ? <LocationOffIcon /> : <MyLocationIcon />}
            onClick={onToggleLocation}
            loading={locating}
            sx={{ whiteSpace: "nowrap" }}
          >
            {usingLocation ? "Using my location" : "Use my location"}
          </Button>

          <TextField
            select
            label="Compartment size"
            size="small"
            value={filters.size}
            onChange={(e) => onChange({ size: e.target.value as CompartmentSize | "" })}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">Any size</MenuItem>
            {SIZE_ORDER.map((size) => (
              <MenuItem key={size} value={size}>
                {SIZE_LABEL[size]}
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

        {usingLocation && (
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
