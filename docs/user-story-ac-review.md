# User Story AC Review — 2026-10-21

## Summary

| Story | AC met | Main gaps |
| --- | --- | --- |
| US-01 User Login | +- | Expired session mid-use is not redirected to login |
| US-03 Create and Manage Users | +- | Deactivated users existing token stays valid until expiry |
| US-04 Assign Roles to Users | + | — |
| US-05 Create and Manage Projects | +- | Items can be created in projects the user has no access to |
| US-07 Define Item Types | +- | items can't be edited yet |
| US-09 Create Item | - | No item list in the UI; status is a raw number input |

---

## Issues in detail

### US-01 — User Login

The token lasts 8h (`AuthService.TokenLifetime`) and is kept in `localStorage`, so it survives reloads. **But:** the expiry check only runs when a route renders. We should add a check while the page is open.

---

### US-03 — Create and Manage Users

A deactivated user can't log in again, but a token they already have stays valid for up to 8h, because no endpoint checks `Isactive` per request. We should reject requests from deactivated users.

### US-05 — Create and Manage Projects

`POST /api/items` checks that the project exists but not that the caller can access it, so a contributor can create items in any project through the API. Viewing items and CSV import already call `CanAccessAsync`, so create should do the same.

### US-07 — Define Item Types

"Existing items keep their type unless it is explicitly changed" can't be checked yet, because items can't be edited until US-11. `EnsureAssignableAsync` already handles the unchanged-type case, so it should work once editing exists.

### US-09 — Create Item

- There's no item list in the UI. `GET /api/items?projectId=` exists on the backend, but no page uses it.
- The form has no validation of its own. The backend blocks missing fields, but the user sees one combined error message and empty dropdowns are sent as `0`.
- Status is a raw number input until US-08 (statuses) is done.
