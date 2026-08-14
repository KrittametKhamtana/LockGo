export type CompartmentSize = "S" | "M" | "L";
export type CompartmentStatus = "Available" | "Occupied";
export type OperatingStatus = "Open" | "Closed";

export interface Compartment {
  id: string;
  size: CompartmentSize;
  price: number;
  status: CompartmentStatus;
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
}

export interface LockerDetail {
  id: string;
  name: string;
  address: string;
  lat: number;
  lng: number;
  operatingStatus: OperatingStatus;
  compartments: Compartment[];
}

export interface LockerSearchParams {
  location?: string;
  distance?: number;
  size?: CompartmentSize;
  availability?: boolean;
}
