import { apiClient } from "./client";
import type { AuthResponse, SignInRequest, SignUpRequest } from "../types/auth";

export async function signUp(request: SignUpRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/auth/signup", request);
  return data;
}

export async function signIn(request: SignInRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/auth/signin", request);
  return data;
}
