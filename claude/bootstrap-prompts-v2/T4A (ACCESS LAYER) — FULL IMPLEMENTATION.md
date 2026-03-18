# WHYCESPACE — WBSM v3 STRICT MODE
## T4A (ACCESS LAYER) — FULL IMPLEMENTATION

You are implementing the T4A (Access Layer) of Whycespace.

⚠️ THIS IS A PURE ACCESS LAYER
⚠️ NO BUSINESS LOGIC ALLOWED
⚠️ MUST STRICTLY FOLLOW WBSM v3

---

# 🧠 SYSTEM DEFINITION

T4A = Access Layer

Responsibilities:
- API exposure
- Request validation
- Command mapping
- Response shaping
- Gateway security

---

# 🔒 HARD RULES

## ❌ T4A MUST NOT:

- Contain business logic
- Call T2E engines
- Call T3I engines directly
- Access database
- Perform calculations

---

## ✅ T4A MUST:

- Call T1M dispatcher ONLY
- Map request → command
- Return response DTOs
- Enforce authentication/authorization

---

# 🧱 TARGET STRUCTURE

src/engines/T4A/

├ api/
├ applications/
├ experience/
├ gateway/
├ contracts/
├ middleware/
├ tools/
└ tests/

---

# 🧱 PART 1 — CONTRACTS (FOUNDATION)

Create:

contracts/

---

## IMPLEMENT:

contracts/
├ requests/
├ responses/
├ dto/
└ mappings/

---

### EXAMPLES

Create:

- AllocateCapitalRequest.cs
- AllocateCapitalResponse.cs
- VaultAllocationDto.cs

---

## MAPPING

Create mapping layer:

Map:
Request → Command  
CommandResult → Response  

---

# 🧱 PART 2 — API LAYER

Create:

api/controllers/

---

## IMPLEMENT CONTROLLERS

### Example:

CapitalController.cs

- POST /capital/allocate
- POST /capital/contribute

VaultController.cs

- POST /vault/allocate
- POST /vault/transfer

---

## REQUIREMENTS

- Use DTOs
- Validate input
- Call application layer
- Return IActionResult

---

# 🧱 PART 3 — APPLICATION LAYER

Create:

applications/

---

## STRUCTURE

applications/
├ capital/
├ vault/
├ property/
├ identity/
└ workforce/

---

## IMPLEMENT

Each application:

- Receives request DTO
- Maps to command
- Calls dispatcher
- Returns response DTO

---

## EXAMPLE

AllocateCapitalApplication.cs

---

# 🧱 PART 4 — GATEWAY (CRITICAL)

Create:

gateway/

---

## IMPLEMENT

- RequestRouter
- AuthenticationHandler
- AuthorizationHandler
- RateLimiter

---

## REQUIREMENTS

- Validate identity via WhyceID
- Enforce access rules
- Forward to API

---

# 🧱 PART 5 — MIDDLEWARE

Create:

middleware/

---

## IMPLEMENT

- LoggingMiddleware
- ExceptionMiddleware
- ValidationMiddleware
- PolicyEnforcementMiddleware
- TraceMiddleware

---

## REQUIREMENTS

- Attach CorrelationId
- Log all requests
- Enforce policy at entry

---

# 🧱 PART 6 — EXPERIENCE LAYER

Create:

experience/

---

## STRUCTURE

experience/
├ admin/
├ investor/
├ operator/
├ mobile/
└ web/

---

## PURPOSE

- Shape response for UI
- No logic
- Only formatting

---

# 🧱 PART 7 — TOOLS

Create:

tools/

---

## IMPLEMENT

- Debug endpoints
- System inspection tools
- Dev CLI tools

---

# 🧱 PART 8 — DISPATCH INTEGRATION

Inject:

IRuntimeDispatcher

---

## FLOW

Controller
→ Application Layer
→ Dispatcher
→ T1M
→ T2E

---

# 🧱 PART 9 — READ API (T3I INTEGRATION)

## IMPLEMENT READ ENDPOINTS

- GET /analytics/*
- GET /monitoring/*
- GET /reports/*

---

## REQUIREMENTS

- Read from projection store OR query service
- NO direct T3I engine calls

---

# 🧱 PART 10 — SECURITY

## IMPLEMENT

- JWT authentication
- Role-based access (RBAC)
- Policy enforcement

---

# 🧱 PART 11 — TESTS

Create:

tests/

---

## INCLUDE

- Controller tests
- Integration tests
- Contract tests

---

# 🔁 DATA FLOW

Client
→ Gateway
→ Middleware
→ API Controller
→ Application Layer
→ Dispatcher
→ T1M → T2E

---

# 🔍 VALIDATION CHECKLIST

Ensure:

✅ No business logic in T4A  
✅ No T2E direct calls  
✅ No DB access  
✅ Dispatcher used everywhere  
✅ Middleware active  
✅ Contracts clean  
✅ Build succeeds  

---

# 📦 OUTPUT REQUIRED

1. Full folder structure
2. Controllers implemented
3. Application layer implemented
4. Gateway + middleware implemented
5. DTO + mapping layer
6. Example endpoints
7. Build success

---

# 🔒 FINAL PRINCIPLE

T4A = CONTROLLED ACCESS

It exposes the system  
but NEVER becomes the system.

---

Proceed with full implementation.