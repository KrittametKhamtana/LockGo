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
import { DEFAULT_FILTERS, type Filters } from "../types/filters";
import { DURATION_OPTIONS, MAX_ADVANCE_DAYS, toLocalInputValue } from "../utils/booking";

interface FilterBarProps {
  filters: Filters;
  onChange: (patch: Partial<Filters>) => void;
  onToggleLocation: () => void;
  onReset: () => void;
  locating: boolean;
}

export function FilterBar({ filters, onChange, onToggleLocation, onReset, locating }: FilterBarProps) {
  const usingLocation = filters.location.length > 0;
  const isDefault = JSON.stringify(filters) === JSON.stringify(DEFAULT_FILTERS);
  const bookingLater = filters.startTime.length > 0;
  const maxStartTime = toLocalInputValue(
    new Date(Date.now() + MAX_ADVANCE_DAYS * 24 * 60 * 60 * 1000),
  );

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
            slotProps={{ inputLabel: { shrink: true }, select: { displayEmpty: true } }}
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

        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={2}
          alignItems={{ sm: "center" }}
          justifyContent="space-between"
        >
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={bookingLater}
                  onChange={(e) =>
                    onChange({ startTime: e.target.checked ? toLocalInputValue(new Date()) : "" })
                  }
                />
              }
              label="Book for a later time"
              sx={{ whiteSpace: "nowrap" }}
            />

            {bookingLater && (
              <>
                <TextField
                  type="datetime-local"
                  label="Starts"
                  size="small"
                  value={filters.startTime}
                  onChange={(e) => onChange({ startTime: e.target.value })}
                  slotProps={{
                    inputLabel: { shrink: true },
                    htmlInput: { min: toLocalInputValue(new Date()), max: maxStartTime },
                  }}
                  sx={{ minWidth: 220 }}
                />

                <TextField
                  select
                  label="For"
                  size="small"
                  value={filters.durationHours}
                  onChange={(e) => onChange({ durationHours: Number(e.target.value) })}
                  sx={{ minWidth: 140 }}
                >
                  {DURATION_OPTIONS.map((option) => (
                    <MenuItem key={option.hours} value={option.hours}>
                      {option.label}
                    </MenuItem>
                  ))}
                </TextField>
              </>
            )}
          </Stack>

          <Button color="inherit" onClick={onReset} disabled={isDefault} sx={{ whiteSpace: "nowrap" }}>
            Reset filters
          </Button>
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
