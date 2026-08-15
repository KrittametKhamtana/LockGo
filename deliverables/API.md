# API Documentation

Base URL (local dev): `http://localhost:5117/api`

Interactive Swagger UI is available at `http://localhost:5117/swagger` when
running in Development.

All error responses share one shape:

```json
{ "error": { "code": "SOME_CODE", "message": "Human-readable explanation." } }
```

A locker can hold several compartments of the same size. The API always
reports availability **per size** (`sizeAvailability`) rather than per
individual compartment — booking picks a size and the server assigns a free
compartment of that size, so individual compartment IDs are never exposed
to the client except in the reservation result.

Availability everywhere (search, detail, and the booking write path) is an
overlap test against a **booking window** — `startTime`/`durationHours`,
defaulting to "right now" — so browsing a future slot and booking it agree
on what "free" means.

---

## Auth

No endpoint currently requires the token these issue — see
[`deliverables/ARCHITECTURE.md`](ARCHITECTURE.md) for why that's deliberate.
Passwords are hashed with BCrypt; tokens are JWTs signed with the `Jwt:Secret`
from configuration.

### `POST /api/auth/signup`

**Request**

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane@example.com",
  "username": "janedoe",
  "password": "at-least-8-characters",
  "confirmPassword": "at-least-8-characters"
}
```

**201 Created** — same shape as `/signin` below (auto-signs the new account in).

**Errors**

| Status | Code | Cause |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Missing name/username, invalid email, password under 8 characters, or `password` ≠ `confirmPassword` |
| 409 | `EMAIL_TAKEN` | An account with this email already exists |
| 409 | `USERNAME_TAKEN` | This username is already taken |

### `POST /api/auth/signin`

**Request** — `{ "username": "janedoe", "password": "..." }`

**200 OK**

```json
{
  "token": "eyJhbGciOi...",
  "userId": "b3f1c2b0-...",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane@example.com",
  "username": "janedoe"
}
```

**401 Unauthorized** — `{ "error": { "code": "INVALID_CREDENTIALS", ... } }`
for either a wrong username or wrong password, deliberately indistinguishable
so the error can't be used to enumerate valid usernames.

---

## `GET /api/lockers`

Search/filter lockers.

| Query param    | Type    | Description |
|----------------|---------|--------------|
| `location`     | string  | `"lat,lng"`, e.g. `13.7278,100.5241`. Omit to skip distance filtering/sorting — there's no device geolocation or geocoding service server-side; the client supplies coordinates (e.g. from the browser's own Geolocation API). |
| `distance`     | number  | Max distance in km from `location`. Ignored if `location` isn't supplied. |
| `size`         | string  | `S` \| `M` \| `L`. Only return lockers with at least one *available* compartment of this size when combined with `availability=true`; otherwise only that it offers the size at all. |
| `availability` | boolean | `true` to only return lockers with at least one available compartment (of the requested `size`, if given). |
| `search`       | string  | Free-text, case-insensitive match against locker name or address. |
| `startTime`    | datetime | ISO 8601. Report availability for this future moment instead of right now. |
| `durationHours` | int    | Paired with `startTime` — the length of the window to check. |

**200 OK**

```json
[
  {
    "id": "b3f1c2b0-...",
    "name": "LockGo Central Station",
    "address": "1 Silom Road, Bangkok",
    "lat": 13.7278,
    "lng": 100.5241,
    "operatingStatus": "Open",
    "distanceKm": 1.4,
    "minPrice": 20,
    "availableCompartmentCount": 6,
    "sizeAvailability": [
      { "size": "S", "price": 20, "availableCount": 3, "totalCount": 4 },
      { "size": "M", "price": 35, "availableCount": 2, "totalCount": 3 },
      { "size": "L", "price": 50, "availableCount": 1, "totalCount": 2 }
    ],
    "isFullyBooked": false
  }
]
```

- `distanceKm` is `null` when `location` wasn't supplied. Results are sorted
  by distance when it was.
- `sizeAvailability` only lists sizes the locker actually offers — a locker
  with no Large compartments simply has no `"size": "L"` entry.
- `minPrice`/`availableCompartmentCount` describe the requested `size` only
  when a `size` filter is active; otherwise they describe all sizes.
- `isFullyBooked` is `true` when the locker is `Open` but every compartment
  is currently taken — distinct from `operatingStatus: "Closed"`, which
  means the site itself isn't operating.

---

## `GET /api/lockers/{id}`

Locker detail with its per-size availability.

**200 OK**

```json
{
  "id": "b3f1c2b0-...",
  "name": "LockGo Central Station",
  "address": "1 Silom Road, Bangkok",
  "lat": 13.7278,
  "lng": 100.5241,
  "operatingStatus": "Open",
  "sizeAvailability": [
    { "size": "S", "price": 20, "availableCount": 3, "totalCount": 4 },
    { "size": "M", "price": 35, "availableCount": 0, "totalCount": 3 }
  ],
  "isFullyBooked": false
}
```

**404 Not Found** — `{ "error": { "code": "LOCKER_NOT_FOUND", ... } }`

---

## `POST /api/reservations`

Books a **locker + size** — the server assigns a specific free compartment
of that size inside the transaction, retrying (up to 5 attempts) against a
different compartment of the same size if it loses an optimistic-concurrency
race for the one it picked, so two concurrent requests for the same size
correctly land on two different compartments rather than one failing
outright while a sibling was free. **Idempotent** on `idempotencyKey` —
safe to retry, including a rapid double-click resending the same request;
see [`ARCHITECTURE.md`](ARCHITECTURE.md#the-concurrency-critical-path-post-apireservations).

`startTime` is client-supplied (advance booking, not just immediate-use) —
the server only checks it's within a sane range and uses it as-is for the
availability-overlap check.

**Request**

```json
{
  "lockerId": "b3f1c2b0-...",
  "size": "M",
  "durationHours": 2,
  "idempotencyKey": "b1f6c9de-2b2a-4e3a-9c0e-6e0b7a2f9a11",
  "startTime": "2026-08-14T09:00:00+00:00"
}
```

| Field            | Type     | Constraints |
|------------------|----------|-------------|
| `lockerId`       | guid     | required |
| `size`           | string   | `S` \| `M` \| `L` |
| `durationHours`  | int      | 1–72 |
| `idempotencyKey` | string   | required — a client-generated UUID, one per checkout attempt, reused across retries of that same attempt |
| `startTime`      | datetime | ISO 8601. Must not be more than 5 minutes in the past (grace window for clock skew / time spent on the confirm screen) nor more than 30 days in the future |

**201 Created**

```json
{
  "id": "f0a1...",
  "bookingNumber": "LG-20260814-7K3N9P",
  "lockerId": "b3f1c2b0-...",
  "lockerName": "LockGo Central Station",
  "lockerAddress": "1 Silom Road, Bangkok",
  "compartmentId": "d4e9...",
  "compartmentSize": "M",
  "price": 35,
  "startTime": "2026-08-14T09:00:00+00:00",
  "endTime": "2026-08-14T11:00:00+00:00",
  "status": "Active",
  "isActive": true
}
```

`compartmentId` identifies the specific compartment the server assigned —
useful for the confirmation screen, but never something the client chooses.

**Errors**

| Status | Code                    | Cause |
|--------|-------------------------|-------|
| 400    | `VALIDATION_ERROR`      | `durationHours` out of range, invalid `size`, missing `idempotencyKey`, `startTime` out of range, malformed request body |
| 404    | `COMPARTMENT_NOT_FOUND` | The locker doesn't offer that size at all |
| 409    | `LOCKER_CLOSED`         | The locker isn't open |
| 409    | `NO_AVAILABILITY`       | The locker offers that size, but every compartment of it is booked for the requested window (including after retrying against sibling compartments) |

---

## `GET /api/reservations/{id}`

**200 OK** — same shape as the `POST` response above.

**404 Not Found** — `{ "error": { "code": "RESERVATION_NOT_FOUND", ... } }`

---

## Error reference

| HTTP status | When |
|-------------|------|
| 400 | Invalid request body / query params |
| 401 | Sign-in failed (bad username or password) |
| 404 | Locker, size, or reservation not found |
| 409 | Locker closed, every compartment of the requested size is booked, or duplicate email/username on sign-up |
| 500 | Unhandled server error — no stack trace is ever returned to the client (see `AppExceptionHandler`) |
