Need to add how we should work on the project, incl. project management on github, branching strategy in seperate file, etc.

# Development: branching strategy

IMPORTANT:

- Never rewrite history that has already been pushed to a shared branch

At the start of every sprint:

- Create a feature branch from main based on your user story
- Implement the feature
- Run the formatter to format the code `dotnet csharpier format .` from inside `backend/`
- Run the linter and fix all the code errors: run `dotnet format style --verify-no-changes` and
  `dotnet format analyzers --verify-no-changes` from inside `backend/`(github will reject the PR if
  you don't)
- Merge to main and resolve merge conflicts with teammates if any
