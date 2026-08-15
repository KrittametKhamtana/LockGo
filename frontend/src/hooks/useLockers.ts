import { useQuery } from "@tanstack/react-query";
import { getLockerById, searchLockers } from "../api/lockers";
import type { BookingWindowParams, LockerSearchParams } from "../types/locker";

export function useLockers(params: LockerSearchParams) {
  return useQuery({
    queryKey: ["lockers", params],
    queryFn: () => searchLockers(params),
  });
}

export function useLocker(id: string | undefined, window: BookingWindowParams = {}) {
  return useQuery({
    queryKey: ["locker", id, window],
    queryFn: () => getLockerById(id!, window),
    enabled: Boolean(id),
  });
}
