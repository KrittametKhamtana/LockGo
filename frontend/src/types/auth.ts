export interface SignUpRequest {
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
}

export interface SignInRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
}

export interface AuthUser {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
}
