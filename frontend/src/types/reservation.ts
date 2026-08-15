import type { CompartmentSize } from "./locker";

export type ReservationStatus = "Active" | "Completed" | "Cancelled";

export interface CreateReservationRequest {
  lockerId: string;
  size: CompartmentSize;
  durationHours: number;
  idempotencyKey: string;
  /** ISO 8601 with offset — when the booking should start. */
  startTime: string;
}

export interface Reservation {
  id: string;
  bookingNumber: string;
  lockerId: string;
  lockerName: string;
  lockerAddress: string;
  compartmentId: string;
  compartmentSize: CompartmentSize;
  price: number;
  startTime: string;
  endTime: string;
  status: ReservationStatus;
  isActive: boolean;
}
