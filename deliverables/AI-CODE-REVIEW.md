# AI Code Review — รีวิวโค้ดที่ AI เขียน

เอกสารนี้คือการที่ **คนรีวิวโค้ดที่ AI สร้างขึ้นมา** ไม่ใช่ AI รีวิวตัวเอง
เลือกมา 1 ส่วนการทำงาน แล้วตรวจ 5 ประเด็น: ถูกต้องไหม / มีบั๊กไหม /
มีความเสี่ยงด้านความปลอดภัยไหม / มีปัญหา performance ไหม / ดูแลรักษาต่อง่ายไหม

---

## เลือกโค้ดส่วนไหนมารีวิว

**Flow การจอง ตั้งแต่หน้าจอจนถึง Service** — เป็น flow ที่ผู้ใช้กด Confirm
แล้วเกิดการจองจริง

```
ReservationPage.tsx  (หน้าจอ ยืนยันการจอง)
        ↓
useCreateReservation  (React Query mutation)
        ↓
api/reservations.ts  (ยิง POST /api/reservations)
        ↓
ReservationsController.Create  (รับ request)
        ↓
ReservationService.CreateAsync  (ตรวจ + สั่งจอง)
```

**ทำไมเลือกส่วนนี้**

1. เป็นเส้นทางที่ "ผิดแล้วเจ็บจริง" — เกี่ยวกับเงินและการจองของผู้ใช้
2. ครอบทุกชั้นของระบบในเส้นเดียว (UI → API → Business Logic)
3. **ยังไม่เคยถูกรีวิวมาก่อน** — [`AI_USAGE.md`](AI_USAGE.md) หัวข้อ 3 ที่ AI
   รีวิวตัวเองไว้ ครอบเฉพาะ *ข้างใน transaction* (`CreateInTransactionAsync`
   กับ `EfUnitOfWork`) ส่วน *ทางเข้าจากหน้าจอ* ยังไม่มีใครตรวจ

---

## สรุปผลตรวจ

| # | ประเด็น | หัวข้อ | ความรุนแรง | สถานะ |
|---|---|---|---|---|
| 1 | Idempotency key ค้างค่าเดิม แต่ข้อมูลที่ส่งเปลี่ยนได้ | มีบั๊กไหม | 🔴 สูง | เสนอให้แก้ |
| 2 | ดูรายการจองของคนอื่นได้ ไม่มีการเช็คเจ้าของ | Security | 🔴 สูง (ยังไม่เกิดตอนนี้) | บันทึกไว้ |
| 3 | โค้ด fallback ของ UUID พังในเคสที่ตัวเองดักไว้ | ถูกต้องไหม | 🟡 กลาง | เสนอให้แก้ |
| 4 | กติกาตรวจข้อมูลเขียนซ้ำ 2 ที่ | Maintainable | 🟡 กลาง | บันทึกไว้ |
| 5 | Retry เปิด transaction ใหม่ทุกรอบ | Performance | 🟢 ต่ำ | รับได้ |

---

## 🔴 1. มีบั๊กไหม — จองได้ผลลัพธ์ผิดแบบเงียบๆ

### อาการ

ผู้ใช้เลือก **4 ชั่วโมง** แต่ระบบคืนใบจอง **2 ชั่วโมง** มาให้
ไม่มี error ไม่มีคำเตือนอะไรเลย

### สาเหตุ

`idempotencyKey` คือรหัสกันจองซ้ำ ที่ frontend สร้างขึ้นมาแล้วแนบไปกับ request
ฝั่ง server ถ้าเจอรหัสนี้ซ้ำ จะถือว่า "อันนี้เคยจองไปแล้ว" แล้วคืนใบเดิมกลับมา

ปัญหาคือ **รหัสนี้ถูกสร้างครั้งเดียวตอนเปิดหน้าจอ แล้วไม่เปลี่ยนอีกเลย**
ในขณะที่ **ข้อมูลที่ผู้ใช้เลือก (เวลาเริ่ม / จำนวนชั่วโมง) แก้ได้ตลอดเวลา**

```tsx
// frontend/src/pages/ReservationPage.tsx:35
const idempotencyKey = useMemo(() => generateUUID(), []);
//                                                   ↑
//              วงเล็บว่าง = สร้างครั้งเดียว ไม่สร้างใหม่อีก

// แต่ 2 ค่านี้ผู้ใช้กดเปลี่ยนได้ตลอด
const [durationHours, setDurationHours] = useState(...);  // บรรทัด 38
const [startTime, setStartTime] = useState(...);          // บรรทัด 39
```

ส่วนฝั่ง server เจอรหัสซ้ำปุ๊บ คืนของเดิมทันที **ไม่เทียบเลยว่าข้อมูลที่ส่งมา
รอบนี้ตรงกับรอบก่อนไหม**

```csharp
// LockGo.Application/Services/ReservationService.cs:118-122
var existing = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
if (existing is not null)
{
    return MapToDto(existing);   // ← คืนของเดิมเลย ไม่เทียบ durationHours / startTime
}
```

### ลำดับเหตุการณ์ที่ทำให้พัง

1. ผู้ใช้เลือก **2 ชั่วโมง** กด Confirm
2. Server จองสำเร็จและบันทึกลง DB เรียบร้อย **แต่ response หายระหว่างทาง**
   (เน็ตมือถือหลุด / timeout)
3. หน้าจอขึ้น error ผู้ใช้ยังอยู่หน้าเดิม ปุ่มกดได้อีก
4. ผู้ใช้เปลี่ยนใจ เลือก **4 ชั่วโมง** กด Confirm ใหม่
5. ส่ง **รหัสเดิม** ไปพร้อมกับ 4 ชั่วโมง
6. Server เห็นรหัสซ้ำ → คืนใบจอง **2 ชั่วโมง** อันเดิม
7. หน้าจอเด้งไปหน้า confirmation แสดง 2 ชั่วโมง เหมือนสำเร็จปกติ

**ผู้ใช้ขอ 4 ชั่วโมง ได้ 2 ชั่วโมง และไม่มีอะไรบอกว่าผิด**

### ทำไม test 60 ตัวจับไม่ได้

ทุก test ที่ทดสอบเรื่องนี้ ยิงซ้ำด้วย **ข้อมูลชุดเดิม** เสมอ
ไม่มี test ตัวไหนยิง **รหัสเดิม + ข้อมูลต่าง** ซึ่งเป็นเคสที่พังจริง

> รากของปัญหา: idempotency key ควรผูกกับ **ข้อมูลที่จะจอง**
> ไม่ใช่ผูกกับ **การเปิดหน้าจอ**

---

## 🔴 2. Security — ดูใบจองของคนอื่นได้ ไม่มีการเช็คเจ้าของ

```csharp
// LockGo.Api/Controllers/ReservationsController.cs:36
[HttpGet("{id:guid}")]
public async Task<ActionResult<ReservationDto>> GetById(Guid id, CancellationToken ct)
{
    var reservation = await _reservationService.GetByIdAsync(id, ct);
    return Ok(reservation);   // ← รับ id มาแล้วคืนเลย
}
```

ไม่มี `[Authorize]` และไม่มีการเทียบว่า "คนที่ขอดู เป็นเจ้าของใบจองนี้จริงไหม"
ใครมี id ก็เปิดดูได้ทั้งหมด: ชื่อ locker, **ที่อยู่**, เวลาเริ่ม–หมดอายุ,
เลขที่การจอง

### แต่ — ตอนนี้ยังไม่ใช่ช่องโหว่ที่ใช้โจมตีได้จริง

เพราะทุกการจองยังผูกกับ user ปลอมตัวเดียว (`MockUser.Id`) ตามที่ตั้งใจ descope ไว้
แปลว่าตอนนี้ยัง **ไม่มีข้อมูล "ของคนอื่น" ให้รั่ว**

**สิ่งที่ต้องจำ:** นาทีที่เอา auth มาต่อกับการจองจริง (ให้แต่ละคนมีใบจองของตัวเอง)
บั๊กนี้จะกลายเป็นช่องโหว่ทันที — ต้องเพิ่มการเช็คเจ้าของ **ในงานเดียวกัน**
ไม่ใช่ค่อยตามแก้ทีหลัง

---

## 🟡 3. ถูกต้องไหม — โค้ดสำรองของ UUID พังในเคสที่ตัวเองดักไว้

```ts
// frontend/src/utils/uuid.ts
export function generateUUID(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
  //  ↑ ดักไว้ว่า crypto อาจไม่มีเลย
    return crypto.randomUUID();
  }

  const bytes = crypto.getRandomValues(new Uint8Array(16));
  //            ↑ แต่ตรงนี้เรียก crypto ตรงๆ ไม่ได้ดักอะไรเลย
```

ถ้า `crypto` ไม่มีจริงตามที่บรรทัดบนดักไว้ โค้ดจะตกมาบรรทัดล่างแล้ว
**พังด้วย `ReferenceError: crypto is not defined`** แทนที่จะทำงานสำรองให้

**ความรุนแรงจริง:** ต่ำ เพราะ browser สมัยใหม่มี `crypto` ทุกตัวแม้เข้าผ่าน HTTP
(ตัวที่ถูกจำกัดเฉพาะ HTTPS คือ `randomUUID` กับ `subtle` เท่านั้น)
แต่ประเด็นคือ **การดักบรรทัดบนสื่อว่าโค้ดทนทานกว่าความเป็นจริง** ซึ่งจะหลอก
คนที่มาอ่านทีหลัง

ไฟล์นี้เกิดจากการแก้บั๊กจริงตอน deploy (PR
[#1](https://github.com/KrittametKhamtana/LockGo/pull/1)) — แก้ถูกทางแล้ว
แต่ยังไม่ครบ

---

## 🟡 4. Maintainable — กติกาเดียวกันเขียนไว้ 2 ที่

| กติกา | ฝั่ง Frontend | ฝั่ง Backend |
|---|---|---|
| จองล่วงหน้าได้กี่วัน | `MAX_ADVANCE_DAYS = 30` | `MaxAdvanceBooking = 30 days` |
| ย้อนหลังได้กี่นาที | `PAST_GRACE_MINUTES = 5` | `StartTimePastGrace = 5 min` |
| จองได้กี่ชั่วโมง | ตัวเลือก 2–48 ชม. | รับ 1–72 ชม. |

ในโค้ดเองก็ยอมรับว่าลอกกันมา:

```ts
// frontend/src/utils/booking.ts:30
/** Mirrors the backend's clock-skew grace so a value it would accept isn't rejected here first. */
```

**ปัญหาที่จะเกิด:** ถ้าวันหลังแก้ backend เป็น 14 วัน แต่ลืมแก้ frontend
หน้าจอจะยังให้เลือก 30 วันอยู่ ผู้ใช้เลือกไป กด Confirm แล้วโดน error 400
โดยที่หน้าจอไม่ได้บอกอะไรล่วงหน้าเลย

**ทางแก้ที่แนะนำ:** ให้ backend ส่งค่าพวกนี้มาทาง config endpoint
หรืออย่างน้อยเขียน test ที่ assert ว่าค่าทั้งสองฝั่งตรงกัน

---

## 🟢 5. Performance — เจอเล็กน้อย รับได้

- **Retry loop เปิด transaction ใหม่ทุกรอบ** (`ReservationService.cs:82-110`)
  สูงสุด 5 รอบต่อ 1 request บน connection pool ที่ตั้งไว้แค่ 10
  → ตอนคนแย่งกันจองหนักๆ จะกิน connection เร็วกว่าที่คิด
  แต่มีเพดานชัดเจนอยู่แล้ว ไม่ใช่ปัญหาเฉียบพลัน

- **Cache ไม่ถูกล้างเมื่อ response หาย** (`useReservation.ts:13-16`)
  ล้าง cache เฉพาะตอนสำเร็จ → ในเคสข้อ 1 (จองติดแต่ response หาย)
  ผู้ใช้กด Back กลับไปจะเห็นจำนวนช่องว่างเป็นค่าเก่า

---

## ✅ ส่วนที่ตรวจแล้วไม่พบปัญหา

**การกันกดปุ่มซ้ำ ทำไว้ 3 ชั้นจริง และรัดกุมดี**

1. `submittingRef` — กันแบบทันที ใน tick เดียวกับที่กด ก่อน React จะทัน
   re-render ปุ่มให้เป็น disabled ด้วยซ้ำ (`ReservationPage.tsx:57, 73-76`)
2. `disabled={mutation.isPending}` — กันหลัง re-render (`ReservationPage.tsx:193`)
3. ฝั่ง server — เช็ค idempotency key + unique constraint ที่ระดับ DB
   (`ReservationService.cs:88-98`)

ชั้นไหนพลาด ชั้นถัดไปรับได้หมด

**การเรียก Hooks ถูกต้องตามกติกา React** — hooks ทุกตัวถูกเรียกก่อน
early return ที่บรรทัด 59 ไม่ผิด Rules of Hooks

**ราคาไม่เพี้ยน** — `Price` เป็นราคาต่อการจอง (ไม่ใช่ต่อชั่วโมง)
ทั้งใน seed data, DTO และหน้าจอ แสดงตรงกันหมด ไม่มีปัญหาหน่วยไม่ตรงกัน

> *(แต่มีคำถามเชิง business: จอง 2 วัน ควรราคาเท่าจอง 2 ชั่วโมงจริงหรือ?
> อันนี้เป็นเรื่อง requirement ไม่ใช่บั๊กของโค้ด)*

---

# AI Generated Code → Your Review → Final Code

สองจุดที่แก้จริง

## จุดที่ 1 — Idempotency key

### AI Generated Code

```tsx
// frontend/src/pages/ReservationPage.tsx:35
const idempotencyKey = useMemo(() => generateUUID(), []);
```

### Your Review

รหัสกันจองซ้ำถูกสร้างครั้งเดียวตอนเปิดหน้า แต่ข้อมูลที่ผู้ใช้เลือก
(เวลาเริ่ม, จำนวนชั่วโมง) แก้ได้ตลอด พอ request แรกส่งถึง server สำเร็จ
แต่ response หายกลางทาง ผู้ใช้จะแก้ตัวเลือกแล้วกดใหม่ด้วยรหัสเดิม
ทำให้ server คืนใบจองอันเก่ามาแทน — **ได้ผลลัพธ์ผิดโดยไม่มีสัญญาณเตือน**

รหัสนี้ควรผูกกับ *ข้อมูลที่จะจอง* ไม่ใช่ *การเปิดหน้าจอ*

### Final Code

```tsx
// สร้างรหัสใหม่เมื่อข้อมูลที่จะจองเปลี่ยน — กดซ้ำด้วยข้อมูลเดิมยังใช้รหัสเดิม
// (กันจองซ้ำได้เหมือนเดิม) แต่พอผู้ใช้เปลี่ยนใจ จะได้รหัสใหม่ทันที
const idempotencyKey = useMemo(() => generateUUID(), [startTime, durationHours]);
```

**เสริมฝั่ง server ได้อีก (ถ้าอยากแน่นกว่านี้):** เก็บ hash ของข้อมูลที่จองไว้
คู่กับรหัส ถ้ารหัสตรงแต่ข้อมูลไม่ตรง ให้ตอบ `409 IDEMPOTENCY_KEY_REUSED`
แทนที่จะคืนใบเก่ามาเงียบๆ (เป็นวิธีที่ Stripe ใช้)

**Test ที่ควรเพิ่ม:** ยิง 2 request ด้วยรหัสเดียวกันแต่ `durationHours` ต่างกัน
แล้ว assert ว่าต้องไม่ได้ใบจองอันเดิมกลับมาเฉยๆ

---

## จุดที่ 2 — UUID fallback

### AI Generated Code

```ts
// frontend/src/utils/uuid.ts
if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
  return crypto.randomUUID();
}

const bytes = crypto.getRandomValues(new Uint8Array(16));
```

### Your Review

บรรทัดแรกดักไว้ว่า `crypto` อาจไม่มีอยู่เลย แต่บรรทัดสำรองกลับเรียก
`crypto.getRandomValues` ตรงๆ โดยไม่ดักอะไร ถ้า `crypto` ไม่มีจริง
จะพังด้วย `ReferenceError` แทนที่จะทำงานสำรอง — **การดักบรรทัดแรก
สื่อความทนทานที่โค้ดไม่มีจริง**

### Final Code

```ts
export function generateUUID(): string {
  if (typeof crypto === "undefined" || typeof crypto.getRandomValues !== "function") {
    throw new Error("Secure random number generation is unavailable in this browser.");
  }

  if (typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  const bytes = crypto.getRandomValues(new Uint8Array(16));
  // ... เหมือนเดิม
}
```

เช็คสิ่งที่จำเป็นจริงก่อน (`getRandomValues`) แล้วค่อยเลือกทางที่ดีที่สุด
ถ้าไม่มีจริงก็ throw ข้อความที่อ่านรู้เรื่อง ดีกว่า `ReferenceError` ดิบๆ

---

## สรุป

**ผ่านแบบมีเงื่อนไข — แก้จุดที่ 1 ก่อน แล้วค่อยส่ง**

จุดที่ 1 เป็นบั๊กที่ต้องแก้จริง เพราะมันคืนผลลัพธ์ผิดให้ผู้ใช้แบบเงียบสนิท
ไม่มี error ให้เห็น ไม่มี log ให้ตาม แต่แก้ง่ายมาก — เติมตัวแปร 2 ตัว
ลงใน dependency array

จุดที่ 2 แก้เพราะมันถูกและง่าย ไม่ใช่เพราะเร่งด่วน

ข้อ 2 (Security) กับข้อ 4 (Maintainable) บันทึกไว้เป็นหนี้ทางเทคนิคที่รู้ตัว
พร้อมเงื่อนไขชัดเจนว่าต้องจัดการเมื่อไหร่ — ข้อ 2 ต้องแก้พร้อมกับตอนต่อ auth
เข้ากับการจองจริง

**ข้อสังเกตที่น่าสนใจของโปรเจกต์นี้:** นี่เป็นครั้งที่ 3 แล้วที่เจอ
"test ผ่านหมดแต่พิสูจน์ได้น้อยกว่าที่คิด" — ครั้งแรกคือ concurrency test
ที่ผ่านด้วยเหตุผลผิด ([`DEBUGGING.md`](DEBUGGING.md)) ครั้งที่สองคือ 2 บั๊ก
ที่เจอเฉพาะตอนต่อ DB จริง ([`AI_USAGE.md`](AI_USAGE.md#4-live-postgres-verification--and-two-real-bugs-it-caught))
และครั้งนี้คือ test ที่ยิงซ้ำด้วยข้อมูลชุดเดิมเสมอ เลยไม่มีวันเจอเคส
"รหัสเดิม + ข้อมูลต่าง"

บทเรียนร่วมกันคือ **ต้องถามเสมอว่า test กำลังพิสูจน์อะไรจริงๆ**
ไม่ใช่แค่ดูว่ามันเขียว
