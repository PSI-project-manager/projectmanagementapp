## issues with current sprint plans that this aims to fix

- existing plan schedules admin CRUD before the permission model those stories depend on. US-03
  (manage users) has an acceptance criteria "non-admin can't access admin screens" - that needs
  roles (US-04, Sprint 2). US-05 (manage projects) has "contributor sees only authorized projects" -
  that needs project access (US-06, Sprint 3) and roles. So Sprint 1 literally can't be built as
  written; you'd be coding access rules against a permission system that doesn't exist yet
- existing plan is only 5 sprints and our sprints are meant to be a week long. new plan aims to fit
  in 3 months (i kept extra time because as we develop we will understand things about the app we
  dont understand now and our plans will change, so we will need the left over time to implement
  those)
- US-8 should be added to the list of things to do after the core app is finished, if added at all
  because in my opinion it isnt an important feature since statuses can be defined by just 3 stages:
  unassigned, assigned/in progress, completed
- regarding US-11 i dont think its necessary for a contributor to edit their own tasks, i think the
  standard way to manage a team is to have 1 manager which plans and assigns everything instead of
  everyone making tasks for themselves. the only thing contributors should be able to do is to mark
  their status as completed (can change this in the end if we add dynamic role management)
- US-16, US-17, US-18 suggest that this sprint plan was copied from a backend project with little
  frontend interface, but because we are building a fullstack app from the beginning REST is just a
  way to communicate with the backend and is needed on a per-feature basis not just as some final
  thing we do.

## rough features in order of completion

# 1

- auth (register/login/logout)
- as a user i want to be able to create and delete organizations, if i create one i automatically
  become the admin.
- as a user i want to be able to belong to more than one organization and switch which one im
  currently looking at. this is why roles are per-org and why basically every query from here on has
  to be scoped by org.
- hardcode roles and their permissions, hardcode task statuses

# 2

- as an admin i want to have a place where i can manage all the users inside my org (invite users to
  org, assign/remove roles, kick)
- as an admin i want to create, update and delete projects inside an organization
- as an admin i want to have a place where i can define task categories

# 3

- as an admin i want to be able to create/update/delete tasks and assign contributors

# 4

note: all users added to an organization see all the projects of that organization, can change later

- as anyone inside a project i want to be able to view the task list

# 5

- as a contributor i want to be able to mark tasks as complete

# 6

- as anyone inside a project i want to be able to view a task from the task list in detail
- as anyone inside a project i want to be able to filter tasks by category, status, assignee
- as anyone inside a project i want to be able to search tasks

# 7

- as an admin i want to have a place where i can see logs of everything that happened previously in
  the project. who marked their task as done at what time, what tasks were created, edited, removed
  and by who, these logs shouldnt be editable for transparency between admins managing the org.

## after core is finished

- (optional) as an admin i want to have a place where i can define roles and what permissions each
  role has and which projects the role has access to (like discord)
