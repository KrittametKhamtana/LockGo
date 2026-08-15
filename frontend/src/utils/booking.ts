export const MAX_ADVANCE_DAYS = 30;

export const DURATION_OPTIONS = [
  { hours: 2, label: "2 hours" },
  { hours: 4, label: "4 hours" },
  { hours: 8, label: "8 hours" },
  { hours: 24, label: "1 day" },
  { hours: 48, label: "2 days" },
];

/**
 * datetime-local speaks the user's wall clock with no offset, so the value has
 * to be built from local parts — toISOString() here would shift by the offset
 * and show the wrong time in the field.
 */
export function toLocalInputValue(date: Date): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

/** Parses a datetime-local field value, or null when it's empty/unparseable. */
export function parseLocalInputValue(localValue: string): Date | null {
  if (!localValue) {
    return null;
  }
  const date = new Date(localValue);
  return Number.isNaN(date.getTime()) ? null : date;
}

/** Mirrors the backend's clock-skew grace so a value it would accept isn't rejected here first. */
export const PAST_GRACE_MINUTES = 5;

export function isValidStartTime(date: Date): boolean {
  const now = Date.now();
  return (
    date.getTime() >= now - PAST_GRACE_MINUTES * 60_000 &&
    date.getTime() <= now + MAX_ADVANCE_DAYS * 24 * 60 * 60 * 1000
  );
}
