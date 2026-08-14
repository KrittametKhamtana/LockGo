# API Documentation

Base URL (local dev): `http://localhost:5117/api`

Interactive Swagger UI is available at `http://localhost:5117/swagger` when
running in Development.

All error responses share one shape:

```json
{ "error": { "code": "SOME_CODE", "message": "Human-readable explanation." } }
```

---

## `GET /api/lockers`

Search/filter lockers.

| Query param    | Type    | Description |
|----------------|---------|--------------|
| `location`     | string  | `"lat,lng"`, e.g. `13.7278,100.5241`. Omit to skip distance filtering/sorting — there's no device geolocation or geocoding service server-side; the client supplies coordinates (e.g. from the browser's own Geolocation API). |
| `distance`     | number  | Max distance in km from `location`. Ignored if `location` isn't supplied. |
| `size`         | string  | `S` \| `M` \| `L`. Only return lockers with at least one compartment of this size. |
| `availability` | boolean | `true` to only return lockers with at least one `Available` compartment. |

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
    "availableCompartmentCount": 3
  }
]
```

`distanceKm` is `null` when `location` wasn't supplied. Results are sorted
by distance when it was.

---

## `GET /api/lockers/{id}`

Locker detail with its compartments.

**200 OK**

```json
{
  "id": "b3f1c2b0-...",
  "name": "LockGo Central Station",
  "address": "1 Silom Road, Bangkok",
  "lat": 13.7278,
  "lng": 100.5241,
  "operatingStatus": "Open",
  "compartments": [
    { "id": "...", "size": "S", "price": 20, "status": "Available" },
    { "id": "...", "size": "M", "price": 35, "status": "Occupied" },
    { "id": "...", "size": "L", "price": 50, "status": "Available" }
  ]
}
```

**404 Not Found** — `{ "error": { "code": "LOCKER_NOT_FOUND", ... } }`

---

## `POST /api/reservations`

Creates a reservation. **Idempotent** on `idempotencyKey` — safe to retry,
including a rapid double-click resending the same request; see
[`docs/ARCHITECTURE.md`](ARCHITECTURE.md#the-concurrency-critical-path-post-apireservations).

`startTime` is always set server-side to the moment the request is
processed (this is an immediate-use locker booking) — the client only
chooses a duration.

**Request**

```json
{
  "compartmentId": "d4e9...",
  "durationHours": 2,
  "idempotencyKey": "b1f6c9de-2b2a-4e3a-9c0e-6e0b7a2f9a11"
}
```

| Field            | Type   | Constraints |
|------------------|--------|-------------|
| `compartmentId`  | guid   | required |
| `durationHours`  | int    | 1–72 |
| `idempotencyKey` | string | required — a client-generated UUID, one per checkout attempt, reused across retries of that same attempt |

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

**Errors**

| Status | Code                  | Cause |
|--------|-----------------------|-------|
| 400    | `VALIDATION_ERROR`    | `durationHours` out of range, missing `idempotencyKey`, malformed request body |
| 404    | `COMPARTMENT_NOT_FOUND` | `compartmentId` doesn't exist |
| 409    | `LOCKER_CLOSED`       | The compartment's locker isn't open |
| 409    | `NO_AVAILABILITY`     | The compartment is already booked for an overlapping window |

---

## `GET /api/reservations/{id}`

**200 OK** — same shape as the `POST` response above.

**404 Not Found** — `{ "error": { "code": "RESERVATION_NOT_FOUND", ... } }`

---

## Error reference

| HTTP status | When |
|-------------|------|
| 400 | Invalid request body / query params |
| 404 | Locker, compartment, or reservation not found |
| 409 | Locker closed, or compartment unavailable for the requested window |
| 500 | Unhandled server error — no stack trace is ever returned to the client (see `AppExceptionHandler`) |
