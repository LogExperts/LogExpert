using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Security;
using System.Text;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.JsonConverters;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;

using Newtonsoft.Json;

namespace LogExpert.Persister.Tests;

[TestFixture]
public class SessionFileComposerTests
{
    #region Fields

    private string _testDirectory;
    private string _sessionDirectory;
    private string _logFileName;

    /// <summary>
    /// Equality is serialize-&amp;-compare: both sides are serialized with deterministic settings
    /// (reusing the Columnizer/Encoding converters) and compared as strings. Deep by
    /// construction — a new field automatically joins the comparison.
    /// </summary>
    private static readonly JsonSerializerSettings _comparisonSettings = new()
    {
        Converters =
        {
            new ColumnizerJsonConverter(),
            new EncodingJsonConverter()
        },
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
    };

    /// <summary>
    /// The named exclusion list (spec, Testing Decisions): fields that legitimately do not
    /// round-trip. Each entry is justified in the spec's mapping table.
    /// </summary>
    private static readonly string[] _namedExclusions =
    [
        // Dead fields — written only by the legacy XML reader, applied nowhere:
        nameof(PersistenceData.BookmarkListPosition),
        nameof(PersistenceData.BookmarkListVisible),
        nameof(PersistenceData.ShowBookmarkCommentColumn),
        nameof(PersistenceData.ColumnizerName),
        nameof(PersistenceData.SettingsSaveLoadLocation),
        // Always null in practice — never assigned anywhere in the UI:
        nameof(PersistenceData.SessionFileName),
        // Transformed by Persister on SameDir saves after compose — documented, not asserted:
        nameof(PersistenceData.FileName),
    ];


    #endregion

    [SetUp]
    public void Setup ()
    {
        _testDirectory = Path.Join(Path.GetTempPath(), "LogExpertTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);
        _sessionDirectory = Path.Join(_testDirectory, "sessionFiles");

        _logFileName = Path.Join(_testDirectory, "test.log");
        File.WriteAllText(_logFileName, "Test log content");
    }

    [TearDown]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Test")]
    public void TearDown ()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Helpers and fixtures

    private static string SerializeForComparison (object value)
    {
        return JsonConvert.SerializeObject(value, _comparisonSettings);
    }

    /// <summary>
    /// Serialize-&amp;-compare with the excluded top-level fields removed from the JSON — used by
    /// the reverse trip, whose exclusions are named and justified, never implicit.
    /// </summary>
    private static string SerializeExcluding (object value, IReadOnlyCollection<string> excludedProperties)
    {
        var json = Newtonsoft.Json.Linq.JObject.Parse(SerializeForComparison(value));

        foreach (var propertyName in excludedProperties)
        {
            _ = json.Remove(propertyName);
        }

        return json.ToString();
    }

    /// <summary>
    /// Reflects over an instance's public properties and returns the names of those that could
    /// not prove a mapping exists: a value equal to a freshly-constructed baseline (a dropped
    /// mapping would leave the construction default standing and every trip would still pass),
    /// or a null/empty string or collection. The fixture-completeness tests use this so that
    /// adding a field without extending the fixtures fails immediately.
    /// </summary>
    private static List<string> GetUnpopulatedProperties (object instance, IReadOnlyCollection<string> excludedProperties)
    {
        var baseline = Activator.CreateInstance(instance.GetType());
        List<string> unpopulated = [];

        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (excludedProperties.Contains(property.Name))
            {
                continue;
            }

            var value = property.GetValue(instance);
            var isUnpopulated = value switch
            {
                null => true,
                string text => text.Length == 0,
                IEnumerable items => !items.Cast<object>().Any(),
                _ => value.Equals(property.GetValue(baseline)),
            };

            if (isUnpopulated)
            {
                unpopulated.Add(property.Name);
            }
        }

        return unpopulated;
    }

    /// <summary>
    /// The fully-populated snapshot fixture: every property must hold a value differing from the
    /// construction default (the fixture-completeness test enforces this), so every mapped field
    /// takes part in every trip. Carries <paramref name="filterTabDepth"/> levels of fully
    /// populated Filter Pipe children, so the round trips cover the recursion (the spec requires
    /// at least two).
    /// </summary>
    private static SessionSnapshot CreateFullSnapshot (int filterTabDepth = 2)
    {
        return new SessionSnapshot
        {
            FollowTail = true,
            Encoding = Encoding.UTF8,
            LineCount = 1234,
            CurrentLine = 42,
            FirstDisplayedLine = 17,
            FilterPosition = 333,
            FilterVisible = true,
            FilterAdvanced = true,
            CellSelectMode = true,
            FilterSaveListVisible = true,
            MultiFile = true,
            MultiFileMaxDays = 7,
            MultiFilePattern = "app*.log",
            TabName = "My Tab",
            HighlightGroupName = "Errors",
            FileName = @"C:\logs\app.log",
            BookmarkList = new SortedList<int, Bookmark> { [5] = new Bookmark(5, "a manual bookmark") },
            RowHeightList = new SortedList<int, RowHeightEntry> { [3] = new RowHeightEntry(3, 60) },
            MultiFileNames = ["app.1.log", "app.2.log"],
            FilterParams = CreateFilterParams("ERROR"),
            Columnizer = new DefaultLogfileColumnizer(),
            FilterTabs = filterTabDepth > 0
                ?
                [
                    new FilterTabSnapshot
                    {
                        FilterParams = CreateFilterParams($"WARN depth {filterTabDepth}"),
                        Snapshot = CreateFullSnapshot(filterTabDepth - 1),
                    }
                ]
                : [],
        };
    }

    private static FilterParams CreateFilterParams (string searchText)
    {
        return new FilterParams
        {
            SearchText = searchText,
            IsCaseSensitive = true,
            IsRegex = true,
            SpreadBefore = 1,
            SpreadBehind = 2,
        };
    }

    /// <summary>
    /// The populated <see cref="PersistenceData"/> fixture. Every carried field holds a value
    /// differing from the construction defaults (the fixture-completeness test enforces this),
    /// and the named-exclusion fields are populated on top, so the reverse trip proves the
    /// exclusion list is load-bearing. Children carry only mapped fields — the exclusion
    /// stripping is top-level by design, so nothing excluded may hide in the tree.
    /// </summary>
    private static PersistenceData CreateFullPersistenceData ()
    {
        var data = CreateCarriedPersistenceData(filterTabDepth: 2);

        data.SessionFileName = "legacy-never-set.lxp";
        data.BookmarkListPosition = 999;
        data.BookmarkListVisible = true;
        data.ShowBookmarkCommentColumn = true;
#pragma warning disable CS0618 // populated deliberately: the exclusion list must be load-bearing
        data.ColumnizerName = "legacy XML columnizer name";
#pragma warning restore CS0618
        data.SettingsSaveLoadLocation = "legacy save location";

        return data;
    }

    private static PersistenceData CreateCarriedPersistenceData (int filterTabDepth)
    {
        return new PersistenceData
        {
            // false because PersistenceData's initializer is true: a fixture value equal to the
            // construction default cannot prove the mapping exists.
            FollowTail = false,
            Encoding = Encoding.UTF8,
            LineCount = 1234,
            CurrentLine = 42,
            FirstDisplayedLine = 17,
            FilterPosition = 333,
            FilterVisible = true,
            FilterAdvanced = true,
            CellSelectMode = true,
            FilterSaveListVisible = true,
            MultiFile = true,
            MultiFileMaxDays = 7,
            MultiFilePattern = "app*.log",
            TabName = "My Tab",
            HighlightGroupName = "Errors",
            FileName = @"C:\logs\app.log",
            BookmarkList = new SortedList<int, Bookmark> { [5] = new Bookmark(5, "a manual bookmark") },
            RowHeightList = new SortedList<int, RowHeightEntry> { [3] = new RowHeightEntry(3, 60) },
            MultiFileNames = ["app.1.log", "app.2.log"],
            FilterParamsList = [CreateFilterParams("ERROR")],
            Columnizer = new DefaultLogfileColumnizer(),
            FilterTabDataList = filterTabDepth > 0
                ?
                [
                    new FilterTabData
                    {
                        FilterParams = CreateFilterParams($"WARN depth {filterTabDepth}"),
                        PersistenceData = CreateCarriedPersistenceData(filterTabDepth - 1),
                    }
                ]
                : [],
        };
    }

    /// <summary>
    /// Writes a Session File in the pre-JSON XML format, carrying <paramref name="searchTexts"/>
    /// as its filter list — the shape older LogExpert versions wrote, where that list is a copy
    /// of the whole Saved Filter List. Only the nodes the reader needs are present:
    /// <c>options</c> is mandatory (the reader dereferences it unconditionally), and each
    /// <c>filter</c> holds the base64 payload described on
    /// <see cref="EncodeLegacyXmlFilterParams"/>.
    /// </summary>
    private string WriteLegacyXmlSessionFile (params string[] searchTexts)
    {
        // Distinct spreads, so an assertion on the restored entry can tell which one it came from.
        var filters = string.Concat(searchTexts.Select((searchText, index) =>
            $"<filter><params>{EncodeLegacyXmlFilterParams(searchText, spreadBefore: index + 1)}</params></filter>"));

        var xml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <logexpert>
              <file fileName="{SecurityElement.Escape(_logFileName)}" lineCount="1234">
                <options>
                  <currentline line="42" />
                  <firstDisplayedLine line="17" />
                  <filter visible="1" advanced="1" position="333" />
                  <followTail enabled="1" />
                </options>
                <filters>{filters}</filters>
              </file>
            </logexpert>
            """;

        var fileName = Path.Join(_testDirectory, "legacy.lxp");
        File.WriteAllText(fileName, xml, Encoding.UTF8);

        return fileName;
    }

    /// <summary>
    /// The payload of one legacy <c>&lt;params&gt;</c> node, produced the way the XML writer
    /// produced it before it was deleted (see <c>Persister.WriteFilter</c> at <c>ef33f363^</c>):
    /// Newtonsoft with default settings, base64-encoded. Serializing here rather than
    /// hand-writing the JSON is the point — a hand-written literal only pins a shape we invented,
    /// and would silently omit the fields that are hard to read back (notably <c>Color</c>, which
    /// Newtonsoft writes as <c>"Black"</c>).
    /// </summary>
    private static string EncodeLegacyXmlFilterParams (string searchText, int spreadBefore)
    {
        var filterParams = CreateFilterParams(searchText);
        filterParams.SpreadBefore = spreadBefore;

        var json = JsonConvert.SerializeObject(filterParams);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    #endregion

    #region Fixture completeness (the regression guard)

    // Pins that the guard itself can fire — an all-defaults instance must be reported.
    [Test]
    public void FixtureCompleteness_UnpopulatedSnapshot_IsDetected ()
    {
        Assert.That(GetUnpopulatedProperties(new SessionSnapshot(), []), Is.Not.Empty);
    }

    [Test]
    public void FixtureCompleteness_SnapshotFixture_LeavesNoPropertyUnpopulated ()
    {
        Assert.That(GetUnpopulatedProperties(CreateFullSnapshot(), []), Is.Empty,
            "Extend CreateFullSnapshot — every SessionSnapshot property must hold a populated value so the round trips exercise it");
    }

    [Test]
    public void FixtureCompleteness_PersistenceDataFixture_LeavesNoMappedPropertyUnpopulated ()
    {
        Assert.That(GetUnpopulatedProperties(CreateFullPersistenceData(), _namedExclusions), Is.Empty,
            "Extend CreateFullPersistenceData (or, if the field legitimately never round-trips, justify it on the spec's named exclusion list)");
    }

    #endregion

    #region Round trips

    [Test]
    public void ForwardTrip_ComposeThenDecompose_ReturnsEqualSnapshot ()
    {
        var snapshot = CreateFullSnapshot();

        var persistenceData = SessionFileComposer.Compose(snapshot);
        var roundTripped = SessionFileComposer.Decompose(persistenceData);

        Assert.That(SerializeForComparison(roundTripped), Is.EqualTo(SerializeForComparison(snapshot)));
    }

    // The reverse trip catches new PersistenceData fields the snapshot doesn't carry — the
    // classic "added a setting, forgot to persist it" case starts on this side.
    [Test]
    public void ReverseTrip_DecomposeThenCompose_PreservesEverythingButTheNamedExclusions ()
    {
        var original = CreateFullPersistenceData();

        var snapshot = SessionFileComposer.Decompose(original);
        var recomposed = SessionFileComposer.Compose(snapshot);

        Assert.That(
            SerializeExcluding(recomposed, _namedExclusions),
            Is.EqualTo(SerializeExcluding(original, _namedExclusions)));
    }

    // The composer is pure: composing a Session File can never change live Log Window state (the
    // IsFilterTail side effect this seam removes lives at the Log Window's call site now).
    [Test]
    public void Compose_DoesNotMutateTheSnapshot ()
    {
        var snapshot = CreateFullSnapshot();
        var before = SerializeForComparison(snapshot);

        _ = SessionFileComposer.Compose(snapshot);

        Assert.That(SerializeForComparison(snapshot), Is.EqualTo(before));
    }

    [Test]
    public void Decompose_DoesNotMutateThePersistenceData ()
    {
        var persistenceData = CreateFullPersistenceData();
        var before = SerializeForComparison(persistenceData);

        _ = SessionFileComposer.Decompose(persistenceData);

        Assert.That(SerializeForComparison(persistenceData), Is.EqualTo(before));
    }

    // The flagship trip: one test guarding the mapping AND the serialization (the
    // missing-JSON-converter class of bug). Uses a non-SameDir save location because Persister
    // rewrites FileName on SameDir saves (transformed by design, see the spec).
    [Test]
    public void DiskTrip_ComposePersistLoadDecompose_ReturnsEqualSnapshot ()
    {
        var snapshot = CreateFullSnapshot();
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir,
        };

        var persistenceData = SessionFileComposer.Compose(snapshot);
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _sessionDirectory);
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, _sessionDirectory);
        var roundTripped = SessionFileComposer.Decompose(loadedData);

        Assert.That(File.Exists(savedFileName), Is.True, "The .lxp must actually have been written");
        Assert.That(SerializeForComparison(roundTripped), Is.EqualTo(SerializeForComparison(snapshot)));
    }

    #endregion

    #region Window filter (issue #666)

    // A Session File carries the Window Filter, not a copy of the Saved Filter List. The
    // on-disk shape stays a list so existing .lxp files keep loading — new saves just write
    // exactly one entry into it.
    [Test]
    public void Compose_WritesTheWindowFilterAsTheSingleFilterParamsListEntry ()
    {
        var snapshot = CreateFullSnapshot(filterTabDepth: 0);
        snapshot.FilterParams = CreateFilterParams("the window's own filter");

        var composed = SessionFileComposer.Compose(snapshot);

        Assert.That(composed.FilterParamsList, Has.Count.EqualTo(1));
        Assert.That(composed.FilterParamsList[0].SearchText, Is.EqualTo("the window's own filter"));
    }

    // SaveFilters off (and filter-less windows) leave the snapshot without a filter — the field
    // must not serialize a null element into the list.
    [Test]
    public void Compose_WithoutAWindowFilter_WritesAnEmptyFilterParamsList ()
    {
        var snapshot = CreateFullSnapshot(filterTabDepth: 0);
        snapshot.FilterParams = null;

        Assert.That(SessionFileComposer.Compose(snapshot).FilterParamsList, Is.Empty);
    }

    // The compatibility story: a pre-#666 Session File carries a copy of the whole Saved Filter
    // List, and load has always taken [0] as the Window Filter. That stays true — and the
    // trailing entries, which nothing ever restored, are dropped when such a file is re-saved.
    [Test]
    public void Decompose_LegacySessionFileCarryingTheGlobalList_KeepsTheFirstEntryAndDropsTheRest ()
    {
        var legacy = CreateCarriedPersistenceData(filterTabDepth: 0);
        legacy.FilterParamsList =
        [
            CreateFilterParams("the first global filter"),
            CreateFilterParams("another global filter"),
        ];

        var snapshot = SessionFileComposer.Decompose(legacy);
        var recomposed = SessionFileComposer.Compose(snapshot);

        Assert.That(snapshot.FilterParams.SearchText, Is.EqualTo("the first global filter"));
        Assert.That(recomposed.FilterParamsList, Has.Count.EqualTo(1));
        Assert.That(recomposed.FilterParamsList[0].SearchText, Is.EqualTo("the first global filter"));
    }

    // The oldest Session Files are XML, not JSON, and are read by a different reader
    // (PersisterXML) that fills the very same FilterParamsList with the old Saved Filter List. Loading
    // through Persister.Load exercises the real path — JSON parse fails, the XML fallback takes
    // over — so this pins that the [0] rule reaches such a file too, not just JSON ones.
    [Test]
    public void Decompose_LegacyXmlSessionFileOnDisk_TakesTheFirstFilterAsTheWindowFilter ()
    {
        var sessionFileName = WriteLegacyXmlSessionFile("the first global filter", "another global filter");

        var loaded = Core.Classes.Persister.Persister.Load(sessionFileName);
        var snapshot = SessionFileComposer.Decompose(loaded);

        Assert.That(loaded.FilterParamsList, Has.Count.EqualTo(2),
            "the XML fallback must actually have parsed both legacy entries — otherwise this test proves nothing");
        Assert.That(snapshot.FilterParams.SearchText, Is.EqualTo("the first global filter"));
        Assert.That(snapshot.FilterParams.SpreadBefore, Is.EqualTo(1),
            "the restored entry must be the first one, whole — the second carries SpreadBefore 2");
        Assert.That(snapshot.FilterParams.Color, Is.EqualTo(Color.Black),
            "Color is the field the legacy payload writes as \"Black\" — losing it means the entry was not really read");
    }

    [Test]
    public void Decompose_SessionFileWithoutFilters_LeavesTheWindowFilterUnset ()
    {
        var data = CreateCarriedPersistenceData(filterTabDepth: 0);
        data.FilterParamsList = [];

        Assert.That(SessionFileComposer.Decompose(data).FilterParams, Is.Null);
    }

    #endregion

    #region Omitted fields (the ✖ rows of the mapping table)

    // The snapshot does not carry the dead fields or SessionFileName, so Compose must leave them
    // at their construction defaults — the .lxp on-disk shape stays unchanged. (Decompose
    // ignoring them is structural: the snapshot has no such properties.) The reverse trip strips
    // these fields, so only this test would catch Compose writing into them.
    [Test]
    public void Compose_LeavesOmittedFieldsAtConstructionDefaults ()
    {
        var composed = SessionFileComposer.Compose(CreateFullSnapshot());
        var defaults = new PersistenceData();

        Assert.Multiple(() =>
        {
            Assert.That(composed.SessionFileName, Is.EqualTo(defaults.SessionFileName));
            Assert.That(composed.BookmarkListPosition, Is.EqualTo(defaults.BookmarkListPosition));
            Assert.That(composed.BookmarkListVisible, Is.EqualTo(defaults.BookmarkListVisible));
            Assert.That(composed.ShowBookmarkCommentColumn, Is.EqualTo(defaults.ShowBookmarkCommentColumn));
#pragma warning disable CS0618 // asserting the obsolete field stays at its default is the point
            Assert.That(composed.ColumnizerName, Is.EqualTo(defaults.ColumnizerName));
#pragma warning restore CS0618
            Assert.That(composed.SettingsSaveLoadLocation, Is.EqualTo(defaults.SettingsSaveLoadLocation));
        });
    }

    #endregion

    #region IsStale (Rollover staleness rule)

    // Saved LineCount greater than the file's current line count means the snapshot
    // belongs to a longer, since-rolled file.
    [TestCase(100, 50, ExpectedResult = true, TestName = "IsStale_SavedCountGreaterThanCurrent_IsStale")]
    [TestCase(100, 100, ExpectedResult = false, TestName = "IsStale_SavedCountEqualToCurrent_IsNotStale")]
    [TestCase(50, 100, ExpectedResult = false, TestName = "IsStale_SavedCountLessThanCurrent_IsNotStale")]
    [TestCase(0, 0, ExpectedResult = false, TestName = "IsStale_BothCountsZero_IsNotStale")]
    [TestCase(1, 0, ExpectedResult = true, TestName = "IsStale_SavedCountAgainstEmptyFile_IsStale")]
    [TestCase(0, 100, ExpectedResult = false, TestName = "IsStale_NoSavedCount_IsNotStale")]
    public bool IsStale_ComparesSavedLineCountToCurrent (int savedLineCount, int currentLineCount)
    {
        var snapshot = new SessionSnapshot { LineCount = savedLineCount };

        return SessionFileComposer.IsStale(snapshot, currentLineCount);
    }

    #endregion
}
