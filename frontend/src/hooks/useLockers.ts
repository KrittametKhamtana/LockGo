import { useQuery } from "@tanstack/react-query";
import { getLockerById, searchLockers } from "../api/lockers";
import type { LockerSearchParams } from "../types/locker";

export function useLockers(params: LockerSearchParams) {
  return useQuery({
    queryKey: ["lockers", params],
    queryFn: () => searchLockers(params),
  });
}

export function useLocker(id: string | undefined) {
  return useQuery({
    queryKey: ["locker", id],
    queryFn: () => getLockerById(id!),
    enabled: Boolean(id),
  });
}
