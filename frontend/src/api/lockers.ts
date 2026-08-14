import { apiClient } from "./client";
import type { LockerDetail, LockerListItem, LockerSearchParams } from "../types/locker";

export async function searchLockers(params: LockerSearchParams): Promise<LockerListItem[]> {
  const { data } = await apiClient.get<LockerListItem[]>("/lockers", {
    params: {
      location: params.location || undefined,
      distance: params.distance,
      size: params.size,
      availability: params.availability,
    },
  });
  return data;
}

export async function getLockerById(id: string): Promise<LockerDetail> {
  const { data } = await apiClient.get<LockerDetail>(`/lockers/${id}`);
  return data;
}
