Perform a Domain Boundary Audit for the Whycespace repository.

The project uses DDD architecture under WBSM v3.

Scan:

src/domain/

Detect the following violations:

1. Infrastructure dependencies
2. Engine dependencies
3. Runtime dependencies
4. System dependencies
5. Persistence code
6. HTTP or messaging logic

Report:

SECTION 1 — Domain Folder Structure  
SECTION 2 — Dependency Violations  
SECTION 3 — Infrastructure Leaks  
SECTION 4 — Aggregate Boundary Analysis  
SECTION 5 — Domain Event Review  
SECTION 6 — Recommended Refactoring  
SECTION 7 — Domain Architecture Score