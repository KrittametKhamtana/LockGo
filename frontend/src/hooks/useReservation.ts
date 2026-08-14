import { useMutation, useQuery } from "@tanstack/react-query";
import { createReservation, getReservationById } from "../api/reservations";
import type { CreateReservationRequest } from "../types/reservation";

export function useCreateReservation() {
  return useMutation({
    mutationFn: (request: CreateReservationRequest) => createReservation(request),
  });
}

export function useReservationQuery(id: string | undefined) {
  return useQuery({
    queryKey: ["reservation", id],
    queryFn: () => getReservationById(id!),
    enabled: Boolean(id),
  });
}
