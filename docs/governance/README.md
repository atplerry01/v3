# Whycespace Governance

## Upstream Systems

Governance is enforced by three upstream systems located in `src/system/upstream/`:

### WhycePolicy

Policy-based governance engine. Evaluates rules against execution context to enforce constitutional constraints before workflows proceed.

- **PolicyGovernor** — registers and evaluates policy rules
- Policy severities: `Info`, `Warning`, `Critical`

### WhyceChain

Immutable audit ledger providing cryptographic verification of system actions.

- **ChainLedger** — append-only chain with SHA-256 hash linking
- Supports chain integrity verification via `Verify()`

### WhyceID

Identity management for all system participants.

- **IdentityProvider** — registers and resolves `WhyceIdentity` records
- Identities carry roles and claims for authorization decisions

## Constitutional Engines (T0U)

| Engine                      | Purpose                          |
|-----------------------------|----------------------------------|
| PolicyValidationEngine      | Validates policy constraints     |
| ChainVerificationEngine     | Verifies chain integrity         |
| IdentityVerificationEngine  | Verifies user identity           |

## Policy Middleware

The platform API gateway includes `PolicyMiddleware` which integrates with the `PolicyGovernor` to enforce governance at the HTTP boundary.
