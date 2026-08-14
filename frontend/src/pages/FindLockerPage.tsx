import { useState } from "react";
import { Alert, CircularProgress, Stack, Typography } from "@mui/material";
import { FilterBar, type Filters } from "../components/FilterBar";
import { LockerCard } from "../components/LockerCard";
import { useLockers } from "../hooks/useLockers";
import { useDebouncedValue } from "../hooks/useDebouncedValue";

const DEFAULT_FILTERS: Filters = {
  search: "",
  location: "",
  distanceKm: 10,
  size: "",
  availableOnly: false,
};

export function FindLockerPage() {
  const [filters, setFilters] = useState<Filters>(DEFAULT_FILTERS);
  const [locating, setLocating] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);

  // Avoids a request per keystroke while typing in the search box.
  const debouncedSearch = useDebouncedValue(filters.search, 300);

  const { data: lockers, isLoading, isError } = useLockers({
    search: debouncedSearch,
    location: filters.location,
    distance: filters.location ? filters.distanceKm : undefined,
    size: filters.size || undefined,
    availability: filters.availableOnly || undefined,
  });

  function handleToggleLocation() {
    // Toggling off just clears the coordinates — distance filtering and sorting
    // are both driven by their presence, so the list falls back to unsorted.
    if (filters.location) {
      setFilters((prev) => ({ ...prev, location: "" }));
      setLocationError(null);
      return;
    }

    if (!navigator.geolocation) {
      setLocationError("Your browser doesn't support geolocation.");
      return;
    }

    setLocating(true);
    setLocationError(null);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setFilters((prev) => ({
          ...prev,
          location: `${position.coords.latitude},${position.coords.longitude}`,
        }));
        setLocating(false);
      },
      () => {
        setLocationError("Couldn't get your location — showing all lockers instead.");
        setLocating(false);
      },
    );
  }

  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Find a locker</Typography>
        <Typography variant="body1" color="text.secondary">
          Search nearby lockers and reserve a compartment in seconds.
        </Typography>
      </div>

      <FilterBar
        filters={filters}
        onChange={(patch) => setFilters((prev) => ({ ...prev, ...patch }))}
        onToggleLocation={handleToggleLocation}
        locating={locating}
      />

      {locationError && <Alert severity="warning">{locationError}</Alert>}

      {isLoading && (
        <Stack alignItems="center" py={6}>
          <CircularProgress />
        </Stack>
      )}

      {isError && <Alert severity="error">Couldn't load lockers. Please try again.</Alert>}

      {lockers && lockers.length === 0 && (
        <Alert severity="info">No lockers match your filters.</Alert>
      )}

      <Stack spacing={2}>
        {lockers?.map((locker) => (
          <LockerCard key={locker.id} locker={locker} />
        ))}
      </Stack>
    </Stack>
  );
}
