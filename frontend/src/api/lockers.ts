import { apiClient } from "./client";
import type {
  BookingWindowParams,
  LockerDetail,
  LockerListItem,
  LockerSearchParams,
} from "../types/locker";

export async function searchLockers(params: LockerSearchParams): Promise<LockerListItem[]> {
  const { data } = await apiClient.get<LockerListItem[]>("/lockers", {
    params: {
      location: params.location || undefined,
      distance: params.distance,
      size: params.size,
      availability: params.availability,
      search: params.search || undefined,
      startTime: params.startTime || undefined,
      durationHours: params.startTime ? params.durationHours : undefined,
    },
  });
  return data;
}

export async function getLockerById(id: string, window: BookingWindowParams = {}): Promise<LockerDetail> {
  const { data } = await apiClient.get<LockerDetail>(`/lockers/${id}`, {
    params: {
      startTime: window.startTime || undefined,
      durationHours: window.startTime ? window.durationHours : undefined,
    },
  });
  return data;
}
