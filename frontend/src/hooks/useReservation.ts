import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createReservation, getReservationById } from "../api/reservations";
import type { CreateReservationRequest } from "../types/reservation";

export function useCreateReservation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateReservationRequest) => createReservation(request),
    // The booked compartment changes availability for its locker, so cached
    // list/detail results are stale the instant this succeeds — don't wait
    // out staleTime for the user to see it reflected when they navigate back.
    onSuccess: (_reservation, request) => {
      queryClient.invalidateQueries({ queryKey: ["lockers"] });
      queryClient.invalidateQueries({ queryKey: ["locker", request.lockerId] });
    },
  });
}

export function useReservationQuery(id: string | undefined) {
  return useQuery({
    queryKey: ["reservation", id],
    queryFn: () => getReservationById(id!),
    enabled: Boolean(id),
  });
}
