using System.Collections;
using System.Collections.Concurrent;

namespace Api.Imports;

public sealed class ImportReport : IEnumerable<ImportRowResult>
{
    private readonly ConcurrentQueue<ImportRowResult> _results = [];

    public int TotalCount => _results.Count;

    public int CreatedCount => _results.Count(r => r.Outcome == ImportOutcome.Created);

    public int FailedCount => _results.Count(r => r.Outcome == ImportOutcome.Failed);

    public void Add(ImportRowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _results.Enqueue(result);
    }

    public IEnumerator<ImportRowResult> GetEnumerator() =>
        _results.OrderBy(r => r.LineNumber).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
