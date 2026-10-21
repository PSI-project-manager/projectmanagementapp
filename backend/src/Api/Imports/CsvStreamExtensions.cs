using System.Runtime.CompilerServices;
using System.Text;

namespace Api.Imports;

public static class CsvStreamExtensions
{
    public static async IAsyncEnumerable<CsvLine> ReadCsvLinesAsync(
        this Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true
        );

        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } text)
        {
            lineNumber++;
            yield return new CsvLine(lineNumber, text);
        }
    }
}
