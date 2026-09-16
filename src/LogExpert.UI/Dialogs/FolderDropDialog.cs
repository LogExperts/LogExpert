using System.Globalization;
using System.Runtime.Versioning;

using LogExpert.Core.Classes.FileDrop;

using NLog;

namespace LogExpert.UI.Dialogs;

/// <summary>One cancellable discovery and selection session. No files are opened by this dialog.</summary>
[SupportedOSPlatform("windows")]
internal sealed class FolderDropDialog : Form
{
    private const int PAGE_SIZE = 500;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Task<DroppedFileDiscoveryResult> _discovery;
    private readonly CancellationTokenSource _cancellation;
    private readonly Label _status = new() { Name = "statusLabel", AutoSize = true, Dock = DockStyle.Fill };
    private readonly TextBox _filter = new() { Name = "filterBox", Width = 320, Enabled = false };
    private readonly Button _selectAll = new() { Name = "selectAllButton", AutoSize = true, Enabled = false };
    private readonly Button _selectNone = new() { Name = "selectNoneButton", AutoSize = true, Enabled = false };
    private readonly Button _open = new() { Name = "openButton", AutoSize = true, Enabled = false };
    private readonly TextBox _skipped = new() { Name = "skippedPaths", Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Visible = false };
    private readonly DataGridView _grid = new()
    {
        Name = "filesGrid", Dock = DockStyle.Fill, VirtualMode = true,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };
    private readonly Button _previous = new() { Name = "previousPageButton", AutoSize = true, Enabled = false };
    private readonly Button _next = new() { Name = "nextPageButton", AutoSize = true, Enabled = false };
    private readonly Label _page = new() { AutoSize = true, Margin = new Padding(3, 7, 3, 3) };
    private int _pageStart;
    private string[] _files = [];
    private bool[] _selected = [];
    private int[] _visible = [];

    public string[] SelectedFiles { get; private set; } = [];

    public FolderDropDialog (Task<DroppedFileDiscoveryResult> discovery, CancellationToken cancellationToken)
    {
        _discovery = discovery;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = Resources.FolderDrop_Title;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Size = new Size(860, 590);
        MinimumSize = new Size(620, 420);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = Resources.FolderDrop_Instructions, AutoSize = true, Dock = DockStyle.Fill }, 0, 0);

        var filterPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        filterPanel.Controls.Add(new Label { Text = Resources.FolderDrop_Filter, AutoSize = true, Margin = new Padding(3, 7, 3, 3) });
        filterPanel.Controls.Add(_filter);
        _selectAll.Text = Resources.FolderDrop_SelectAll;
        _selectNone.Text = Resources.FolderDrop_SelectNone;
        filterPanel.Controls.Add(_selectAll);
        filterPanel.Controls.Add(_selectNone);
        layout.Controls.Add(filterPanel, 0, 1);

        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = Resources.FolderDrop_Select, Width = 65, SortMode = DataGridViewColumnSortMode.NotSortable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Resources.FolderDrop_Path, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        layout.Controls.Add(_grid, 0, 2);
        _status.Text = Resources.FolderDrop_Discovering;
        var pagination = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        _previous.Text = Resources.FolderDrop_Previous;
        _next.Text = Resources.FolderDrop_Next;
        pagination.Controls.Add(_previous);
        pagination.Controls.Add(_next);
        pagination.Controls.Add(_page);
        layout.Controls.Add(pagination, 0, 3);
        layout.Controls.Add(_status, 0, 4);
        _skipped.Height = 95;
        layout.Controls.Add(_skipped, 0, 5);

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Name = "cancelButton", Text = Resources.FolderDrop_Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
        _open.Text = Resources.FolderDrop_Open;
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(_open);
        layout.Controls.Add(buttons, 0, 6);
        AcceptButton = _open;
        CancelButton = cancel;

        _previous.Click += (_, _) => ChangePage(-PAGE_SIZE);
        _next.Click += (_, _) => ChangePage(PAGE_SIZE);
        _filter.TextChanged += (_, _) => ApplyFilter();
        _selectAll.Click += (_, _) =>
        {
            _grid.EndEdit();
            foreach (var index in _visible)
            {
                _selected[index] = true;
            }
            UpdateSelection();
        };
        _selectNone.Click += (_, _) =>
        {
            _grid.EndEdit();
            Array.Clear(_selected);
            UpdateSelection();
        };
        _grid.CellValueNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && _pageStart + e.RowIndex < _visible.Length)
            {
                var index = _visible[_pageStart + e.RowIndex];
                e.Value = e.ColumnIndex == 0 ? _selected[index] : _files[index];
            }
        };
        _grid.CellValuePushed += (_, e) =>
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0 && _pageStart + e.RowIndex < _visible.Length)
            {
                _selected[_visible[_pageStart + e.RowIndex]] = e.Value is true;
                UpdateSelection();
            }
        };
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _open.Click += (_, _) =>
        {
            _grid.EndEdit();
            SelectedFiles = _files.Where((_, index) => _selected[index]).ToArray();
            if (SelectedFiles.Length > 0)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        cancel.Click += (_, _) => Close();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Keep failures at the async UI boundary inside the cancellable preview.")]
    protected override async void OnShown (EventArgs e)
    {
        base.OnShown(e);
        try
        {
            var result = await _discovery.WaitAsync(_cancellation.Token).ConfigureAwait(true);
            if (IsDisposed || _cancellation.IsCancellationRequested)
            {
                return;
            }
            if (!result.IncludesFolders && result.Skipped.Length == 0)
            {
                SelectedFiles = result.Files;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _files = result.Files;
            _selected = new bool[_files.Length];
            _filter.Enabled = true;
            _selectAll.Enabled = _files.Length > 0;
            _selectNone.Enabled = _files.Length > 0;
            if (result.Skipped.Length > 0)
            {
                _skipped.Text = string.Format(CultureInfo.CurrentCulture, Resources.FolderDrop_Skipped, result.Skipped.Length)
                    + Environment.NewLine + string.Join(Environment.NewLine, result.Skipped.Select(skip => skip.Path + " — " + (skip.Reason ?? Resources.FolderDrop_DirectoryLink)));
                _skipped.Visible = true;
            }
            ApplyFilter();
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Folder drop discovery failed");
            if (!IsDisposed && !_cancellation.IsCancellationRequested)
            {
                _status.Text = string.Format(CultureInfo.CurrentCulture, Resources.FolderDrop_Failed, ex.Message);
            }
        }
    }

    private void ApplyFilter ()
    {
        _grid.EndEdit();
        _grid.CurrentCell = null;
        _pageStart = 0;
        _visible = Enumerable.Range(0, _files.Length)
            .Where(index => _files[index].Contains(_filter.Text, StringComparison.OrdinalIgnoreCase)).ToArray();
        ShowPage();
    }

    private void ChangePage (int offset)
    {
        _grid.EndEdit();
        _grid.CurrentCell = null;
        _pageStart += offset;
        ShowPage();
    }

    private void ShowPage ()
    {
        // VirtualMode still allocates DataGridView rows; bound that cost for large folder trees.
        _grid.RowCount = Math.Min(PAGE_SIZE, _visible.Length - _pageStart);
        _previous.Enabled = _pageStart > 0;
        _next.Enabled = _pageStart + PAGE_SIZE < _visible.Length;
        _page.Text = string.Format(CultureInfo.CurrentCulture, Resources.FolderDrop_Page,
            _visible.Length == 0 ? 0 : _pageStart + 1, _pageStart + _grid.RowCount, _visible.Length);
        UpdateSelection();
    }
    private void UpdateSelection ()
    {
        var count = _selected.Count(selected => selected);
        _open.Enabled = count > 0;
        _status.Text = _files.Length == 0 ? Resources.FolderDrop_Empty
            : string.Format(CultureInfo.CurrentCulture, Resources.FolderDrop_Summary, _files.Length, _visible.Length, count);
        _grid.Invalidate();
    }

    protected override void OnFormClosed (FormClosedEventArgs e)
    {
        _cancellation.Cancel();
        base.OnFormClosed(e);
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
        }
        base.Dispose(disposing);
    }
}