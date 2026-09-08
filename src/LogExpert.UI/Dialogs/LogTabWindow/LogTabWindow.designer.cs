using System.Windows.Forms;
using LogExpert.Core.Enums;
using LogExpert.Dialogs;
using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Controls.LogTabWindow
{
    partial class LogTabWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent ()
        {
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(LogTabWindow));
            statusStrip = new StatusStrip();
            labelLines = new ToolStripStatusLabel();
            labelSize = new ToolStripStatusLabel();
            labelCurrentLine = new ToolStripStatusLabel();
            loadProgessBar = new ToolStripProgressBar();
            labelStatus = new ToolStripStatusLabel();
            mainMenuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            openURIToolStripMenuItem = new ToolStripMenuItem();
            closeFileToolStripMenuItem = new ToolStripMenuItem();
            reloadToolStripMenuItem = new ToolStripMenuItem();
            newFromClipboardToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator1 = new ToolStripSeparator();
            multiFileToolStripMenuItem = new ToolStripMenuItem();
            multiFileEnabledStripMenuItem = new ToolStripMenuItem();
            multifileMaskToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator2 = new ToolStripSeparator();
            loadSessionToolStripMenuItem = new ToolStripMenuItem();
            saveSessionToolStripMenuItem = new ToolStripMenuItem();
            exportBookmarksToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator3 = new ToolStripSeparator();
            lastUsedToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            viewNavigateToolStripMenuItem = new ToolStripMenuItem();
            goToLineToolStripMenuItem = new ToolStripMenuItem();
            searchToolStripMenuItem = new ToolStripMenuItem();
            filterToolStripMenuItem = new ToolStripMenuItem();
            bookmarksToolStripMenuItem = new ToolStripMenuItem();
            toggleBookmarkToolStripMenuItem = new ToolStripMenuItem();
            jumpToNextToolStripMenuItem = new ToolStripMenuItem();
            jumpToPrevToolStripMenuItem = new ToolStripMenuItem();
            showBookmarkListToolStripMenuItem = new ToolStripMenuItem();
            columnFinderToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator5 = new ToolStripSeparator();
            encodingToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator6 = new ToolStripSeparator();
            timeshiftToolStripMenuItem = new ToolStripMenuItem();
            timeshiftToolStripTextBox = new ToolStripTextBox();
            ToolStripSeparator4 = new ToolStripSeparator();
            copyMarkedLinesIntoNewTabToolStripMenuItem = new ToolStripMenuItem();
            optionToolStripMenuItem = new ToolStripMenuItem();
            columnizerToolStripMenuItem = new ToolStripMenuItem();
            hilightingToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator7 = new ToolStripSeparator();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            pluginTrustManagementToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator9 = new ToolStripSeparator();
            cellSelectModeToolStripMenuItem = new ToolStripMenuItem();
            alwaysOnTopToolStripMenuItem = new ToolStripMenuItem();
            hideLineColumnToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator8 = new ToolStripSeparator();
            lockInstanceToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            configureToolStripMenuItem = new ToolStripMenuItem();
            configureToolStripSeparator = new ToolStripSeparator();
            helpToolStripMenuItem = new ToolStripMenuItem();
            showHelpToolStripMenuItem = new ToolStripMenuItem();
            ToolStripSeparator11 = new ToolStripSeparator();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            debugToolStripMenuItem = new ToolStripMenuItem();
            dumpLogBufferInfoToolStripMenuItem = new ToolStripMenuItem();
            dumpBufferDiagnosticToolStripMenuItem = new ToolStripMenuItem();
            runGCToolStripMenuItem = new ToolStripMenuItem();
            gCInfoToolStripMenuItem = new ToolStripMenuItem();
            throwExceptionGUIThreadToolStripMenuItem = new ToolStripMenuItem();
            throwExceptionbackgroundThToolStripMenuItem = new ToolStripMenuItem();
            throwExceptionBackgroundThreadToolStripMenuItem = new ToolStripMenuItem();
            loglevelToolStripMenuItem = new ToolStripMenuItem();
            warnLogLevelToolStripMenuItem = new ToolStripMenuItem();
            infoLogLevelToolStripMenuItem = new ToolStripMenuItem();
            debugLogLevelToolStripMenuItem = new ToolStripMenuItem();
            disableWordHighlightModeToolStripMenuItem = new ToolStripMenuItem();
            checkBoxHost = new CheckBox();
            toolStripContainer = new ToolStripContainer();
            dockPanel = new DockPanel();
            externalToolsToolStrip = new ToolStrip();
            buttonToolStrip = new ToolStrip();
            toolStripButtonOpen = new ToolStripButton();
            lineToolStripSeparatorExtension1 = new ToolStripSeparator();
            toolStripButtonSearch = new ToolStripButton();
            toolStripButtonFilter = new ToolStripButton();
            lineToolStripSeparatorExtension2 = new ToolStripSeparator();
            toolStripButtonBookmark = new ToolStripButton();
            toolStripButtonUp = new ToolStripButton();
            toolStripButtonDown = new ToolStripButton();
            lineToolStripSeparatorExtension3 = new ToolStripSeparator();
            toolStripButtonBubbles = new ToolStripButton();
            lineToolStripSeparatorExtension4 = new ToolStripSeparator();
            toolStripButtonTail = new ToolStripButton();
            lineToolStripSeparatorExtension5 = new ToolStripSeparator();
            highlightGroupsToolStripComboBox = new ToolStripComboBox();
            checkBoxFollowTail = new CheckBox();
            tabContextMenuStrip = new ContextMenuStrip(components);
            closeThisTabToolStripMenuItem = new ToolStripMenuItem();
            closeOtherTabsToolStripMenuItem = new ToolStripMenuItem();
            closeAllTabsToolStripMenuItem = new ToolStripMenuItem();
            tabColorToolStripMenuItem = new ToolStripMenuItem();
            tabRenameToolStripMenuItem = new ToolStripMenuItem();
            copyPathToClipboardToolStripMenuItem = new ToolStripMenuItem();
            findInExplorerToolStripMenuItem = new ToolStripMenuItem();
            truncateFileToolStripMenuItem = new ToolStripMenuItem();
            dragControlDateTime = new DateTimeDragControl();
            statusStrip.SuspendLayout();
            mainMenuStrip.SuspendLayout();
            toolStripContainer.ContentPanel.SuspendLayout();
            toolStripContainer.TopToolStripPanel.SuspendLayout();
            toolStripContainer.SuspendLayout();
            buttonToolStrip.SuspendLayout();
            tabContextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip
            // 
            statusStrip.AutoSize = false;
            statusStrip.ImageScalingSize = new Size(24, 24);
            statusStrip.Items.AddRange(new ToolStripItem[] { labelLines, labelSize, labelCurrentLine, loadProgessBar, labelStatus });
            statusStrip.Location = new Point(0, 982);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(3, 0, 23, 0);
            statusStrip.Size = new Size(1603, 35);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 5;
            statusStrip.Text = "statusStrip1";
            // 
            // labelLines
            // 
            labelLines.AutoSize = false;
            labelLines.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            labelLines.BorderStyle = Border3DStyle.SunkenOuter;
            labelLines.Name = "labelLines";
            labelLines.Size = new Size(26, 30);
            labelLines.Text = "0";
            // 
            // labelSize
            // 
            labelSize.AutoSize = false;
            labelSize.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            labelSize.BorderStyle = Border3DStyle.SunkenOuter;
            labelSize.Name = "labelSize";
            labelSize.Size = new Size(26, 30);
            labelSize.Text = "0";
            // 
            // labelCurrentLine
            // 
            labelCurrentLine.AutoSize = false;
            labelCurrentLine.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            labelCurrentLine.BorderStyle = Border3DStyle.SunkenOuter;
            labelCurrentLine.Name = "labelCurrentLine";
            labelCurrentLine.Size = new Size(28, 30);
            labelCurrentLine.Text = "L:";
            // 
            // loadProgessBar
            // 
            loadProgessBar.Name = "loadProgessBar";
            loadProgessBar.Size = new Size(83, 29);
            // 
            // labelStatus
            // 
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(39, 30);
            labelStatus.Text = "Ready";
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.AllowMerge = false;
            mainMenuStrip.Dock = DockStyle.None;
            mainMenuStrip.ImageScalingSize = new Size(24, 24);
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewNavigateToolStripMenuItem, optionToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem, debugToolStripMenuItem });
            mainMenuStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            mainMenuStrip.Location = new Point(0, 31);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(1603, 23);
            mainMenuStrip.TabIndex = 6;
            mainMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, openURIToolStripMenuItem, closeFileToolStripMenuItem, reloadToolStripMenuItem, newFromClipboardToolStripMenuItem, ToolStripSeparator1, multiFileToolStripMenuItem, ToolStripSeparator2, loadSessionToolStripMenuItem, saveSessionToolStripMenuItem, exportBookmarksToolStripMenuItem, ToolStripSeparator3, lastUsedToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 19);
            fileToolStripMenuItem.Text = "File";
            fileToolStripMenuItem.DropDownOpening += OnFileToolStripMenuItemDropDownOpening;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.BackColor = SystemColors.Control;
            openToolStripMenuItem.Image = LogExpert.Resources.File_open;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(243, 22);
            openToolStripMenuItem.Text = "Open...";
            openToolStripMenuItem.Click += OnOpenToolStripMenuItemClick;
            // 
            // openURIToolStripMenuItem
            // 
            openURIToolStripMenuItem.Name = "openURIToolStripMenuItem";
            openURIToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.U;
            openURIToolStripMenuItem.Size = new Size(243, 22);
            openURIToolStripMenuItem.Text = "Open URL...";
            openURIToolStripMenuItem.ToolTipText = "Opens a file by entering a URL which is supported by a file system plugin";
            openURIToolStripMenuItem.Click += OnOpenURIToolStripMenuItemClick;
            // 
            // closeFileToolStripMenuItem
            // 
            closeFileToolStripMenuItem.Image = LogExpert.Resources.Close;
            closeFileToolStripMenuItem.Name = "closeFileToolStripMenuItem";
            closeFileToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F4;
            closeFileToolStripMenuItem.Size = new Size(243, 22);
            closeFileToolStripMenuItem.Text = "Close File";
            closeFileToolStripMenuItem.Click += OnCloseFileToolStripMenuItemClick;
            // 
            // reloadToolStripMenuItem
            // 
            reloadToolStripMenuItem.Image = LogExpert.Resources.Restart_alt;
            reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            reloadToolStripMenuItem.ShortcutKeys = Keys.F5;
            reloadToolStripMenuItem.Size = new Size(243, 22);
            reloadToolStripMenuItem.Text = "Reload";
            reloadToolStripMenuItem.Click += OnReloadToolStripMenuItemClick;
            // 
            // newFromClipboardToolStripMenuItem
            // 
            newFromClipboardToolStripMenuItem.Name = "newFromClipboardToolStripMenuItem";
            newFromClipboardToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newFromClipboardToolStripMenuItem.Size = new Size(243, 22);
            newFromClipboardToolStripMenuItem.Text = "New tab from clipboard";
            newFromClipboardToolStripMenuItem.ToolTipText = "Creates a new tab with content from clipboard";
            newFromClipboardToolStripMenuItem.Click += OnNewFromClipboardToolStripMenuItemClick;
            // 
            // ToolStripSeparator1
            // 
            ToolStripSeparator1.Name = "ToolStripSeparator1";
            ToolStripSeparator1.Size = new Size(240, 6);
            // 
            // multiFileToolStripMenuItem
            // 
            multiFileToolStripMenuItem.CheckOnClick = true;
            multiFileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { multiFileEnabledStripMenuItem, multifileMaskToolStripMenuItem });
            multiFileToolStripMenuItem.Name = "multiFileToolStripMenuItem";
            multiFileToolStripMenuItem.Size = new Size(243, 22);
            multiFileToolStripMenuItem.Text = "MultiFile";
            multiFileToolStripMenuItem.ToolTipText = "Treat multiple files as one large file (e.g. data.log, data.log.1, data.log.2,...)";
            multiFileToolStripMenuItem.Click += OnMultiFileToolStripMenuItemClick;
            // 
            // multiFileEnabledStripMenuItem
            // 
            multiFileEnabledStripMenuItem.CheckOnClick = true;
            multiFileEnabledStripMenuItem.Name = "multiFileEnabledStripMenuItem";
            multiFileEnabledStripMenuItem.Size = new Size(165, 22);
            multiFileEnabledStripMenuItem.Text = "Enable MultiFile";
            multiFileEnabledStripMenuItem.Click += OnMultiFileEnabledStripMenuItemClick;
            // 
            // multifileMaskToolStripMenuItem
            // 
            multifileMaskToolStripMenuItem.Name = "multifileMaskToolStripMenuItem";
            multifileMaskToolStripMenuItem.Size = new Size(165, 22);
            multifileMaskToolStripMenuItem.Text = "File name mask...";
            multifileMaskToolStripMenuItem.Click += OnMultiFileMaskToolStripMenuItemClick;
            // 
            // ToolStripSeparator2
            // 
            ToolStripSeparator2.Name = "ToolStripSeparator2";
            ToolStripSeparator2.Size = new Size(240, 6);
            // 
            // loadSessionToolStripMenuItem
            // 
            loadSessionToolStripMenuItem.Name = "loadSessionToolStripMenuItem";
            loadSessionToolStripMenuItem.Size = new Size(243, 22);
            loadSessionToolStripMenuItem.Text = "Load session...";
            loadSessionToolStripMenuItem.ToolTipText = "Load a saved session (list of log files)";
            loadSessionToolStripMenuItem.Click += OnLoadSessionToolStripMenuItemClick;
            // 
            // saveSessionToolStripMenuItem
            // 
            saveSessionToolStripMenuItem.Name = "saveSessionToolStripMenuItem";
            saveSessionToolStripMenuItem.Size = new Size(243, 22);
            saveSessionToolStripMenuItem.Text = "Save session...";
            saveSessionToolStripMenuItem.ToolTipText = "Save a session (all open tabs)";
            saveSessionToolStripMenuItem.Click += OnSaveSessionToolStripMenuItemClick;
            // 
            // exportBookmarksToolStripMenuItem
            // 
            exportBookmarksToolStripMenuItem.Name = "exportBookmarksToolStripMenuItem";
            exportBookmarksToolStripMenuItem.Size = new Size(243, 22);
            exportBookmarksToolStripMenuItem.Text = "Export bookmarks...";
            exportBookmarksToolStripMenuItem.ToolTipText = "Write a list of bookmarks and their comments to a CSV file";
            exportBookmarksToolStripMenuItem.Click += OnExportBookmarksToolStripMenuItemClick;
            // 
            // ToolStripSeparator3
            // 
            ToolStripSeparator3.Name = "ToolStripSeparator3";
            ToolStripSeparator3.Size = new Size(240, 6);
            // 
            // lastUsedToolStripMenuItem
            // 
            lastUsedToolStripMenuItem.Name = "lastUsedToolStripMenuItem";
            lastUsedToolStripMenuItem.Size = new Size(243, 22);
            lastUsedToolStripMenuItem.Text = "Last used";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Image = LogExpert.Resources.Exit;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitToolStripMenuItem.Size = new Size(243, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += OnExitToolStripMenuItemClick;
            // 
            // viewNavigateToolStripMenuItem
            // 
            viewNavigateToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { goToLineToolStripMenuItem, searchToolStripMenuItem, filterToolStripMenuItem, bookmarksToolStripMenuItem, columnFinderToolStripMenuItem, ToolStripSeparator5, encodingToolStripMenuItem, ToolStripSeparator6, timeshiftToolStripMenuItem, timeshiftToolStripTextBox, ToolStripSeparator4, copyMarkedLinesIntoNewTabToolStripMenuItem });
            viewNavigateToolStripMenuItem.Name = "viewNavigateToolStripMenuItem";
            viewNavigateToolStripMenuItem.Size = new Size(96, 19);
            viewNavigateToolStripMenuItem.Text = "View/Navigate";
            // 
            // goToLineToolStripMenuItem
            // 
            goToLineToolStripMenuItem.Name = "goToLineToolStripMenuItem";
            goToLineToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.G;
            goToLineToolStripMenuItem.Size = new Size(177, 22);
            goToLineToolStripMenuItem.Text = "Go to line...";
            goToLineToolStripMenuItem.Click += OnGoToLineToolStripMenuItemClick;
            // 
            // searchToolStripMenuItem
            // 
            searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            searchToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            searchToolStripMenuItem.Size = new Size(177, 22);
            searchToolStripMenuItem.Text = "Search...";
            searchToolStripMenuItem.Click += OnSearchToolStripMenuItemClick;
            // 
            // filterToolStripMenuItem
            // 
            filterToolStripMenuItem.Image = LogExpert.Resources.Filter;
            filterToolStripMenuItem.Name = "filterToolStripMenuItem";
            filterToolStripMenuItem.ShortcutKeys = Keys.F4;
            filterToolStripMenuItem.Size = new Size(177, 22);
            filterToolStripMenuItem.Text = "Filter";
            filterToolStripMenuItem.Click += OnFilterToolStripMenuItemClick;
            // 
            // bookmarksToolStripMenuItem
            // 
            bookmarksToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toggleBookmarkToolStripMenuItem, jumpToNextToolStripMenuItem, jumpToPrevToolStripMenuItem, showBookmarkListToolStripMenuItem });
            bookmarksToolStripMenuItem.Name = "bookmarksToolStripMenuItem";
            bookmarksToolStripMenuItem.Size = new Size(177, 22);
            bookmarksToolStripMenuItem.Text = "Bookmarks";
            // 
            // toggleBookmarkToolStripMenuItem
            // 
            toggleBookmarkToolStripMenuItem.BackColor = SystemColors.Control;
            toggleBookmarkToolStripMenuItem.ForeColor = SystemColors.ControlDarkDark;
            toggleBookmarkToolStripMenuItem.Image = LogExpert.Resources.Bookmark_add;
            toggleBookmarkToolStripMenuItem.Name = "toggleBookmarkToolStripMenuItem";
            toggleBookmarkToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F2;
            toggleBookmarkToolStripMenuItem.Size = new Size(212, 22);
            toggleBookmarkToolStripMenuItem.Text = "Toggle Bookmark";
            toggleBookmarkToolStripMenuItem.Click += OnToggleBookmarkToolStripMenuItemClick;
            // 
            // jumpToNextToolStripMenuItem
            // 
            jumpToNextToolStripMenuItem.BackColor = SystemColors.Control;
            jumpToNextToolStripMenuItem.ForeColor = SystemColors.ControlDarkDark;
            jumpToNextToolStripMenuItem.Image = LogExpert.Resources.ArrowDown;
            jumpToNextToolStripMenuItem.Name = "jumpToNextToolStripMenuItem";
            jumpToNextToolStripMenuItem.ShortcutKeys = Keys.F2;
            jumpToNextToolStripMenuItem.Size = new Size(212, 22);
            jumpToNextToolStripMenuItem.Text = "Jump to next";
            jumpToNextToolStripMenuItem.Click += OnJumpToNextToolStripMenuItemClick;
            // 
            // jumpToPrevToolStripMenuItem
            // 
            jumpToPrevToolStripMenuItem.BackColor = SystemColors.Control;
            jumpToPrevToolStripMenuItem.ForeColor = SystemColors.ControlDarkDark;
            jumpToPrevToolStripMenuItem.Image = LogExpert.Resources.ArrowUp;
            jumpToPrevToolStripMenuItem.Name = "jumpToPrevToolStripMenuItem";
            jumpToPrevToolStripMenuItem.ShortcutKeys = Keys.Shift | Keys.F2;
            jumpToPrevToolStripMenuItem.Size = new Size(212, 22);
            jumpToPrevToolStripMenuItem.Text = "Jump to prev";
            jumpToPrevToolStripMenuItem.Click += OnJumpToPrevToolStripMenuItemClick;
            // 
            // showBookmarkListToolStripMenuItem
            // 
            showBookmarkListToolStripMenuItem.BackColor = SystemColors.Control;
            showBookmarkListToolStripMenuItem.ForeColor = SystemColors.ControlDarkDark;
            showBookmarkListToolStripMenuItem.Name = "showBookmarkListToolStripMenuItem";
            showBookmarkListToolStripMenuItem.ShortcutKeys = Keys.F6;
            showBookmarkListToolStripMenuItem.Size = new Size(212, 22);
            showBookmarkListToolStripMenuItem.Text = "Bookmark list";
            showBookmarkListToolStripMenuItem.Click += OnShowBookmarkListToolStripMenuItemClick;
            // 
            // columnFinderToolStripMenuItem
            // 
            columnFinderToolStripMenuItem.CheckOnClick = true;
            columnFinderToolStripMenuItem.Name = "columnFinderToolStripMenuItem";
            columnFinderToolStripMenuItem.ShortcutKeys = Keys.F8;
            columnFinderToolStripMenuItem.Size = new Size(177, 22);
            columnFinderToolStripMenuItem.Text = "Column finder";
            columnFinderToolStripMenuItem.Click += OnColumnFinderToolStripMenuItemClick;
            // 
            // ToolStripSeparator5
            // 
            ToolStripSeparator5.Name = "ToolStripSeparator5";
            ToolStripSeparator5.Size = new Size(174, 6);
            // 
            // encodingToolStripMenuItem
            // 
            // Rows are added at runtime by EncodingMenuBuilder, from the single offered-encoding list.
            encodingToolStripMenuItem.Name = "encodingToolStripMenuItem";
            encodingToolStripMenuItem.Size = new Size(177, 22);
            encodingToolStripMenuItem.Text = "Encoding";
            // 
            // ToolStripSeparator6
            // 
            ToolStripSeparator6.Name = "ToolStripSeparator6";
            ToolStripSeparator6.Size = new Size(174, 6);
            // 
            // timeshiftToolStripMenuItem
            // 
            timeshiftToolStripMenuItem.CheckOnClick = true;
            timeshiftToolStripMenuItem.Name = "timeshiftToolStripMenuItem";
            timeshiftToolStripMenuItem.Size = new Size(177, 22);
            timeshiftToolStripMenuItem.Text = "Timeshift";
            timeshiftToolStripMenuItem.ToolTipText = "If supported by the columnizer, you can set an offset to the displayed log time";
            timeshiftToolStripMenuItem.CheckStateChanged += OnTimeShiftToolStripMenuItemCheckStateChanged;
            // 
            // timeshiftToolStripTextBox
            // 
            timeshiftToolStripTextBox.BorderStyle = BorderStyle.FixedSingle;
            timeshiftToolStripTextBox.Enabled = false;
            timeshiftToolStripTextBox.Name = "timeshiftToolStripTextBox";
            timeshiftToolStripTextBox.Size = new Size(100, 23);
            timeshiftToolStripTextBox.Text = "+00:00:00.000";
            timeshiftToolStripTextBox.ToolTipText = "Time offset (hh:mm:ss.fff)";
            timeshiftToolStripTextBox.KeyDown += OnTimeShiftMenuTextBoxKeyDown;
            // 
            // ToolStripSeparator4
            // 
            ToolStripSeparator4.Name = "ToolStripSeparator4";
            ToolStripSeparator4.Size = new Size(174, 6);
            // 
            // copyMarkedLinesIntoNewTabToolStripMenuItem
            // 
            copyMarkedLinesIntoNewTabToolStripMenuItem.Name = "copyMarkedLinesIntoNewTabToolStripMenuItem";
            copyMarkedLinesIntoNewTabToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.T;
            copyMarkedLinesIntoNewTabToolStripMenuItem.Size = new Size(177, 22);
            copyMarkedLinesIntoNewTabToolStripMenuItem.Text = "Copy to Tab";
            copyMarkedLinesIntoNewTabToolStripMenuItem.ToolTipText = "Copies all selected lines into a new tab page";
            copyMarkedLinesIntoNewTabToolStripMenuItem.Click += OnCopyMarkedLinesIntoNewTabToolStripMenuItemClick;
            // 
            // optionToolStripMenuItem
            // 
            optionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { columnizerToolStripMenuItem, hilightingToolStripMenuItem, ToolStripSeparator7, settingsToolStripMenuItem, pluginTrustManagementToolStripMenuItem, ToolStripSeparator9, cellSelectModeToolStripMenuItem, alwaysOnTopToolStripMenuItem, hideLineColumnToolStripMenuItem, ToolStripSeparator8, lockInstanceToolStripMenuItem });
            optionToolStripMenuItem.Name = "optionToolStripMenuItem";
            optionToolStripMenuItem.Size = new Size(61, 19);
            optionToolStripMenuItem.Text = "Options";
            optionToolStripMenuItem.DropDownOpening += OnOptionToolStripMenuItemDropDownOpening;
            // 
            // columnizerToolStripMenuItem
            // 
            columnizerToolStripMenuItem.Name = "columnizerToolStripMenuItem";
            columnizerToolStripMenuItem.Size = new Size(227, 30);
            columnizerToolStripMenuItem.Text = "Columnizer...";
            columnizerToolStripMenuItem.ToolTipText = "Splits various kinds of logfiles into fixed columns";
            columnizerToolStripMenuItem.Click += OnSelectFilterToolStripMenuItemClick;
            // 
            // hilightingToolStripMenuItem
            // 
            hilightingToolStripMenuItem.Name = "hilightingToolStripMenuItem";
            hilightingToolStripMenuItem.Size = new Size(227, 30);
            hilightingToolStripMenuItem.Text = "Highlighting and triggers...";
            hilightingToolStripMenuItem.Click += OnHighlightingToolStripMenuItemClick;
            // 
            // ToolStripSeparator7
            // 
            ToolStripSeparator7.Name = "ToolStripSeparator7";
            ToolStripSeparator7.Size = new Size(224, 6);
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Image = LogExpert.Resources.Settings;
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(227, 30);
            settingsToolStripMenuItem.Text = "Settings...";
            settingsToolStripMenuItem.Click += OnSettingsToolStripMenuItemClick;
            // 
            // toolStripMenuItemPluginTrustManagement
            // 
            pluginTrustManagementToolStripMenuItem.Name = "toolStripMenuItemPluginTrustManagement";
            pluginTrustManagementToolStripMenuItem.Size = new Size(227, 30);
            pluginTrustManagementToolStripMenuItem.Text = "Plugin &Trust Management...";
            pluginTrustManagementToolStripMenuItem.Click += OnPluginTrustToolStripMenuItemClick;
            // 
            // ToolStripSeparator9
            // 
            ToolStripSeparator9.Name = "ToolStripSeparator9";
            ToolStripSeparator9.Size = new Size(224, 6);
            // 
            // cellSelectModeToolStripMenuItem
            // 
            cellSelectModeToolStripMenuItem.CheckOnClick = true;
            cellSelectModeToolStripMenuItem.Name = "cellSelectModeToolStripMenuItem";
            cellSelectModeToolStripMenuItem.Size = new Size(227, 30);
            cellSelectModeToolStripMenuItem.Text = "Cell select mode";
            cellSelectModeToolStripMenuItem.ToolTipText = "Switches between foll row selection and single cell selection mode";
            cellSelectModeToolStripMenuItem.Click += OnCellSelectModeToolStripMenuItemClick;
            // 
            // alwaysOnTopToolStripMenuItem
            // 
            alwaysOnTopToolStripMenuItem.CheckOnClick = true;
            alwaysOnTopToolStripMenuItem.Name = "alwaysOnTopToolStripMenuItem";
            alwaysOnTopToolStripMenuItem.Size = new Size(227, 30);
            alwaysOnTopToolStripMenuItem.Text = "Always on top";
            alwaysOnTopToolStripMenuItem.Click += OnAlwaysOnTopToolStripMenuItemClick;
            // 
            // hideLineColumnToolStripMenuItem
            // 
            hideLineColumnToolStripMenuItem.CheckOnClick = true;
            hideLineColumnToolStripMenuItem.Name = "hideLineColumnToolStripMenuItem";
            hideLineColumnToolStripMenuItem.Size = new Size(227, 30);
            hideLineColumnToolStripMenuItem.Text = "Hide line column";
            hideLineColumnToolStripMenuItem.Click += OnHideLineColumnToolStripMenuItemClick;
            // 
            // ToolStripSeparator8
            // 
            ToolStripSeparator8.Name = "ToolStripSeparator8";
            ToolStripSeparator8.Size = new Size(224, 6);
            // 
            // lockInstanceToolStripMenuItem
            // 
            lockInstanceToolStripMenuItem.Name = "lockInstanceToolStripMenuItem";
            lockInstanceToolStripMenuItem.Size = new Size(227, 30);
            lockInstanceToolStripMenuItem.Text = "Lock instance";
            lockInstanceToolStripMenuItem.ToolTipText = "When enabled all new launched LogExpert instances will redirect to this window";
            lockInstanceToolStripMenuItem.Click += OnLockInstanceToolStripMenuItemClick;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { configureToolStripMenuItem, configureToolStripSeparator });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(46, 19);
            toolsToolStripMenuItem.Text = "Tools";
            toolsToolStripMenuItem.ToolTipText = "Launch external tools (configure in the settings)";
            toolsToolStripMenuItem.DropDownItemClicked += OnToolsToolStripMenuItemDropDownItemClicked;
            // 
            // configureToolStripMenuItem
            // 
            configureToolStripMenuItem.Name = "configureToolStripMenuItem";
            configureToolStripMenuItem.Size = new Size(136, 22);
            configureToolStripMenuItem.Text = "Configure...";
            configureToolStripMenuItem.Click += OnConfigureToolStripMenuItemClick;
            // 
            // configureToolStripSeparator
            // 
            configureToolStripSeparator.Name = "configureToolStripSeparator";
            configureToolStripSeparator.Size = new Size(133, 6);
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showHelpToolStripMenuItem, ToolStripSeparator11, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 19);
            helpToolStripMenuItem.Text = "Help";
            // 
            // showHelpToolStripMenuItem
            // 
            showHelpToolStripMenuItem.Name = "showHelpToolStripMenuItem";
            showHelpToolStripMenuItem.ShortcutKeys = Keys.F1;
            showHelpToolStripMenuItem.Size = new Size(148, 22);
            showHelpToolStripMenuItem.Text = "Show help";
            showHelpToolStripMenuItem.Click += OnShowHelpToolStripMenuItemClick;
            // 
            // ToolStripSeparator11
            // 
            ToolStripSeparator11.Name = "ToolStripSeparator11";
            ToolStripSeparator11.Size = new Size(145, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(148, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += OnAboutToolStripMenuItemClick;
            // 
            // debugToolStripMenuItem
            // 
            debugToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            debugToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { dumpLogBufferInfoToolStripMenuItem, dumpBufferDiagnosticToolStripMenuItem, runGCToolStripMenuItem, gCInfoToolStripMenuItem, throwExceptionGUIThreadToolStripMenuItem, throwExceptionbackgroundThToolStripMenuItem, throwExceptionBackgroundThreadToolStripMenuItem, loglevelToolStripMenuItem, disableWordHighlightModeToolStripMenuItem });
            debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            debugToolStripMenuItem.Size = new Size(54, 19);
            debugToolStripMenuItem.Text = "Debug";
            // 
            // dumpLogBufferInfoToolStripMenuItem
            // 
            dumpLogBufferInfoToolStripMenuItem.Name = "dumpLogBufferInfoToolStripMenuItem";
            dumpLogBufferInfoToolStripMenuItem.Size = new Size(274, 22);
            dumpLogBufferInfoToolStripMenuItem.Text = "Dump LogBuffer info";
            dumpLogBufferInfoToolStripMenuItem.Click += OnDumpLogBufferInfoToolStripMenuItemClick;
            // 
            // dumpBufferDiagnosticToolStripMenuItem
            // 
            dumpBufferDiagnosticToolStripMenuItem.Name = "dumpBufferDiagnosticToolStripMenuItem";
            dumpBufferDiagnosticToolStripMenuItem.Size = new Size(274, 22);
            dumpBufferDiagnosticToolStripMenuItem.Text = "Dump buffer diagnostic";
            dumpBufferDiagnosticToolStripMenuItem.Click += OnDumpBufferDiagnosticToolStripMenuItemClick;
            // 
            // runGCToolStripMenuItem
            // 
            runGCToolStripMenuItem.Name = "runGCToolStripMenuItem";
            runGCToolStripMenuItem.Size = new Size(274, 22);
            runGCToolStripMenuItem.Text = "Run GC";
            runGCToolStripMenuItem.Click += OnRunGCToolStripMenuItemClick;
            // 
            // gCInfoToolStripMenuItem
            // 
            gCInfoToolStripMenuItem.Name = "gCInfoToolStripMenuItem";
            gCInfoToolStripMenuItem.Size = new Size(274, 22);
            gCInfoToolStripMenuItem.Text = "Dump GC info";
            gCInfoToolStripMenuItem.Click += OnGCInfoToolStripMenuItemClick;
            // 
            // throwExceptionGUIThreadToolStripMenuItem
            // 
            throwExceptionGUIThreadToolStripMenuItem.Name = "throwExceptionGUIThreadToolStripMenuItem";
            throwExceptionGUIThreadToolStripMenuItem.Size = new Size(274, 22);
            throwExceptionGUIThreadToolStripMenuItem.Text = "Throw exception (GUI Thread)";
            throwExceptionGUIThreadToolStripMenuItem.Click += OnThrowExceptionGUIThreadToolStripMenuItemClick;
            // 
            // throwExceptionbackgroundThToolStripMenuItem
            // 
            throwExceptionbackgroundThToolStripMenuItem.Name = "throwExceptionbackgroundThToolStripMenuItem";
            throwExceptionbackgroundThToolStripMenuItem.Size = new Size(274, 22);
            throwExceptionbackgroundThToolStripMenuItem.Text = "Throw exception (Async delegate)";
            throwExceptionbackgroundThToolStripMenuItem.Click += OnThrowExceptionBackgroundThToolStripMenuItemClick;
            // 
            // throwExceptionBackgroundThreadToolStripMenuItem
            // 
            throwExceptionBackgroundThreadToolStripMenuItem.Name = "throwExceptionBackgroundThreadToolStripMenuItem";
            throwExceptionBackgroundThreadToolStripMenuItem.Size = new Size(274, 22);
            throwExceptionBackgroundThreadToolStripMenuItem.Text = "Throw exception (background thread)";
            throwExceptionBackgroundThreadToolStripMenuItem.Click += OnThrowExceptionBackgroundThreadToolStripMenuItemClick;
            // 
            // loglevelToolStripMenuItem
            // 
            loglevelToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { warnLogLevelToolStripMenuItem, infoLogLevelToolStripMenuItem, debugLogLevelToolStripMenuItem });
            loglevelToolStripMenuItem.Name = "loglevelToolStripMenuItem";
            loglevelToolStripMenuItem.Size = new Size(274, 22);
            loglevelToolStripMenuItem.Text = "Loglevel";
            loglevelToolStripMenuItem.DropDownOpening += OnLogLevelToolStripMenuItemDropDownOpening;
            loglevelToolStripMenuItem.Click += OnLogLevelToolStripMenuItemClick;
            // 
            // warnLogLevelToolStripMenuItem
            // 
            warnLogLevelToolStripMenuItem.Name = "warnLogLevelToolStripMenuItem";
            warnLogLevelToolStripMenuItem.Size = new Size(109, 22);
            warnLogLevelToolStripMenuItem.Text = "Warn";
            warnLogLevelToolStripMenuItem.Click += OnWarnToolStripMenuItemClick;
            // 
            // infoLogLevelToolStripMenuItem
            // 
            infoLogLevelToolStripMenuItem.Name = "infoLogLevelToolStripMenuItem";
            infoLogLevelToolStripMenuItem.Size = new Size(109, 22);
            infoLogLevelToolStripMenuItem.Text = "Info";
            infoLogLevelToolStripMenuItem.Click += OnInfoToolStripMenuItemClick;
            // 
            // debugLogLevelToolStripMenuItem1
            // 
            debugLogLevelToolStripMenuItem.Name = "debugLogLevelToolStripMenuItem1";
            debugLogLevelToolStripMenuItem.Size = new Size(109, 22);
            debugLogLevelToolStripMenuItem.Text = "Debug";
            debugLogLevelToolStripMenuItem.Click += OnDebugLogLevelToolStripMenuItemClick;
            // 
            // disableWordHighlightModeToolStripMenuItem
            // 
            disableWordHighlightModeToolStripMenuItem.CheckOnClick = true;
            disableWordHighlightModeToolStripMenuItem.Name = "disableWordHighlightModeToolStripMenuItem";
            disableWordHighlightModeToolStripMenuItem.Size = new Size(274, 22);
            disableWordHighlightModeToolStripMenuItem.Text = "Disable word highlight mode";
            disableWordHighlightModeToolStripMenuItem.Click += OnDisableWordHighlightModeToolStripMenuItemClick;
            // 
            // checkBoxHost
            // 
            checkBoxHost.AccessibleName = "host";
            checkBoxHost.AutoSize = true;
            checkBoxHost.BackColor = Color.Transparent;
            checkBoxHost.Location = new Point(9, 1);
            checkBoxHost.Name = "checkBoxHost";
            checkBoxHost.Size = new Size(80, 22);
            checkBoxHost.TabIndex = 7;
            checkBoxHost.Text = "Follow tail";
            checkBoxHost.UseVisualStyleBackColor = false;
            // 
            // toolStripContainer
            // 
            // 
            // toolStripContainer.BottomToolStripPanel
            // 
            toolStripContainer.BottomToolStripPanel.RenderMode = ToolStripRenderMode.System;
            toolStripContainer.BottomToolStripPanelVisible = false;
            // 
            // toolStripContainer.ContentPanel
            // 
            toolStripContainer.ContentPanel.Controls.Add(dockPanel);
            toolStripContainer.ContentPanel.Margin = new Padding(0);
            toolStripContainer.ContentPanel.Size = new Size(1603, 928);
            toolStripContainer.Dock = DockStyle.Fill;
            // 
            // toolStripContainer.LeftToolStripPanel
            // 
            toolStripContainer.LeftToolStripPanel.Enabled = false;
            toolStripContainer.LeftToolStripPanelVisible = false;
            toolStripContainer.Location = new Point(0, 0);
            toolStripContainer.Margin = new Padding(0);
            toolStripContainer.Name = "toolStripContainer";
            // 
            // toolStripContainer.RightToolStripPanel
            // 
            toolStripContainer.RightToolStripPanel.Enabled = false;
            toolStripContainer.RightToolStripPanelVisible = false;
            toolStripContainer.Size = new Size(1603, 982);
            toolStripContainer.TabIndex = 13;
            toolStripContainer.Text = "toolStripContainer1";
            // 
            // toolStripContainer.TopToolStripPanel
            // 
            toolStripContainer.TopToolStripPanel.Controls.Add(buttonToolStrip);
            toolStripContainer.TopToolStripPanel.Controls.Add(externalToolsToolStrip);
            toolStripContainer.TopToolStripPanel.Controls.Add(mainMenuStrip);
            // 
            // dockPanel
            // 
            dockPanel.DefaultFloatWindowSize = new Size(600, 400);
            dockPanel.Dock = DockStyle.Fill;
            dockPanel.DockBackColor = Color.FromArgb(238, 238, 242);
            dockPanel.Location = new Point(0, 0);
            dockPanel.Margin = new Padding(0);
            dockPanel.Name = "dockPanel";
            dockPanel.ShowAutoHideContentOnHover = false;
            dockPanel.ShowDocumentIcon = true;
            dockPanel.Size = new Size(1603, 928);
            dockPanel.TabIndex = 14;
            dockPanel.ActiveContentChanged += OnDockPanelActiveContentChanged;
            // 
            // externalToolsToolStrip
            // 
            externalToolsToolStrip.AllowMerge = false;
            externalToolsToolStrip.Dock = DockStyle.None;
            externalToolsToolStrip.ImageScalingSize = new Size(24, 24);
            externalToolsToolStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            externalToolsToolStrip.Location = new Point(3, 0);
            externalToolsToolStrip.Name = "externalToolsToolStrip";
            externalToolsToolStrip.Size = new Size(1, 0);
            externalToolsToolStrip.TabIndex = 8;
            externalToolsToolStrip.ItemClicked += OnExternalToolsToolStripItemClicked;
            // 
            // buttonToolStrip
            // 
            buttonToolStrip.AllowMerge = false;
            buttonToolStrip.Dock = DockStyle.None;
            buttonToolStrip.ImageScalingSize = new Size(24, 24);
            buttonToolStrip.Items.AddRange(new ToolStripItem[] { toolStripButtonOpen, lineToolStripSeparatorExtension1, toolStripButtonSearch, toolStripButtonFilter, lineToolStripSeparatorExtension2, toolStripButtonBookmark, toolStripButtonUp, toolStripButtonDown, lineToolStripSeparatorExtension3, toolStripButtonBubbles, lineToolStripSeparatorExtension4, toolStripButtonTail, lineToolStripSeparatorExtension5, highlightGroupsToolStripComboBox });
            buttonToolStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            buttonToolStrip.Location = new Point(4, 0);
            buttonToolStrip.Name = "buttonToolStrip";
            buttonToolStrip.Size = new Size(406, 31);
            buttonToolStrip.TabIndex = 7;
            // 
            // toolStripButtonOpen
            // 
            toolStripButtonOpen.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonOpen.Image = LogExpert.Resources.File_open;
            toolStripButtonOpen.ImageTransparentColor = Color.Magenta;
            toolStripButtonOpen.Name = "toolStripButtonOpen";
            toolStripButtonOpen.Size = new Size(28, 28);
            toolStripButtonOpen.Text = "Open File";
            toolStripButtonOpen.ToolTipText = "Open file";
            toolStripButtonOpen.Click += OnToolStripButtonOpenClick;
            // 
            // lineToolStripSeparatorExtension1
            // 
            lineToolStripSeparatorExtension1.Name = "lineToolStripSeparatorExtension1";
            lineToolStripSeparatorExtension1.Size = new Size(6, 23);
            // 
            // toolStripButtonSearch
            // 
            toolStripButtonSearch.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonSearch.Image = LogExpert.Resources.Search;
            toolStripButtonSearch.ImageTransparentColor = Color.Magenta;
            toolStripButtonSearch.Name = "toolStripButtonSearch";
            toolStripButtonSearch.Size = new Size(28, 28);
            toolStripButtonSearch.Text = "Search";
            toolStripButtonSearch.ToolTipText = "Search";
            toolStripButtonSearch.Click += OnToolStripButtonSearchClick;
            // 
            // toolStripButtonFilter
            // 
            toolStripButtonFilter.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonFilter.Image = LogExpert.Resources.Filter;
            toolStripButtonFilter.ImageTransparentColor = Color.Magenta;
            toolStripButtonFilter.Name = "toolStripButtonFilter";
            toolStripButtonFilter.Size = new Size(28, 28);
            toolStripButtonFilter.Text = "Filter";
            toolStripButtonFilter.ToolTipText = "Filter window";
            toolStripButtonFilter.Click += OnToolStripButtonFilterClick;
            // 
            // lineToolStripSeparatorExtension2
            // 
            lineToolStripSeparatorExtension2.ForeColor = SystemColors.ControlDarkDark;
            lineToolStripSeparatorExtension2.Name = "lineToolStripSeparatorExtension2";
            lineToolStripSeparatorExtension2.Size = new Size(6, 23);
            // 
            // toolStripButtonBookmark
            // 
            toolStripButtonBookmark.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonBookmark.Image = LogExpert.Resources.Bookmark_add;
            toolStripButtonBookmark.ImageTransparentColor = Color.Magenta;
            toolStripButtonBookmark.Name = "toolStripButtonBookmark";
            toolStripButtonBookmark.Size = new Size(28, 28);
            toolStripButtonBookmark.Text = "Toggle Bookmark";
            toolStripButtonBookmark.ToolTipText = "Toggle bookmark";
            toolStripButtonBookmark.Click += OnToolStripButtonBookmarkClick;
            // 
            // toolStripButtonUp
            // 
            toolStripButtonUp.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonUp.Image = LogExpert.Resources.ArrowUp;
            toolStripButtonUp.ImageTransparentColor = Color.Magenta;
            toolStripButtonUp.Name = "toolStripButtonUp";
            toolStripButtonUp.Size = new Size(28, 28);
            toolStripButtonUp.Text = "Previous Bookmark";
            toolStripButtonUp.ToolTipText = "Go to previous bookmark";
            toolStripButtonUp.Click += OnToolStripButtonUpClick;
            // 
            // toolStripButtonDown
            // 
            toolStripButtonDown.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonDown.Image = LogExpert.Resources.ArrowDown;
            toolStripButtonDown.ImageTransparentColor = Color.Magenta;
            toolStripButtonDown.Name = "toolStripButtonDown";
            toolStripButtonDown.Size = new Size(28, 28);
            toolStripButtonDown.Text = "Next Bookmark";
            toolStripButtonDown.ToolTipText = "Go to next bookmark";
            toolStripButtonDown.Click += OnToolStripButtonDownClick;
            // 
            // lineToolStripSeparatorExtension3
            // 
            lineToolStripSeparatorExtension3.Name = "lineToolStripSeparatorExtension3";
            lineToolStripSeparatorExtension3.Size = new Size(6, 23);
            // 
            // toolStripButtonBubbles
            // 
            toolStripButtonBubbles.CheckOnClick = true;
            toolStripButtonBubbles.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonBubbles.Image = LogExpert.Resources.bookmark_bubbles;
            toolStripButtonBubbles.ImageAlign = ContentAlignment.BottomCenter;
            toolStripButtonBubbles.ImageTransparentColor = Color.Magenta;
            toolStripButtonBubbles.Name = "toolStripButtonBubbles";
            toolStripButtonBubbles.Size = new Size(28, 28);
            toolStripButtonBubbles.Text = "Show bookmark bubbles";
            toolStripButtonBubbles.Click += OnToolStripButtonBubblesClick;
            // 
            // lineToolStripSeparatorExtension4
            // 
            lineToolStripSeparatorExtension4.Name = "lineToolStripSeparatorExtension4";
            lineToolStripSeparatorExtension4.Size = new Size(6, 23);
            // 
            // toolStripButtonTail
            // 
            toolStripButtonTail.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonTail.Image = (Image)resources.GetObject("toolStripButtonTail.Image");
            toolStripButtonTail.ImageTransparentColor = Color.Magenta;
            toolStripButtonTail.Name = "toolStripButtonTail";
            toolStripButtonTail.Size = new Size(27, 19);
            toolStripButtonTail.Text = "tail";
            // 
            // lineToolStripSeparatorExtension5
            // 
            lineToolStripSeparatorExtension5.Name = "lineToolStripSeparatorExtension5";
            lineToolStripSeparatorExtension5.Size = new Size(6, 23);
            // 
            // highlightGroupsToolStripComboBox
            // 
            highlightGroupsToolStripComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            highlightGroupsToolStripComboBox.DropDownWidth = 250;
            highlightGroupsToolStripComboBox.FlatStyle = FlatStyle.Standard;
            highlightGroupsToolStripComboBox.Name = "highlightGroupsToolStripComboBox";
            highlightGroupsToolStripComboBox.Size = new Size(150, 23);
            highlightGroupsToolStripComboBox.ToolTipText = "Select the current highlight settings for the log file (right-click to open highlight settings)";
            highlightGroupsToolStripComboBox.DropDownClosed += OnHighlightGroupsComboBoxDropDownClosed;
            highlightGroupsToolStripComboBox.MouseUp += OnHighlightGroupsComboBoxMouseUp;
            // 
            // checkBoxFollowTail
            // 
            checkBoxFollowTail.AutoSize = true;
            checkBoxFollowTail.Location = new Point(663, 985);
            checkBoxFollowTail.Margin = new Padding(4, 7, 4, 7);
            checkBoxFollowTail.Name = "checkBoxFollowTail";
            checkBoxFollowTail.Size = new Size(80, 19);
            checkBoxFollowTail.TabIndex = 14;
            checkBoxFollowTail.Text = "Follow tail";
            checkBoxFollowTail.UseVisualStyleBackColor = true;
            checkBoxFollowTail.Click += OnFollowTailCheckBoxClick;
            // 
            // tabContextMenuStrip
            // 
            tabContextMenuStrip.ForeColor = SystemColors.ControlText;
            tabContextMenuStrip.ImageScalingSize = new Size(24, 24);
            tabContextMenuStrip.Items.AddRange(new ToolStripItem[] { closeThisTabToolStripMenuItem, closeOtherTabsToolStripMenuItem, closeAllTabsToolStripMenuItem, tabColorToolStripMenuItem, tabRenameToolStripMenuItem, copyPathToClipboardToolStripMenuItem, findInExplorerToolStripMenuItem, truncateFileToolStripMenuItem });
            tabContextMenuStrip.Name = "tabContextMenuStrip";
            tabContextMenuStrip.Size = new Size(197, 180);
            // 
            // closeThisTabToolStripMenuItem
            // 
            closeThisTabToolStripMenuItem.BackColor = SystemColors.Control;
            closeThisTabToolStripMenuItem.ForeColor = SystemColors.ControlText;
            closeThisTabToolStripMenuItem.Name = "closeThisTabToolStripMenuItem";
            closeThisTabToolStripMenuItem.Size = new Size(196, 22);
            closeThisTabToolStripMenuItem.Text = "Close this tab";
            closeThisTabToolStripMenuItem.Click += OnCloseThisTabToolStripMenuItemClick;
            // 
            // closeOtherTabsToolStripMenuItem
            // 
            closeOtherTabsToolStripMenuItem.Name = "closeOtherTabsToolStripMenuItem";
            closeOtherTabsToolStripMenuItem.Size = new Size(196, 22);
            closeOtherTabsToolStripMenuItem.Text = "Close other tabs";
            closeOtherTabsToolStripMenuItem.ToolTipText = "Close all tabs except of this one";
            closeOtherTabsToolStripMenuItem.Click += OnCloseOtherTabsToolStripMenuItemClick;
            // 
            // closeAllTabsToolStripMenuItem
            // 
            closeAllTabsToolStripMenuItem.Name = "closeAllTabsToolStripMenuItem";
            closeAllTabsToolStripMenuItem.Size = new Size(196, 22);
            closeAllTabsToolStripMenuItem.Text = "Close all tabs";
            closeAllTabsToolStripMenuItem.ToolTipText = "Close all tabs";
            closeAllTabsToolStripMenuItem.Click += OnCloseAllTabsToolStripMenuItemClick;
            // 
            // tabColorToolStripMenuItem
            // 
            tabColorToolStripMenuItem.Name = "tabColorToolStripMenuItem";
            tabColorToolStripMenuItem.Size = new Size(196, 22);
            tabColorToolStripMenuItem.Text = "Tab color...";
            tabColorToolStripMenuItem.ToolTipText = "Sets the tab color";
            tabColorToolStripMenuItem.Click += OnTabColorToolStripMenuItemClick;
            // 
            // tabRenameToolStripMenuItem
            // 
            tabRenameToolStripMenuItem.Name = "tabRenameToolStripMenuItem";
            tabRenameToolStripMenuItem.Size = new Size(196, 22);
            tabRenameToolStripMenuItem.Text = "Tab rename...";
            tabRenameToolStripMenuItem.ToolTipText = "Set the text which is shown on the tab";
            tabRenameToolStripMenuItem.Click += OnTabRenameToolStripMenuItemClick;
            // 
            // copyPathToClipboardToolStripMenuItem
            // 
            copyPathToClipboardToolStripMenuItem.Name = "copyPathToClipboardToolStripMenuItem";
            copyPathToClipboardToolStripMenuItem.Size = new Size(196, 22);
            copyPathToClipboardToolStripMenuItem.Text = "Copy path to clipboard";
            copyPathToClipboardToolStripMenuItem.ToolTipText = "The complete file name (incl. path) is copied to clipboard";
            copyPathToClipboardToolStripMenuItem.Click += OnCopyPathToClipboardToolStripMenuItemClick;
            // 
            // findInExplorerToolStripMenuItem
            // 
            findInExplorerToolStripMenuItem.Name = "findInExplorerToolStripMenuItem";
            findInExplorerToolStripMenuItem.Size = new Size(196, 22);
            findInExplorerToolStripMenuItem.Text = "Find in Explorer";
            findInExplorerToolStripMenuItem.ToolTipText = "Opens an Explorer window and selects the log file";
            findInExplorerToolStripMenuItem.Click += OnFindInExplorerToolStripMenuItemClick;
            // 
            // truncateFileToolStripMenuItem
            // 
            truncateFileToolStripMenuItem.Name = "truncateFileToolStripMenuItem";
            truncateFileToolStripMenuItem.Size = new Size(196, 22);
            truncateFileToolStripMenuItem.Text = "Truncate File";
            truncateFileToolStripMenuItem.ToolTipText = "Try to truncate the file opened in tab";
            truncateFileToolStripMenuItem.Click += TruncateFileToolStripMenuItem_Click;
            // 
            // dragControlDateTime
            // 
            dragControlDateTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            dragControlDateTime.BackColor = SystemColors.Control;
            dragControlDateTime.DateTime = new DateTime(0L);
            dragControlDateTime.DragOrientation = DragOrientations.Vertical;
            dragControlDateTime.Font = new Font("Courier New", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dragControlDateTime.ForeColor = SystemColors.ControlDarkDark;
            dragControlDateTime.HoverColor = Color.LightGray;
            dragControlDateTime.Location = new Point(1017, 977);
            dragControlDateTime.Margin = new Padding(0);
            dragControlDateTime.MaxDateTime = new DateTime(9999, 12, 31, 23, 59, 59, 999);
            dragControlDateTime.MinDateTime = new DateTime(0L);
            dragControlDateTime.Name = "dragControlDateTime";
            dragControlDateTime.Size = new Size(313, 38);
            dragControlDateTime.TabIndex = 14;
            dragControlDateTime.ValueChanged += OnDateTimeDragControlValueChanged;
            dragControlDateTime.ValueDragged += OnDateTimeDragControlValueDragged;
            // 
            // LogTabWindow
            // 
            AllowDrop = true;
            ClientSize = new Size(1603, 1017);
            Controls.Add(checkBoxFollowTail);
            Controls.Add(dragControlDateTime);
            Controls.Add(toolStripContainer);
            Controls.Add(statusStrip);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MainMenuStrip = mainMenuStrip;
            Margin = new Padding(4, 7, 4, 7);
            Name = "LogTabWindow";
            Text = "LogExpert";
            Activated += OnLogTabWindowActivated;
            Deactivate += OnLogTabWindowDeactivate;
            SizeChanged += OnLogTabWindowSizeChanged;
            DragDrop += OnLogWindowDragDrop;
            DragEnter += OnLogTabWindowDragEnter;
            DragOver += OnLogWindowDragOver;
            KeyDown += OnLogTabWindowKeyDown;
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            toolStripContainer.ContentPanel.ResumeLayout(false);
            toolStripContainer.TopToolStripPanel.ResumeLayout(false);
            toolStripContainer.TopToolStripPanel.PerformLayout();
            toolStripContainer.ResumeLayout(false);
            toolStripContainer.PerformLayout();
            buttonToolStrip.ResumeLayout(false);
            buttonToolStrip.PerformLayout();
            tabContextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel labelLines;
        private System.Windows.Forms.ToolStripStatusLabel labelSize;
        private System.Windows.Forms.MenuStrip mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewNavigateToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel labelCurrentLine;
        private System.Windows.Forms.ToolStripProgressBar loadProgessBar;
        private System.Windows.Forms.ToolStripStatusLabel labelStatus;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripContainer toolStripContainer;
        private System.Windows.Forms.ToolStripMenuItem closeFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem multiFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem goToLineToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem timeshiftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyMarkedLinesIntoNewTabToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hilightingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cellSelectModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox timeshiftToolStripTextBox;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnTopToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bookmarksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggleBookmarkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem jumpToNextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem jumpToPrevToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem encodingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem columnizerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private DateTimeDragControl dragControlDateTime;
        private System.Windows.Forms.ToolStripMenuItem showBookmarkListToolStripMenuItem;
        private System.Windows.Forms.ToolStrip buttonToolStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonOpen;
        private System.Windows.Forms.ToolStripButton toolStripButtonSearch;
        private System.Windows.Forms.ToolStripButton toolStripButtonFilter;
        private System.Windows.Forms.ToolStripButton toolStripButtonBookmark;
        private System.Windows.Forms.ToolStripButton toolStripButtonUp;
        private System.Windows.Forms.ToolStripButton toolStripButtonDown;
        private CheckBox checkBoxHost;
        private CheckBox checkBoxFollowTail;
        private ToolStripButton toolStripButtonTail;
        private ToolStripMenuItem showHelpToolStripMenuItem;
        private ToolStripMenuItem hideLineColumnToolStripMenuItem;
        private ToolStripMenuItem lastUsedToolStripMenuItem;
        private ContextMenuStrip tabContextMenuStrip;
        private ToolStripMenuItem closeThisTabToolStripMenuItem;
        private ToolStripMenuItem closeOtherTabsToolStripMenuItem;
        private ToolStripMenuItem closeAllTabsToolStripMenuItem;
        private ToolStripMenuItem tabColorToolStripMenuItem;
        private ToolStripMenuItem loadSessionToolStripMenuItem;
        private ToolStripMenuItem saveSessionToolStripMenuItem;
        private ToolStripButton toolStripButtonBubbles;
        private ToolStripMenuItem copyPathToClipboardToolStripMenuItem;
        private ToolStripMenuItem findInExplorerToolStripMenuItem;
        private ToolStripMenuItem truncateFileToolStripMenuItem;
        private ToolStripMenuItem exportBookmarksToolStripMenuItem;
        private ToolStripComboBox highlightGroupsToolStripComboBox;
        private ToolStripMenuItem debugToolStripMenuItem;
        private ToolStripMenuItem dumpLogBufferInfoToolStripMenuItem;
        private ToolStripMenuItem dumpBufferDiagnosticToolStripMenuItem;
        private ToolStripMenuItem runGCToolStripMenuItem;
        private ToolStripMenuItem gCInfoToolStripMenuItem;
        private ToolStrip externalToolsToolStrip;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem configureToolStripMenuItem;
        private ToolStripMenuItem throwExceptionGUIThreadToolStripMenuItem;
        private ToolStripMenuItem throwExceptionbackgroundThToolStripMenuItem;
        private ToolStripMenuItem throwExceptionBackgroundThreadToolStripMenuItem;
        private ToolStripMenuItem loglevelToolStripMenuItem;
        private ToolStripMenuItem warnLogLevelToolStripMenuItem;
        private ToolStripMenuItem infoLogLevelToolStripMenuItem;
        private ToolStripMenuItem debugLogLevelToolStripMenuItem;
        private ToolStripMenuItem disableWordHighlightModeToolStripMenuItem;
        private ToolStripMenuItem multifileMaskToolStripMenuItem;
        private ToolStripMenuItem multiFileEnabledStripMenuItem;
        private ToolStripMenuItem lockInstanceToolStripMenuItem;
        private ToolStripMenuItem newFromClipboardToolStripMenuItem;
        private ToolStripMenuItem openURIToolStripMenuItem;
        private ToolStripMenuItem columnFinderToolStripMenuItem;
        private DockPanel dockPanel;
        private ToolStripMenuItem tabRenameToolStripMenuItem;
        private ToolStripSeparator lineToolStripSeparatorExtension1;
        private ToolStripSeparator lineToolStripSeparatorExtension2;
        private ToolStripSeparator ToolStripSeparator1;
        private ToolStripSeparator ToolStripSeparator2;
        private ToolStripSeparator ToolStripSeparator3;
        private ToolStripSeparator lineToolStripSeparatorExtension3;
        private ToolStripSeparator lineToolStripSeparatorExtension4;
        private ToolStripSeparator lineToolStripSeparatorExtension5;
        private ToolStripSeparator ToolStripSeparator5;
        private ToolStripSeparator ToolStripSeparator6;
        private ToolStripSeparator ToolStripSeparator4;
        private ToolStripSeparator ToolStripSeparator7;
        private ToolStripSeparator ToolStripSeparator9;
        private ToolStripSeparator ToolStripSeparator8;
        private ToolStripSeparator configureToolStripSeparator;
        private ToolStripSeparator ToolStripSeparator11;
        private ToolStripMenuItem pluginTrustManagementToolStripMenuItem;
    }
}

