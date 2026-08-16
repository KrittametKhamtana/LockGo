import { apiClient } from "./client";
import type { CreateReservationRequest, Reservation } from "../types/reservation";

export async function createReservation(request: CreateReservationRequest): Promise<Reservation> {
  const { data } = await apiClient.post<Reservation>("/reservations", request);
  return data;
}

export async function getReservationByBookingNumber(bookingNumber: string): Promise<Reservation> {
  const { data } = await apiClient.get<Reservation>(`/reservations/${bookingNumber}`);
  return data;
}
