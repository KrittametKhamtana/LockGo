import type { CompartmentSize } from "./locker";

export interface Filters {
  search: string;
  location: string;
  distanceKm: number;
  size: CompartmentSize | "";
  availableOnly: boolean;
  /** datetime-local value; empty means "right now". */
  startTime: string;
  durationHours: number;
}

export const DEFAULT_FILTERS: Filters = {
  search: "",
  location: "",
  distanceKm: 10,
  size: "",
  availableOnly: false,
  startTime: "",
  durationHours: 2,
};
