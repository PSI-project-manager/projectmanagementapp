using Api.Imports;
using Xunit;

namespace Api.Tests.Imports;

public class CsvLineTests
{
    [Fact]
    public void Constructor_StoresLineNumberAndText()
    {
        var line = new CsvLine(7, "a,b,c");

        Assert.Equal(7, line.LineNumber);
        Assert.Equal("a,b,c", line.Text);
    }

    [Fact]
    public void Constructor_NullText_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CsvLine(1, null!));

        Assert.Equal("text", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void IsBlank_EmptyOrWhitespaceText_IsTrue(string text)
    {
        Assert.True(new CsvLine(1, text).IsBlank);
    }

    [Theory]
    [InlineData("a")]
    [InlineData(" a ")]
    [InlineData("title,1,1,description")]
    public void IsBlank_TextWithContent_IsFalse(string text)
    {
        Assert.False(new CsvLine(1, text).IsBlank);
    }

    [Fact]
    public void Equals_SameLineNumberAndText_AreEqual()
    {
        var first = new CsvLine(3, "abc");
        var second = new CsvLine(3, "abc");

        Assert.True(first.Equals(second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentLineNumber_AreNotEqual()
    {
        var first = new CsvLine(3, "abc");
        var second = new CsvLine(4, "abc");

        Assert.False(first.Equals(second));
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_DifferentText_AreNotEqual()
    {
        var first = new CsvLine(3, "abc");
        var second = new CsvLine(3, "abd");

        Assert.False(first.Equals(second));
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_Object_OnlyEqualToBoxedCsvLineWithSameValues()
    {
        var line = new CsvLine(3, "abc");

        Assert.True(line.Equals((object)new CsvLine(3, "abc")));
        Assert.False(line.Equals((object)new CsvLine(4, "abc")));
        Assert.False(line.Equals("3: abc"));
        Assert.False(line.Equals(null));
    }

    [Fact]
    public void GetHashCode_EqualLines_WorkAsHashSetKeys()
    {
        var set = new HashSet<CsvLine> { new(1, "a"), new(2, "b") };

        Assert.Contains(new CsvLine(1, "a"), set);
        Assert.DoesNotContain(new CsvLine(1, "b"), set);
    }

    [Fact]
    public void ToString_ShowsLineNumberAndText()
    {
        Assert.Equal("3: abc", new CsvLine(3, "abc").ToString());
    }
}
