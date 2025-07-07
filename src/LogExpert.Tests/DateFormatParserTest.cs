using LogExpert.Core.Classes.DateTimeParser;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LogExpert.Tests;

[TestFixture]
public class DateFormatParserTest
{
    [Test]
    public void CanParseAllCultures()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

        // HashSet<string> exclude = ["dz", "ckb-IR", "ar-SA", "lrc" , "lrc-IR", "mzn" , "mzn-IR", "ps"];

        foreach (var culture in cultures)
        {
            if (culture.Name == "dz" || culture.Name == "ar" || culture.Name.StartsWith("ar-") || culture.Name.StartsWith("dz-"))
            {
                Console.WriteLine($"The ${culture.Name} (${culture.DisplayName}) time format is not supported yet.");
                continue;
            }

            var datePattern = GetDateAndTimeFormat(culture);

            if (datePattern.StartsWith('g'))
            {
                Console.WriteLine("time format that starts with g is not supported yet.");
                continue;
            }

            var message = $"Culture: {culture.Name} ({culture.EnglishName} {datePattern})";
            var sections = Parser.ParseSections(datePattern, out var syntaxError);

            Assert.That(syntaxError, Is.False, message);

            var dateSection = sections.FirstOrDefault();
            Assert.That(dateSection, Is.Not.Null, message);

            var now = DateTime.Now;
            var expectedFormattedDate = now.ToString(datePattern);
            var actualFormattedDate = now.ToString(string.Join("", dateSection.GeneralTextDateDurationParts));
            Assert.That(actualFormattedDate, Is.EqualTo(expectedFormattedDate), message);
        }
    }

    [Test]
    [TestCase("en-US", "MM", "dd", "yyyy", "hh", "mm", "ss", "tt")]
    [TestCase("fr-FR", "dd", "MM", "yyyy", "HH", "mm", "ss")]
    [TestCase("de-DE", "dd", "MM", "yyyy", "HH", "mm", "ss")]
    [TestCase("ar-TN", "dd", "MM", "yyyy", "hh", "mm", "ss", "tt")]
    [TestCase("as", "dd", "MM", "yyyy", "tt", "hh", "mm", "ss")]
    [TestCase("bg", "dd", "MM", "yyyy", "HH", "mm", "ss")]
    public void TestDateFormatParserFromCulture(string cultureInfoName, params string[] expectedDateParts)
    {
        var culture = CultureInfo.GetCultureInfo(cultureInfoName);

        var datePattern = GetDateAndTimeFormat(culture);

        var sections = Parser.ParseSections(datePattern, out var syntaxError);

        var message = $"Culture: {culture.EnglishName}, Actual date pattern: {datePattern}";

        Assert.That(syntaxError, Is.False, message);

        var dateSection = sections.FirstOrDefault();
        Assert.That(dateSection, Is.Not.Null);

        var dateParts = dateSection
            .GeneralTextDateDurationParts
            .Where(Token.IsDatePart)
            .Select(p => DateFormatPartAdjuster.AdjustDateTimeFormatPart(p))
            .ToArray();

        Assert.That(dateParts.Length, Is.EqualTo(expectedDateParts.Length), message);

        for (var i = 0; i < expectedDateParts.Length; i++)
        {
            var expected = expectedDateParts[i];
            var actual = dateParts[i];
            Assert.That(actual, Is.EqualTo(expected), message);
        }
    }

    static string RemoveCharacters(string input, string charsToRemove)
    {
        HashSet<char> charsToRemoveSet = new(charsToRemove);
        StringBuilder result = new();

        foreach (var c in input)
        {
            if (!charsToRemoveSet.Contains(c))
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    private string GetDateAndTimeFormat(CultureInfo culture)
    {

        var InvisibleUNICODEmarkers =
                  "\u00AD\u034F\u061C\u115F\u1160\u17B4\u17B5" +
                            "\u180B\u180C\u180D\u180E\u200B\u200C\u200D\u200E\u200F" +
                            "\u202A\u202B\u202C\u202D\u202E\u202F\u205F\u2060\u2062" +
                            "\u2063\u2064\u2066\u2067\u2068\u2069\u2800\u3164\uFE00" +
                            "\uFE01\uFE02\uFE03\uFE04\uFE05\uFE06\uFE07\uFE08\uFE09" +
                            "\uFE0A\uFE0B\uFE0C\uFE0D\uFE0E\uFE0F";


        var dateTime = string.Concat(culture.DateTimeFormat.ShortDatePattern.ToString(),
            " ",
            culture.DateTimeFormat.LongTimePattern.ToString());

        return RemoveCharacters(dateTime, InvisibleUNICODEmarkers);

    }
}
