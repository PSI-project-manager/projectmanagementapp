namespace Api.Imports;

public readonly struct CsvLine : IEquatable<CsvLine>
{
    public CsvLine(int lineNumber, string text)
    {
        LineNumber = lineNumber;
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public int LineNumber { get; }

    public string Text { get; }

    public bool IsBlank => string.IsNullOrWhiteSpace(Text);

    public bool Equals(CsvLine other) => LineNumber == other.LineNumber && Text == other.Text;

    public override bool Equals(object? obj) => obj is CsvLine other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(LineNumber, Text);

    public override string ToString() => $"{LineNumber}: {Text}";

    public static bool operator ==(CsvLine left, CsvLine right) => left.Equals(right);

    public static bool operator !=(CsvLine left, CsvLine right) => !left.Equals(right);
}
