import { useMutation } from "@tanstack/react-query";
import { signIn, signUp } from "../api/auth";
import { useAuth } from "./useAuth";

export function useSignUp() {
  const { login } = useAuth();

  return useMutation({
    mutationFn: signUp,
    onSuccess: login,
  });
}

export function useSignIn() {
  const { login } = useAuth();

  return useMutation({
    mutationFn: signIn,
    onSuccess: login,
  });
}
