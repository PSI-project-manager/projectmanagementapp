# Backlog Structure — Issue Tracker Project

## Sprint Plan

| Sprint | User Stories | Note |
|---|---|---|
| Sprint 1 | US-01, US-03, US-05, US-07 | Independent base stories — no cross-dependencies within the sprint |
| Sprint 2 | US-08, US-02, US-04, US-09 | US-09 depends on US-08 (same sprint) — close US-08 early in the sprint before starting US-09 |
| Sprint 3 | US-06, US-10, US-11, US-12 | All depend only on earlier sprints |
| Sprint 4 | US-16, US-17, US-13, US-14 | All depend only on earlier sprints |
| Sprint 5 | US-15, US-18 | All depend only on earlier sprints |

**Dependency note:** with 5 independent base stories (US-01, 03, 05, 07, 08) and 4 slots per sprint, at least one dependency pair cannot be fully separated. The only unavoidable overlap here is US-08 → US-09 in Sprint 2; every other dependency pair (e.g. US-03 → US-04, US-01 → US-02) is split across separate sprints.

---

## Sprint 1

### US-01 — User Login
**As a** registered user, **I want to** log in with my credentials, **so that** I can securely access the application according to my role.

Acceptance criteria:
- [ ] With correct credentials, the user is authenticated and redirected to the home screen
- [ ] With incorrect credentials, access is denied and an error message is shown
- [ ] An unauthenticated user trying to open a protected page is redirected to login
- [ ] The session remains active until logout or expiration

### US-03 — Create and Manage Users
**As an** admin, **I want to** create, edit, activate, and deactivate user accounts, **so that** I can control access to the system.

Acceptance criteria:
- [ ] With valid data, a new user account is saved successfully
- [ ] Editing an existing user updates the information in the list
- [ ] A deactivated user cannot log in
- [ ] Non-admin users cannot access user administration screens

### US-05 — Create and Manage Projects
**As an** admin, **I want to** create and maintain projects, **so that** items can be organized by work area.

Acceptance criteria:
- [ ] Creating a project with required fields adds it to the project list
- [ ] Editing a project makes the changes visible to those with access
- [ ] A contributor sees only the projects they are authorized for
- [ ] An item cannot exist without a project

### US-07 — Define Item Types
**As an** admin, **I want to** configure item types, **so that** items are categorized consistently.

Acceptance criteria:
- [ ] A new type becomes selectable when creating/editing an item
- [ ] An inactive type cannot be selected for new items
- [ ] Existing items keep their type unless it is explicitly changed

---

## Sprint 2

### US-08 — Define Statuses
**As an** admin, **I want to** configure statuses, **so that** items move through a controlled lifecycle.

Acceptance criteria:
- [ ] A new status becomes available for assignment
- [ ] A contributor can change an item's status and it is saved
- [ ] An inactive status cannot be assigned to new items

### US-02 — User Logout
**As a** logged-in user, **I want to** log out, **so that** my session is closed.

Acceptance criteria:
- [ ] On logout, the session/token is invalidated
- [ ] After logout, accessing a protected page requires re-authentication

### US-04 — Assign Roles to Users
**As an** admin, **I want to** assign roles to users, **so that** access to functionality is controlled.

Acceptance criteria:
- [ ] Assigning a role grants the user the corresponding permissions
- [ ] A contributor cannot access an admin-only page
- [ ] After a role change, the new permissions apply on the next session

### US-09 — Create Issue / Item
*Depends on US-08 — close US-08 first, then start this story.*

**As a** contributor, **I want to** create an item in a project, **so that** work can be tracked and managed.

Acceptance criteria:
- [ ] With required fields filled in (title, description, project, type, status), the item is created
- [ ] With missing required fields, saving is blocked and validation is shown
- [ ] The new item appears in the project's item list

---

## Sprint 3

### US-06 — Manage Project Access
**As an** admin, **I want to** assign users to projects, **so that** only authorized users can view and manage items in those projects.

Acceptance criteria:
- [ ] Assigning a user to a project lets them see it after login
- [ ] Without access, a direct attempt to open the project is denied
- [ ] Removing a user from a project removes access to it and its items

### US-10 — View Item List
**As a** contributor, **I want to** view the item list in my projects, **so that** I can understand progress.

Acceptance criteria:
- [ ] With project access, items are visible
- [ ] Without access, items are not visible
- [ ] The list refreshes after create/edit

### US-11 — Edit Issue / Item
**As a** contributor, **I want to** edit an existing item, **so that** its information stays accurate.

Acceptance criteria:
- [ ] After updating fields and saving, the changes are persisted
- [ ] Required-field validation applies during edit
- [ ] Unauthorized users cannot edit items outside their access scope

### US-12 — View Item Details
**As a** contributor, **I want to** open an item's detail view, **so that** I can see the full information before editing.

Acceptance criteria:
- [ ] Opening an item shows full details (title, description, project, type, status)
- [ ] An unauthorized access attempt is blocked

---

## Sprint 4

### US-16 — Read Issues / Items via REST API
Acceptance criteria:
- [ ] An authorized request returns items in a structured format
- [ ] Without authorization, access is denied
- [ ] The API returns only allowed data

### US-17 — Create Issues / Items via REST API
Acceptance criteria:
- [ ] With a valid payload and authorization, a new item is created
- [ ] With an invalid payload, the API returns validation feedback
- [ ] Created items are visible in the UI

### US-13 — Filter Issues / Items
Acceptance criteria:
- [ ] Applying a filter (status/type/project) shows only matching items
- [ ] Clearing the filter shows the full accessible list again
- [ ] Filters do not expose unauthorized data

### US-14 — Search Issues / Items
Acceptance criteria:
- [ ] Entering a keyword shows matching items (title/description)
- [ ] With no matches, a "no results" message is shown
- [ ] Search results do not include unauthorized items

---

## Sprint 5

### US-15 — Board Visualization
Acceptance criteria:
- [ ] The board shows items grouped by status
- [ ] A status change is reflected on the board after refresh
- [ ] The board shows only accessible project data

### US-18 — Edit Issues / Items via REST API
Acceptance criteria:
- [ ] With a valid ID, payload, and authorization, the item is updated
- [ ] With an invalid ID/data, the API returns an error
- [ ] Updates are visible in subsequent API reads and the UI

*Extra (Sprint 4-5, if a 4th person has spare capacity): tests, bug fixes from Phase 1, deployment to Azure App Service, Swagger/OpenAPI documentation.*
