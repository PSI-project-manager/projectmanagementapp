# Deadline: week 7, max points: 1.5

Status as of 2026-10-04.

Goals:

- [x] Develop a web application using the material covered in 1-6 lectures.

- [x] Develop an app in ASP.NET that has basic functionality.

- [x] Understand the usage of GitHub, coding in team principles.

- [x] Prepare foundation for future work.

- [x] Learn to code review yourself and take the feedback when getting one.

Formal Requirements:

- [x] Application can be interacted with using some sort of interface. There exists at least one user scenario, which can be demonstrated end to end.

- [x] Creating and using your own class, struct, record and enum. 1 type must be immutable.
  - `ImportReport` (class), `CsvLine` (readonly struct), `ImportRowResult` (record), `ImportOutcome` (enum) in `backend/src/Api/Imports/`. `CsvLine` and `ImportRowResult` are immutable.

- [x] Property usage in struct and class.
  - `CsvLine.LineNumber`, `CsvLine.Text`, `CsvLine.IsBlank`; `ImportReport.TotalCount`, `CreatedCount`, `FailedCount`.

- [x] Named and optional argument usage.

- [x] Extension method usage.
  - `Stream.ReadCsvLinesAsync` (`CsvStreamExtensions`), `ClaimsPrincipal.GetUserId` (`ClaimsPrincipalExtensions`).

- [x] Iterating through collections the right way.
  - `await foreach` in `ItemImportService.ImportAsync`, `foreach` in `ImportReportDto.FromReport`.

- [x] Using a stream to load data (can be from file, web service, socket etc.).
  - The CSV request body is read line by line from a `Stream` (`ReadCsvLinesAsync`).

- [x] LINQ to Objects used where appropriate (methods or queries). If LINQ is not used in a particular scenario, provide a justification.

- [x] Implement at least one of the standard .NET interfaces (IEnumerable, IComparable, IComparer, IEquatable, IEnumerator, etc.)
  - `ImportReport : IEnumerable<ImportRowResult>`, `CsvLine : IEquatable<CsvLine>`.

- [x] All changes reviewed via pull requests; each PR must have description explaining what was done and why. Each team member must have authored at least 3 merged PRs and reviewed at least 3 PRs from teammates. PR is counted as reviewed only if there are any meaningful comments and discussions.

- [x] Uniform coding style is used throughout the project.

- [x] Define 10+ user stories and acceptance criteria for the product. Have at least 3 of them implemented