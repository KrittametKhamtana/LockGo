export type CompartmentSize = "S" | "M" | "L";
export type OperatingStatus = "Open" | "Closed";

export interface CompartmentSizeAvailability {
  size: CompartmentSize;
  price: number;
  availableCount: number;
  totalCount: number;
}

export interface LockerListItem {
  id: string;
  name: string;
  address: string;
  lat: number;
  lng: number;
  operatingStatus: OperatingStatus;
  distanceKm: number | null;
  minPrice: number;
  availableCompartmentCount: number;
  sizeAvailability: CompartmentSizeAvailability[];
  isFullyBooked: boolean;
}

export interface LockerDetail {
  id: string;
  name: string;
  address: string;
  lat: number;
  lng: number;
  operatingStatus: OperatingStatus;
  sizeAvailability: CompartmentSizeAvailability[];
  isFullyBooked: boolean;
}

export interface LockerSearchParams {
  location?: string;
  distance?: number;
  size?: CompartmentSize;
  availability?: boolean;
  search?: string;
}

export const SIZE_LABEL: Record<CompartmentSize, string> = {
  S: "Small",
  M: "Medium",
  L: "Large",
};

/** Fixed order so the S/M/L breakdown reads consistently everywhere. */
export const SIZE_ORDER: CompartmentSize[] = ["S", "M", "L"];
