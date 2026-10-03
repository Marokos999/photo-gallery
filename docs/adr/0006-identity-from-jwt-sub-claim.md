# 0006 — Identity from the API Gateway JWT `sub` claim

**Status:** Accepted

## Context

Every item is owned by a user, so every request needs a trusted user id. Validating tokens inside each
function duplicates work and configuration. Locally there is no identity provider, but the app must still be usable.

## Decision

- **API Gateway's JWT authorizer** validates Cognito tokens (issuer and audience) before any Lambda runs; the
  functions read only the `sub` claim it forwards. Public routes (`GET /api/shared/{code}`, `GET /api/health`)
  opt out with `Authorizer: NONE`.
- One rule in Core (`UserIdentity.Resolve`), used by both the upload Lambda and the Photos API: the JWT `sub`
  wins; the `x-debug-user-id` header is honoured **only** when the service runs against LocalStack.
- Ownership is enforced by key design: every query is scoped to `PK = USER#{sub}`, so one user cannot address
  another user's items even with a guessed id.

## Consequences

- No token validation code or JWKS caching in the functions.
- The debug header is inert in AWS: without `LOCALSTACK_ENDPOINT` it is ignored.
- The frontend does not include a sign-in flow yet; a real deployment needs the Cognito hosted UI or Amplify
  Auth to obtain tokens and send `Authorization: Bearer …`.
