import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5117/api",
  headers: {
    "Content-Type": "application/json",
  },
});

export interface ApiErrorBody {
  error: {
    code: string;
    message: string;
  };
}

export class ApiError extends Error {
  code: string;
  status: number;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status ?? 0;
    const body = error.response?.data as ApiErrorBody | undefined;

    if (body?.error) {
      return Promise.reject(new ApiError(status, body.error.code, body.error.message));
    }

    return Promise.reject(new ApiError(status, "UNKNOWN_ERROR", error.message ?? "Something went wrong."));
  },
);
