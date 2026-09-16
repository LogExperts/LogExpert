using LogExpert.UI.Dialogs;

using NUnit.Framework;

namespace LogExpert.Tests.Dialogs;

/// <summary>
/// Regression tests for the Regex Helper dialog's OK handler. Both combo boxes are
/// DataSource-bound to the history lists (LoadHistory, run on dialog Load), so the
/// handler must never touch ComboBox.Items — doing so throws
/// "Items collection cannot be modified when the DataSource property is set".
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class RegexHelperDialogTests
{
    [Test]
    public void OkClick_AfterLoadBindsHistory_DoesNotThrowAndPromotesPattern ()
    {
        using RegexHelperDialog dialog = new()
        {
            ExpressionHistoryList = ["old-a", "old-b"],
            TesttextHistoryList = ["text-a"],
        };
        dialog.LoadHistory(); // what dialog Load does when the form is shown
        dialog.Pattern = "new-pattern";

        dialog.OnButtonOkClick(dialog, EventArgs.Empty);

        Assert.That(dialog.ExpressionHistoryList, Is.EqualTo(["new-pattern", "old-a", "old-b"]));
    }

    [Test]
    public void OkClick_PatternAlreadyInHistory_MovesItToTheFrontWithoutDuplicate ()
    {
        using RegexHelperDialog dialog = new()
        {
            ExpressionHistoryList = ["old-a", "repeat", "old-b"],
        };
        dialog.LoadHistory();
        dialog.Pattern = "repeat";

        dialog.OnButtonOkClick(dialog, EventArgs.Empty);

        Assert.That(dialog.ExpressionHistoryList, Is.EqualTo(["repeat", "old-a", "old-b"]));
    }

    [Test]
    public void OkClick_HistoryAtCapacity_DropsTheOldestEntry ()
    {
        using RegexHelperDialog dialog = new()
        {
            ExpressionHistoryList = [.. Enumerable.Range(0, 30).Select(i => $"entry-{i}")],
        };
        dialog.LoadHistory();
        dialog.Pattern = "newest";

        dialog.OnButtonOkClick(dialog, EventArgs.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(dialog.ExpressionHistoryList, Has.Count.EqualTo(30));
            Assert.That(dialog.ExpressionHistoryList[0], Is.EqualTo("newest"));
            Assert.That(dialog.ExpressionHistoryList, Does.Not.Contain("entry-29"));
        });
    }
}
