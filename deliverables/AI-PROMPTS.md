# AI Prompts

The actual prompt sequence used to drive this project's build with Claude
Code, in the order they were given. Each one was written with a specific
goal for what the AI should come away understanding or doing next — laid
out below alongside the prompt itself.

## Prompt 1 — Set the AI's role

> "ตอนนี้นายเป็น AI Full Stack Developer ที่เก่งมาก พร้อมรับงาน"
> *("You are now a highly skilled AI Full Stack Developer, ready to take on
> the work.")*

**Goal:** get the AI to understand its own role before anything else — what
it's about to be, going into the rest of the session.

## Prompt 2 — Establish the business context

> "ตอนนี้เราจะทำโปรเจคนึง โดยมี Business แบบนี้ grill me ถ้าสงสัย" (พร้อมแนบไฟล์)
> *("We're going to build a project — here's the business context. Grill me
> if anything's unclear." — with a spec document attached.)*

**Goal:** make sure the AI understands the business structure first, and
actively asks questions about anything it's unsure of, instead of guessing.

## Prompt 3 — Plan and confirm requirements before building

> "plan และสรุป requirement จากเอกสารที่ส่งไป โดยเราจะทำเป็น web application
> แล้วสร้าง System Architecture, DB, Wireframe, API และเอกสารจำเป็นอื่นๆ"
> *("Plan and summarize the requirements from the document I sent — we're
> building a web application, so produce the System Architecture, DB design,
> Wireframe, API design, and any other necessary documentation.")*

**Goal:** get a plan down first, so it can be reviewed before any real
implementation starts — both to avoid burning tokens on the AI re-thinking
its approach repeatedly, and so the AI can loop back and re-read this
context before continuing work instead of drifting from it.

## Prompt 4 — Lock in the tech stack and start building

> "เราจะเริ่มทำกันเลย โดย จะมี tech stack ตามนี้
> - frontend ใช้ React+Vite+Typescript+MUI version ล่าสุด
> - backend ใช้ .Net Core version ล่าสุด
> - DB ใช้ postgres
>
> โดยเขียนออกมาให้ maintain ง่าย, security และ มี performance ที่ดี ส่วนไหนไม่เหมาะตาม
> business, requirement ให้ถามชั้นเลย"
> *("Let's start building. Tech stack: frontend — React + Vite + TypeScript
> + MUI, latest versions; backend — .NET Core, latest version; DB —
> PostgreSQL. Write it to be easy to maintain, secure, and performant. If
> anything doesn't fit the business or requirements, ask me directly.")*

**Goal:** give the AI everything it needs to know about what to use so it
can start implementing right away, aimed at producing code in exactly the
shape intended — with an explicit standing invitation to push back and ask
if the stack or an instruction doesn't actually fit.

## Prompt 5 — Report a bug

> "ที่หน้าจอ xxx มี bug ที่ dropdown xxx คือ ค่า default ไม่แสดงที่ dropdown
> ในขณะที่ค่าถูก select แล้ว"
> *("On screen xxx there's a bug in the xxx dropdown — the default value
> doesn't show in the dropdown even though a value has already been
> selected.")*

**Goal:** point at the exact spot as precisely as possible, so the AI can
locate the issue quickly and correctly instead of searching broadly.
