## plan

because features depend on each other, especially at the start, there will be times where developers
will be "blocked" and wont be able to implement new features because their feature depends on
something another dev is working on. that time should be used to write tests, docs, or try to
improve existing code.

each feature could be owned by a single developer end to end (both frontend and backend) but for
example when we have only 2 features in a sprint it's more sensible to split the feature between 2
people for frontend and backend. the 2 devs should agree on a API contract, document it, and both
work independently.

because the whole point of this course is backend, the exam will be on backend, we should all
alternate frontend and backend each week so that there's no one that does frontend and misses out on
the point of the course.

the semester is 4 months long but this plan is only 3 months to leave room for adjustments later.

the project isnt that big so the sprints are really underfilled for most weeks, we can either
condense it and get it done faster and then try to add more features or just keep it and accept that
some weeks some people wont be working.

i only planned week 1 and 2 properly, ill plan 1 week forward at a time because as we develop we
will understand things about the app we're building that we dont understand now and our tasks may
change.

## Definition of Done (every story)

- works full stack
- request dtos validated by FluentValidation and reject invalid requests with proper error message,
  no stack trace
- includes happy path tests, preferably more
- from sprint 2 on: access is enforced through the single `can()` function, never with permission
  checks written inline in the feature. sprint 1 is exempt because `can()` doesn't exist until
  sprint 2 and auth is authentication, not authorization

## fixed values (can add dynamic management later after core app is finished)

- user roles: `admin`, `contributor`
- task status: `unassigned` / `in progress` / `completed`

## sprint 1 auth

plan for week 1:

- `db/schema.sql` is written in T-SQL and doesnt match current plan, so we can either use a
  code-first approach or rewrite `schema.sql` to postgres and to match current plan, i would
  honestly suggest a code first approach because it will be easier to do migrations

- pijus still needs to containerize db

- azuolas still needs to do his tasks from readme

- someone needs to create a project for backend tests

- 1 dev does auth backend

- 1 dev does auth frontend

### auth - register / login / logout

- register: a new user registers with valid credentials; a duplicate email/username is rejected with
  a clear error
- login: correct credentials, authenticated and redirected to the dashboard
- login: wrong credentials, access denied, error shown, no session created
- logout: redirects to login and invalidates the session/token
- an unauthenticated user opening a protected page is redirected to login

## sprint 2 orgs + possible user roles and task statuses

- 1 dev does create/delete organization backend
- 1 dev does create/delete organization frontend
- 1 dev does hardcode user roles and task statuses

### create / delete organization

- a logged-in user creates an org with a name and becomes its admin
- the creator can delete an org they admin (projects/tasks that belong to that org get deleted too,
  with confirmation (could do smth like github when deleting repo))
- a non-member can't see or access the org
- a user can belong to multiple orgs and switch the active one

### hardcode user roles and task statuses

- possible roles: admin, contributor. they only make sense within an org so each app user must have
  a per-org profile.
- possible task statuses : unassigned / in progress / completed
- a single `can(user, action, resource)` function instead of putting permission logic checks inside
  each feature, each feature should just call this.

# APPROXIMATIONS, STILL TODO

## sprint 3 - org user management

### manage users in the org (admin)

- admin invites a user via link/code; the invitee joins as contributor by default
- admin changes a member's role; the new permissions apply on their next action/session
- admin removes (kicks) a member; that user immediately loses all access to the org
- a contributor cannot open the user-management screen

## sprint 4 - project crud + categories

goal: the structures tasks live inside.

### create / update / delete projects (admin)

- admin creates a project with required fields; it appears in the org's project list
- admin updates a project; changes are visible to members
- admin deletes a project (cascades to its tasks, with confirmation)
- a contributor cannot create/update/delete projects

### define task categories (admin)

- admin creates, renames, and removes categories in the org
- a category becomes selectable when creating/editing a task
- removing a category that's in use is blocked until those tasks are recategorized

## sprint 5 - task create + assign

goal: get tasks into the system.

### create + assign a task (admin)

- admin creates a task with title, description, category, status, assignee (project is inferred by
  app), only required fields are title and description.
- missing required fields block the save and show validation
- a contributor cannot create tasks

## sprint 6 - task update / delete / reassign

goal: full task lifecycle in admin hands.

### update / delete / reassign (admin)

- admin updates any task's fields
- admin reassigns a task to a different org member
- admin deletes a task (with confirmation)
- a contributor cannot update, delete, or reassign tasks

## sprint 7 - view task list

goal: the main screen everything else hangs off. rule (v1): every org member can view every project
in that org.

### view the task list

- any org member opens a project and sees its task list
- the list shows title, category, status, and assignee per task
- the list reflects creates/updates/deletes after refetch
- a non-member of the org cannot see the list

## sprint 8 - mark task complete

goal: the one action a contributor owns.

### mark assigned task complete (contributor)

- a contributor marks a task assigned to them as completed
- a contributor cannot change the status of a task not assigned to them
- the status change persists and shows in the list and detail view
- admin can also set any task's status

## sprint 9 - task detail

goal: see the full picture before acting.

### view task detail

- opening a task shows full details: title, description, project, category, status, assignee,
  created/updated timestamps
- a non-member of the org cannot open the detail view

## sprint 10 - filter tasks

goal: make the list usable at scale.

### filter tasks

- filtering by category / status / assignee shows only matching tasks
- combining filters narrows correctly
- clearing the filter restores the full list
- filters never expose tasks outside the user's org

## sprint 11 - search tasks

goal: find a task directly.

### search tasks

- a keyword matches on title and description
- no matches shows a "no results" message
- search never returns tasks outside the user's org

## sprint 12 - audit log

goal: an immutable, admin-visible history - the final mvp feature. this is added last on purpose: it
means going back through the mutations built in earlier sprints (project/task/user create, update,
delete, status change) and instrumenting each to record an event, then building the viewing screen.
expect to touch files across earlier features.

### view audit log (admin)

- every create/update/delete and status change records an immutable entry: actor, action, target,
  timestamp
- admin views the log for the org/project, ordered newest-first
- entries are read-only - no edit or delete path in the ui or the api
- a contributor cannot view the audit log

## after core is finished (stretch backlog)

- custom roles/permissions - admin defines roles, their permissions, and per-role project access
  (discord-like). slots in behind the existing can() function without a model rewrite.
