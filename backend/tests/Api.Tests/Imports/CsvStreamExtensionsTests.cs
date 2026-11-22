using System.Text;
using Api.Imports;
using Xunit;

namespace Api.Tests.Imports;

public class CsvStreamExtensionsTests
{
    private static MemoryStream StreamOf(string text) => new(Encoding.UTF8.GetBytes(text));

    private static async Task<List<CsvLine>> ReadAllAsync(
        Stream stream,
        CancellationToken ct = default
    )
    {
        var lines = new List<CsvLine>();
        await foreach (var line in stream.ReadCsvLinesAsync(ct))
        {
            lines.Add(line);
        }
        return lines;
    }

    [Fact]
    public async Task ReadCsvLinesAsync_NumbersLinesStartingFromOne()
    {
        using var stream = StreamOf("a,b\nc,d\ne,f");

        var lines = await ReadAllAsync(stream);

        Assert.Equal(
            new[] { new CsvLine(1, "a,b"), new CsvLine(2, "c,d"), new CsvLine(3, "e,f") },
            lines
        );
    }

    [Fact]
    public async Task ReadCsvLinesAsync_EmptyStream_ReturnsNoLines()
    {
        using var stream = StreamOf("");

        var lines = await ReadAllAsync(stream);

        Assert.Empty(lines);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public async Task ReadCsvLinesAsync_SupportsAllLineEndings(string newline)
    {
        using var stream = StreamOf($"one{newline}two{newline}three");

        var lines = await ReadAllAsync(stream);

        Assert.Equal(new[] { "one", "two", "three" }, lines.Select(l => l.Text));
        Assert.Equal(new[] { 1, 2, 3 }, lines.Select(l => l.LineNumber));
    }

    [Fact]
    public async Task ReadCsvLinesAsync_TrailingNewline_DoesNotAddExtraLine()
    {
        using var stream = StreamOf("a\nb\n");

        var lines = await ReadAllAsync(stream);

        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public async Task ReadCsvLinesAsync_BlankLinesInTheMiddle_AreReturnedAndCounted()
    {
        using var stream = StreamOf("a\n\n  \nb");

        var lines = await ReadAllAsync(stream);

        Assert.Equal(4, lines.Count);
        Assert.True(lines[1].IsBlank);
        Assert.True(lines[2].IsBlank);
        Assert.Equal(new CsvLine(4, "b"), lines[3]);
    }

    [Fact]
    public async Task ReadCsvLinesAsync_Utf8Bom_IsNotPartOfTheFirstLine()
    {
        var bytes = Encoding
            .UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("title,itemTypeId\nrow"))
            .ToArray();
        using var stream = new MemoryStream(bytes);

        var lines = await ReadAllAsync(stream);

        Assert.Equal("title,itemTypeId", lines[0].Text);
    }

    [Fact]
    public async Task ReadCsvLinesAsync_Utf16WithBom_IsDecodedCorrectly()
    {
        var bytes = Encoding
            .Unicode.GetPreamble()
            .Concat(Encoding.Unicode.GetBytes("pirma,eilutė\nantra"))
            .ToArray();
        using var stream = new MemoryStream(bytes);

        var lines = await ReadAllAsync(stream);

        Assert.Equal(new[] { "pirma,eilutė", "antra" }, lines.Select(l => l.Text));
    }

    [Fact]
    public async Task ReadCsvLinesAsync_NonAsciiUtf8_IsPreserved()
    {
        using var stream = StreamOf("Ошибка входа,1,1,Не работает логин\nŽąsis,1,1,ąčęė");

        var lines = await ReadAllAsync(stream);

        Assert.Equal("Ошибка входа,1,1,Не работает логин", lines[0].Text);
        Assert.Equal("Žąsis,1,1,ąčęė", lines[1].Text);
    }

    [Fact]
    public async Task ReadCsvLinesAsync_LeavesTheStreamOpen()
    {
        using var stream = StreamOf("a\nb");

        await ReadAllAsync(stream);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task ReadCsvLinesAsync_DoesNotReadTheWholeStreamUpFront()
    {
        var text = string.Join("\n", Enumerable.Range(1, 20_000).Select(n => $"line {n}"));
        using var stream = StreamOf(text);

        await foreach (var line in stream.ReadCsvLinesAsync())
        {
            Assert.Equal(1, line.LineNumber);
            break;
        }

        // only the first buffer was consumed, not the ~200 KB file
        Assert.True(stream.Position < stream.Length / 10);
    }

    [Fact]
    public async Task ReadCsvLinesAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var stream = StreamOf("a\nb");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ReadAllAsync(stream, cts.Token)
        );
    }
}
