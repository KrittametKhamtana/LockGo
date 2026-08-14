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
of that size inside the transaction. **Idempotent** on `idempotencyKey` —
safe to retry, including a rapid double-click resending the same request;
see [`docs/ARCHITECTURE.md`](ARCHITECTURE.md#the-concurrency-critical-path-post-apireservations).

`startTime` is always set server-side to the moment the request is
processed (this is an immediate-use locker booking) — the client only
chooses a duration.

**Request**

```json
{
  "lockerId": "b3f1c2b0-...",
  "size": "M",
  "durationHours": 2,
  "idempotencyKey": "b1f6c9de-2b2a-4e3a-9c0e-6e0b7a2f9a11"
}
```

| Field            | Type   | Constraints |
|------------------|--------|-------------|
| `lockerId`       | guid   | required |
| `size`           | string | `S` \| `M` \| `L` |
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

`compartmentId` identifies the specific compartment the server assigned —
useful for the confirmation screen, but never something the client chooses.

**Errors**

| Status | Code                    | Cause |
|--------|-------------------------|-------|
| 400    | `VALIDATION_ERROR`      | `durationHours` out of range, invalid `size`, missing `idempotencyKey`, malformed request body |
| 404    | `COMPARTMENT_NOT_FOUND` | The locker doesn't offer that size at all |
| 409    | `LOCKER_CLOSED`         | The locker isn't open |
| 409    | `NO_AVAILABILITY`       | The locker offers that size, but every compartment of it is currently booked |

---

## `GET /api/reservations/{id}`

**200 OK** — same shape as the `POST` response above.

**404 Not Found** — `{ "error": { "code": "RESERVATION_NOT_FOUND", ... } }`

---

## Error reference

| HTTP status | When |
|-------------|------|
| 400 | Invalid request body / query params |
| 404 | Locker, size, or reservation not found |
| 409 | Locker closed, or every compartment of the requested size is booked |
| 500 | Unhandled server error — no stack trace is ever returned to the client (see `AppExceptionHandler`) |
