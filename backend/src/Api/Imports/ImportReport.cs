using System.Collections;

namespace Api.Imports;

public sealed class ImportReport : IEnumerable<ImportRowResult>
{
    private readonly List<ImportRowResult> _results = [];

    public int TotalCount => _results.Count;

    public int CreatedCount => _results.Count(r => r.Outcome == ImportOutcome.Created);

    public int FailedCount => _results.Count(r => r.Outcome == ImportOutcome.Failed);

    public void Add(ImportRowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _results.Add(result);
    }

    public IEnumerator<ImportRowResult> GetEnumerator() => _results.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
