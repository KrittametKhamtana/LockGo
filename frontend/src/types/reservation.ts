import type { CompartmentSize } from "./locker";

export type ReservationStatus = "Active" | "Completed" | "Cancelled";

export interface CreateReservationRequest {
  lockerId: number;
  size: CompartmentSize;
  durationHours: number;
  idempotencyKey: string;
  /** ISO 8601 with offset — when the booking should start. */
  startTime: string;
}

export interface Reservation {
  id: number;
  /** The public lookup key — this is what the confirmation URL uses, never `id`. */
  bookingNumber: string;
  lockerId: number;
  lockerName: string;
  lockerAddress: string;
  compartmentId: number;
  compartmentSize: CompartmentSize;
  price: number;
  startTime: string;
  endTime: string;
  status: ReservationStatus;
  isActive: boolean;
}
