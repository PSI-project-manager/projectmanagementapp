architecture decision records here

## ADR-0001: Sprint ordering conflict between `sprints.md` and `plan-overview.md`

Status: proposed — needs team confirmation, not yet a final decision.

### Context

`docs/sprints.md` schedules US-05 (Create and Manage Projects) in Sprint 1 as an
"independent" story. Its own acceptance criteria, however — "editing a project makes the
changes visible to those with access" and "a contributor sees only the projects they are
authorized for" — require a permission model that doesn't exist until later: roles
(US-04, Sprint 2 in `sprints.md`) and project-level access (US-06, Sprint 3 in
`sprints.md`). The same conflict applies to US-03 (manage users), whose "non-admin can't
access admin screens" criterion also needs roles.

`docs/plan-overview.md` was written to address this: it reorders the backlog around
organizations (auth → orgs/membership → hardcoded roles+statuses → org user management →
project CRUD → ...), so that by the time project CRUD is implemented, an org, a role
model, and a `can()` permission function already exist. In that plan, a project belongs
to exactly one organization, and (v1) every org member can see every project in that org
— per-project access is deferred to a later "manage project access" story.

### Decision needed

The two docs haven't been reconciled, and no ADR previously recorded which one is
authoritative. This entry exists to surface the conflict so the team makes an explicit
call instead of two contradicting plans staying in the repo.

### Consequence for this change

Given no `Organization` entity exists yet in the codebase, the `Project` domain entity
added alongside this ADR takes a middle path compatible with either plan: it stores an
`OrganizationId` (a plain `Guid`, not a reference to an `Organization` type) so it doesn't
need to depend on code owned by the auth/orgs story, while still being ready to slot into
the org-scoped model `plan-overview.md` describes. `ListProjectsHandler` currently
returns every project in an organization without per-user filtering, since the
`can()` function and project-access model that "contributor sees only authorized
projects" depends on don't exist yet — that's blocked on other stories, not an oversight.
