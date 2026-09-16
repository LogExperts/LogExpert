using System.Runtime.Versioning;

using LogExpert.Core.Classes.FileDrop;
using LogExpert.UI.Dialogs;

using NUnit.Framework;

namespace LogExpert.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
public class FolderDropDialogTests
{
    [Test]
    public void Preview_StartsUnselectedAndOpensOnlyFilesSelectedThroughTheFilter ()
    {
        string[] files = [@"C:\logs\a\same.log", @"C:\logs\b\same.log", @"C:\logs\output"];
        using var dialog = new FolderDropDialog(Task.FromResult(new DroppedFileDiscoveryResult(true, files, [])), CancellationToken.None);
        dialog.Show();
        Application.DoEvents();

        var grid = (DataGridView)dialog.Controls.Find("filesGrid", true).Single();
        var open = (Button)dialog.Controls.Find("openButton", true).Single();
        Assert.Multiple(() =>
        {
            Assert.That(grid.VirtualMode, Is.True);
            Assert.That(grid.RowCount, Is.EqualTo(3));
            Assert.That(open.Enabled, Is.False);
            Assert.That(dialog.SelectedFiles, Is.Empty);
        });

        dialog.Controls.Find("filterBox", true).Single().Text = @"\b\";
        ((Button)dialog.Controls.Find("selectAllButton", true).Single()).PerformClick();
        open.PerformClick();

        Assert.Multiple(() =>
        {
            Assert.That(dialog.DialogResult, Is.EqualTo(DialogResult.OK));
            Assert.That(dialog.SelectedFiles, Is.EqualTo([files[1]]));
        });
    }

    [Test]
    public void Preview_FilterKeepsHiddenSelectionsAndSelectNoneClearsThem ()
    {
        string[] files = [@"C:\logs\a.log", @"C:\logs\b.log"];
        using var dialog = new FolderDropDialog(Task.FromResult(new DroppedFileDiscoveryResult(true, files, [])), CancellationToken.None);
        dialog.Show();
        Application.DoEvents();
        var filter = dialog.Controls.Find("filterBox", true).Single();
        var all = (Button)dialog.Controls.Find("selectAllButton", true).Single();
        var none = (Button)dialog.Controls.Find("selectNoneButton", true).Single();
        var open = (Button)dialog.Controls.Find("openButton", true).Single();
        filter.Text = "a.log";
        all.PerformClick();
        filter.Text = "b.log";
        Assert.That(open.Enabled, Is.True);
        none.PerformClick();
        Assert.That(open.Enabled, Is.False);
        all.PerformClick();
        open.PerformClick();
        Assert.That(dialog.SelectedFiles, Is.EqualTo([files[1]]));
    }

    [Test]
    public void Preview_CheckboxSelectsOneFile ()
    {
        string[] files = [@"C:\logs\a.log", @"C:\logs\b.log"];
        using var dialog = new FolderDropDialog(Task.FromResult(new DroppedFileDiscoveryResult(true, files, [])), CancellationToken.None);
        dialog.Show();
        Application.DoEvents();
        var grid = (DataGridView)dialog.Controls.Find("filesGrid", true).Single();
        grid[0, 1].Value = true;
        ((Button)dialog.Controls.Find("openButton", true).Single()).PerformClick();
        Assert.That(dialog.SelectedFiles, Is.EqualTo([files[1]]));
    }

    [Test]
    public void Preview_CancelDiscardsSelection ()
    {
        using var dialog = new FolderDropDialog(Task.FromResult(new DroppedFileDiscoveryResult(true, [@"C:\logs\a.log"], [])), CancellationToken.None);
        dialog.Show();
        Application.DoEvents();
        ((Button)dialog.Controls.Find("selectAllButton", true).Single()).PerformClick();
        ((Button)dialog.Controls.Find("cancelButton", true).Single()).PerformClick();
        Assert.That(dialog.SelectedFiles, Is.Empty);
        Assert.That(dialog.DialogResult, Is.EqualTo(DialogResult.Cancel));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Discovery_CancelOrDisposeBeforeCompletion_DiscardsLateResults (bool dispose)
    {
        var completion = new TaskCompletionSource<DroppedFileDiscoveryResult>();
        using var dialog = new FolderDropDialog(completion.Task, CancellationToken.None);
        dialog.Show();
        Application.DoEvents();
        Assert.That(((Button)dialog.Controls.Find("openButton", true).Single()).Enabled, Is.False);
        if (dispose)
        {
            dialog.Dispose();
        }
        else
        {
            ((Button)dialog.Controls.Find("cancelButton", true).Single()).PerformClick();
        }

        completion.SetResult(new DroppedFileDiscoveryResult(true, [@"C:\logs\late.log"], []));
        Application.DoEvents();
        Assert.That(dialog.SelectedFiles, Is.Empty);
        Assert.That(dialog.IsDisposed, Is.True);
    }

    [Test]
    public void Discovery_OwnerCancellation_ClosesPendingDialog ()
    {
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<DroppedFileDiscoveryResult>();
        using var dialog = new FolderDropDialog(completion.Task, cancellation.Token);
        dialog.Show();
        Application.DoEvents();
        cancellation.Cancel();
        Application.DoEvents();
        Assert.That(dialog.SelectedFiles, Is.Empty);
        Assert.That(dialog.Visible, Is.False);
        completion.SetResult(new DroppedFileDiscoveryResult(true, [@"C:\logs\late.log"], []));
        Application.DoEvents();
    }

    [Test]
    public void Preview_EmptyDiscoveryReportsEmptyResultAndSkippedLocations ()
    {
        using var dialog = new FolderDropDialog(Task.FromResult(new DroppedFileDiscoveryResult(true, [],
            [new SkippedDropPath(@"C:\logs\link", null), new SkippedDropPath(@"C:\logs\denied", "Access denied")])), CancellationToken.None);
        dialog.Show();
        Application.DoEvents();
        var report = dialog.Controls.Find("skippedPaths", true).Single();
        Assert.That(report.Text, Does.Contain(@"C:\logs\link").And.Contain(@"C:\logs\denied"));
        Assert.That(((Button)dialog.Controls.Find("openButton", true).Single()).Enabled, Is.False);
        Assert.That(dialog.Controls.Find("statusLabel", true).Single().Text, Is.EqualTo(Resources.FolderDrop_Empty));
    }

    [Test]
    public void Preview_LargeCandidateList_CanFilterAndConfirmLastFile ()
    {
        var files = Enumerable.Range(0, 100000).Select(index => $@"C:\logs\{index:D6}.txt").ToArray();
        using var dialog = new FolderDropDialog(Task.FromResult(new DroppedFileDiscoveryResult(true, files, [])), CancellationToken.None);
        dialog.Show();
        Application.DoEvents();
        dialog.Controls.Find("filterBox", true).Single().Text = "099999.txt";
        var grid = (DataGridView)dialog.Controls.Find("filesGrid", true).Single();
        Assert.That(grid.RowCount, Is.EqualTo(1));
        ((Button)dialog.Controls.Find("selectAllButton", true).Single()).PerformClick();
        ((Button)dialog.Controls.Find("openButton", true).Single()).PerformClick();
        Assert.That(dialog.SelectedFiles, Is.EqualTo([@"C:\logs\099999.txt"]));
    }

    [Test]
    public void Preview_PagingKeepsSelectionsAcrossPages ()
    {
        var files = Enumerable.Range(0, 750).Select(index => $@"C:\logs\{index:D6}.txt").ToArray();
        using var dialog = new FolderDropDialog(Task.FromResult(new DroppedFileDiscoveryResult(true, files, [])), CancellationToken.None);
        dialog.Show();
        Application.DoEvents();
        var grid = (DataGridView)dialog.Controls.Find("filesGrid", true).Single();
        Assert.That(grid.RowCount, Is.EqualTo(500));
        grid[0, 0].Value = true;
        ((Button)dialog.Controls.Find("nextPageButton", true).Single()).PerformClick();
        Assert.That(grid.RowCount, Is.EqualTo(250));
        grid[0, 0].Value = true;
        ((Button)dialog.Controls.Find("previousPageButton", true).Single()).PerformClick();
        Assert.That(grid[0, 0].Value, Is.True);
        ((Button)dialog.Controls.Find("openButton", true).Single()).PerformClick();
        Assert.That(dialog.SelectedFiles, Is.EqualTo([@"C:\logs\000000.txt", @"C:\logs\000500.txt"]));
    }
}