# AI Code Review — รีวิวโค้ดที่ AI เขียน

---

## Flow การจอง

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

---

## Code / Bug

ปัญหาคือ

1. ถ้ามีการกด confirm การจอง แล้ว internet ผู้ใช้พัง อาจทำให้เกิด response
   หายกลางทาง
2. ทำให้ปุ่ม confirm enable มาให้กด และกรอกข้อมูลใหม่ได้ แต่ข้อมูลการจอง
   ลง table ไปแล้ว
3. เมื่อผู้ใช้เปลี่ยนข้อมูล แล้วกด confirm อีกครั้ง จะได้ใบจองเดิม เพราะ UUID
   ไม่ถูกเปลี่ยน เพราะ UUID ถูกสร้างครั้งเดียวตอนเปิดหน้าจอ แล้วไม่เปลี่ยนอีกเลย
   ในขณะที่ข้อมูลที่ผู้ใช้เลือก (เวลาเริ่ม / จำนวนชั่วโมง) แก้ได้ตลอดเวลา

```tsx
const idempotencyKey = useMemo(() => generateUUID(), []);

const [durationHours, setDurationHours] = useState(...);
const [startTime, setStartTime] = useState(...);
```

ส่วนฝั่ง server เมื่อเจอรหัสซ้ำ จะคืนของเดิมทันที โดยไม่ตรวจสอบว่าข้อมูล
ที่ส่งมารอบนี้ตรงกับรอบก่อนหรือไม่ ทำให้ผู้ใช้ได้ใบจองใบเดิม

```csharp
var existing = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
if (existing is not null)
{
    return MapToDto(existing);
}
```

---

## AI Generated Code

```tsx
const idempotencyKey = useMemo(() => generateUUID(), []);
```

## Review

รหัสควรผูกข้อมูลที่จะจอง เพื่อให้ gen UUID ใหม่ เพื่อให้ได้ใบจองใหม่
แล้วพัฒนา feature หน้าประวัติการจอง ไว้ให้ผู้ใช้รู้ และยกเลิกใบจองที่ค้างได้
หรือยกเลิก/ลบจาก admin

## Final Code

ต้องย้าย `useMemo` ลงไปไว้ใต้ `useState` ทั้งสองตัว และผูกข้อมูลที่ผู้ใช้แก้ไว้ด้วย

```tsx
const [durationHours, setDurationHours] = useState(...);
const [startTime, setStartTime] = useState(...);

const idempotencyKey = useMemo(() => generateUUID(), [startTime, durationHours]);
```

## Security

ถ้ามีการแก้ code จาก gen UUID แล้วเปลี่ยนวิธี random อื่นๆ ที่อาจคาดเดาได้
จะมีโอกาสเสี่ยงทำให้มีคนมาดูใบจองนี้ได้เลย

## Performance

ไม่มีปัญหา — gen UUID เร็วอยู่แล้ว ไม่ช้าเวลาข้อมูลเปลี่ยน

## Maintainable

ไม่มีปัญหา — แก้ code ที่ UI ที่เดียว ไม่กระทบส่วนอื่น อาจจะต้องมี comment
ว่าทำไมต้องสร้างรหัสใหม่เมื่อข้อมูลเปลี่ยน
