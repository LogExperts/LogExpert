using LogExpert;

using NUnit.Framework;

using System;
using System.Text;

namespace ColumnizerLib.UnitTests;

[TestFixture]
public class ColumnTests
{
    [Test]
    public void Column_LineCutOff ()
    {
        var expectedFullValue = new StringBuilder().Append('6', 4675).Append("1234").ToString();
        var expectedDisplayValue = expectedFullValue[..4675] + "..."; // Using substring shorthand

        Column column = new()
        {
            FullValue = expectedFullValue
        };

        Assert.That(column.DisplayValue, Is.EqualTo(expectedDisplayValue));
        Assert.That(column.FullValue, Is.EqualTo(expectedFullValue));
    }

    [Test]
    public void Column_NoLineCutOff ()
    {
        var expected = new StringBuilder().Append('6', 4675).ToString();
        Column column = new()
        {
            FullValue = expected
        };

        Assert.That(column.DisplayValue, Is.EqualTo(column.FullValue));
    }

    [Test]
    public void Column_NullCharReplacement()
    {
        Column column = new();

        column.FullValue = "asdf\0";

        //Switch between the different implementation for the windows versions
        //Not that great solution but currently I'm out of ideas, I know that currently 
        //only one implementation depending on the windows version is executed
        if (Environment.Version >= Version.Parse("6.2"))
        {
            Assert.That(column.DisplayValue, Is.EqualTo("asdf␀"));
        }
        else
        {
            Assert.That(column.DisplayValue, Is.EqualTo("asdf "));
        }

        Assert.That(column.FullValue, Is.EqualTo("asdf\0"));
    }

    [Test]
    public void Column_TabReplacement()
    {
        Column column = new();

        column.FullValue = "asdf\t";

        Assert.That(column.DisplayValue, Is.EqualTo("asdf  "));
        Assert.That(column.FullValue, Is.EqualTo("asdf\t"));
    }
}