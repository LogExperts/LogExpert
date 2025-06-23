using System.Windows.Forms;
using LogExpert.Core.Enums;
using LogExpert.Dialogs;
using LogExpert.UI.Extensions.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Controls.LogTabWindow
{
	partial class LogTabWindow
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            AutoHideStripSkin autoHideStripSkin1 = new AutoHideStripSkin();
            DockPanelGradient dockPanelGradient1 = new DockPanelGradient();
            TabGradient tabGradient1 = new TabGradient();
            DockPaneStripSkin dockPaneStripSkin1 = new DockPaneStripSkin();
            DockPaneStripGradient dockPaneStripGradient1 = new DockPaneStripGradient();
            TabGradient tabGradient2 = new TabGradient();
            DockPanelGradient dockPanelGradient2 = new DockPanelGradient();
            TabGradient tabGradient3 = new TabGradient();
            DockPaneStripToolWindowGradient dockPaneStripToolWindowGradient1 = new DockPaneStripToolWindowGradient();
            TabGradient tabGradient4 = new TabGradient();
            TabGradient tabGradient5 = new TabGradient();
            DockPanelGradient dockPanelGradient3 = new DockPanelGradient();
            TabGradient tabGradient6 = new TabGradient();
            TabGradient tabGradient7 = new TabGradient();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogTabWindow));
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
            menuToolStripSeparatorExtension1 = new MenuToolStripSeparatorExtension();
            multiFileToolStripMenuItem = new ToolStripMenuItem();
            multiFileEnabledStripMenuItem = new ToolStripMenuItem();
            multifileMaskToolStripMenuItem = new ToolStripMenuItem();
            menuToolStripSeparatorExtension2 = new MenuToolStripSeparatorExtension();
            loadProjectToolStripMenuItem = new ToolStripMenuItem();
            saveProjectToolStripMenuItem = new ToolStripMenuItem();
            exportBookmarksToolStripMenuItem = new ToolStripMenuItem();
            menuToolStripSeparatorExtension3 = new MenuToolStripSeparatorExtension();
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
            menuToolStripSeparatorExtension5 = new MenuToolStripSeparatorExtension();
            toolStripEncodingMenuItem = new ToolStripMenuItem();
            toolStripEncodingASCIIItem = new ToolStripMenuItem();
            toolStripEncodingANSIItem = new ToolStripMenuItem();
            toolStripEncodingISO88591Item = new ToolStripMenuItem();
            toolStripEncodingUTF8Item = new ToolStripMenuItem();
            toolStripEncodingUTF16Item = new ToolStripMenuItem();
            menuToolStripSeparatorExtension6 = new MenuToolStripSeparatorExtension();
            timeshiftToolStripMenuItem = new ToolStripMenuItem();
            timeshiftMenuTextBox = new ToolStripTextBox();
            menuToolStripSeparatorExtension4 = new MenuToolStripSeparatorExtension();
            copyMarkedLinesIntoNewTabToolStripMenuItem = new ToolStripMenuItem();
            optionToolStripMenuItem = new ToolStripMenuItem();
            columnizerToolStripMenuItem = new ToolStripMenuItem();
            hilightingToolStripMenuItem1 = new ToolStripMenuItem();
            menuToolStripSeparatorExtension7 = new MenuToolStripSeparatorExtension();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            menuToolStripSeparatorExtension9 = new MenuToolStripSeparatorExtension();
            cellSelectModeToolStripMenuItem = new ToolStripMenuItem();
            alwaysOnTopToolStripMenuItem = new ToolStripMenuItem();
            hideLineColumnToolStripMenuItem = new ToolStripMenuItem();
            menuToolStripSeparatorExtension8 = new MenuToolStripSeparatorExtension();
            lockInstanceToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            configureToolStripMenuItem = new ToolStripMenuItem();
            configureToolStripSeparator = new MenuToolStripSeparatorExtension();
            helpToolStripMenuItem = new ToolStripMenuItem();
            showHelpToolStripMenuItem = new ToolStripMenuItem();
            menuToolStripSeparatorExtension11 = new MenuToolStripSeparatorExtension();
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
            warnToolStripMenuItem = new ToolStripMenuItem();
            infoToolStripMenuItem = new ToolStripMenuItem();
            debugToolStripMenuItem1 = new ToolStripMenuItem();
            disableWordHighlightModeToolStripMenuItem = new ToolStripMenuItem();
            host = new CheckBox();
            toolStripContainer = new ToolStripContainer();
            dockPanel = new DockPanel();
            externalToolsToolStrip = new ToolStrip();
            buttonToolStrip = new ToolStrip();
            toolStripButtonOpen = new ToolStripButton();
            lineToolStripSeparatorExtension1 = new LineToolStripSeparatorExtension();
            toolStripButtonSearch = new ToolStripButton();
            toolStripButtonFilter = new ToolStripButton();
            lineToolStripSeparatorExtension2 = new LineToolStripSeparatorExtension();
            toolStripButtonBookmark = new ToolStripButton();
            toolStripButtonUp = new ToolStripButton();
            toolStripButtonDown = new ToolStripButton();
            lineToolStripSeparatorExtension3 = new LineToolStripSeparatorExtension();
            toolStripButtonBubbles = new ToolStripButton();
            lineToolStripSeparatorExtension4 = new LineToolStripSeparatorExtension();
            toolStripButtonTail = new ToolStripButton();
            lineToolStripSeparatorExtension5 = new LineToolStripSeparatorExtension();
            groupsComboBoxHighlightGroups = new ToolStripComboBox();
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
            statusStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            statusStrip.Items.AddRange(new ToolStripItem[] { labelLines, labelSize, labelCurrentLine, loadProgessBar, labelStatus });
            statusStrip.Location = new System.Drawing.Point(0, 954);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(3, 0, 23, 0);
            statusStrip.Size = new System.Drawing.Size(1603, 63);
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
            labelLines.Size = new System.Drawing.Size(26, 58);
            labelLines.Text = "0";
            // 
            // labelSize
            // 
            labelSize.AutoSize = false;
            labelSize.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            labelSize.BorderStyle = Border3DStyle.SunkenOuter;
            labelSize.Name = "labelSize";
            labelSize.Size = new System.Drawing.Size(26, 58);
            labelSize.Text = "0";
            // 
            // labelCurrentLine
            // 
            labelCurrentLine.AutoSize = false;
            labelCurrentLine.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            labelCurrentLine.BorderStyle = Border3DStyle.SunkenOuter;
            labelCurrentLine.Name = "labelCurrentLine";
            labelCurrentLine.Size = new System.Drawing.Size(28, 58);
            labelCurrentLine.Text = "L:";
            // 
            // loadProgessBar
            // 
            loadProgessBar.Name = "loadProgessBar";
            loadProgessBar.Size = new System.Drawing.Size(83, 57);
            // 
            // labelStatus
            // 
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new System.Drawing.Size(39, 58);
            labelStatus.Text = "Ready";
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.AllowMerge = false;
            mainMenuStrip.Dock = DockStyle.None;
            mainMenuStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewNavigateToolStripMenuItem, optionToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem, debugToolStripMenuItem });
            mainMenuStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            mainMenuStrip.Location = new System.Drawing.Point(0, 19);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new System.Drawing.Size(1603, 23);
            mainMenuStrip.TabIndex = 6;
            mainMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, openURIToolStripMenuItem, closeFileToolStripMenuItem, reloadToolStripMenuItem, newFromClipboardToolStripMenuItem, menuToolStripSeparatorExtension1, multiFileToolStripMenuItem, menuToolStripSeparatorExtension2, loadProjectToolStripMenuItem, saveProjectToolStripMenuItem, exportBookmarksToolStripMenuItem, menuToolStripSeparatorExtension3, lastUsedToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 19);
            fileToolStripMenuItem.Text = "File";
            fileToolStripMenuItem.DropDownOpening += OnFileToolStripMenuItemDropDownOpening;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            openToolStripMenuItem.Image = LogExpert.Resources.File_open;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            openToolStripMenuItem.Text = "Open...";
            openToolStripMenuItem.Click += OnOpenToolStripMenuItemClick;
            // 
            // openURIToolStripMenuItem
            // 
            openURIToolStripMenuItem.Name = "openURIToolStripMenuItem";
            openURIToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.U;
            openURIToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            openURIToolStripMenuItem.Text = "Open URL...";
            openURIToolStripMenuItem.ToolTipText = "Opens a file by entering a URL which is supported by a file system plugin";
            openURIToolStripMenuItem.Click += OnOpenURIToolStripMenuItemClick;
            // 
            // closeFileToolStripMenuItem
            // 
            closeFileToolStripMenuItem.Image = LogExpert.Resources.Close;
            closeFileToolStripMenuItem.Name = "closeFileToolStripMenuItem";
            closeFileToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F4;
            closeFileToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            closeFileToolStripMenuItem.Text = "Close File";
            closeFileToolStripMenuItem.Click += OnCloseFileToolStripMenuItemClick;
            // 
            // reloadToolStripMenuItem
            // 
            reloadToolStripMenuItem.Image = LogExpert.Resources.Restart_alt;
            reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            reloadToolStripMenuItem.ShortcutKeys = Keys.F5;
            reloadToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            reloadToolStripMenuItem.Text = "Reload";
            reloadToolStripMenuItem.Click += OnReloadToolStripMenuItemClick;
            // 
            // newFromClipboardToolStripMenuItem
            // 
            newFromClipboardToolStripMenuItem.Name = "newFromClipboardToolStripMenuItem";
            newFromClipboardToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newFromClipboardToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            newFromClipboardToolStripMenuItem.Text = "New tab from clipboard";
            newFromClipboardToolStripMenuItem.ToolTipText = "Creates a new tab with content from clipboard";
            newFromClipboardToolStripMenuItem.Click += OnNewFromClipboardToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension1
            // 
            menuToolStripSeparatorExtension1.Name = "menuToolStripSeparatorExtension1";
            menuToolStripSeparatorExtension1.Size = new System.Drawing.Size(250, 6);
            // 
            // multiFileToolStripMenuItem
            // 
            multiFileToolStripMenuItem.CheckOnClick = true;
            multiFileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { multiFileEnabledStripMenuItem, multifileMaskToolStripMenuItem });
            multiFileToolStripMenuItem.Name = "multiFileToolStripMenuItem";
            multiFileToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            multiFileToolStripMenuItem.Text = "MultiFile";
            multiFileToolStripMenuItem.ToolTipText = "Treat multiple files as one large file (e.g. data.log, data.log.1, data.log.2,...)";
            multiFileToolStripMenuItem.Click += OnMultiFileToolStripMenuItemClick;
            // 
            // multiFileEnabledStripMenuItem
            // 
            multiFileEnabledStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            multiFileEnabledStripMenuItem.CheckOnClick = true;
            multiFileEnabledStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            multiFileEnabledStripMenuItem.Name = "multiFileEnabledStripMenuItem";
            multiFileEnabledStripMenuItem.Size = new System.Drawing.Size(165, 22);
            multiFileEnabledStripMenuItem.Text = "Enable MultiFile";
            multiFileEnabledStripMenuItem.Click += OnMultiFileEnabledStripMenuItemClick;
            // 
            // multifileMaskToolStripMenuItem
            // 
            multifileMaskToolStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            multifileMaskToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            multifileMaskToolStripMenuItem.Name = "multifileMaskToolStripMenuItem";
            multifileMaskToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            multifileMaskToolStripMenuItem.Text = "File name mask...";
            multifileMaskToolStripMenuItem.Click += OnMultiFileMaskToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension2
            // 
            menuToolStripSeparatorExtension2.Name = "menuToolStripSeparatorExtension2";
            menuToolStripSeparatorExtension2.Size = new System.Drawing.Size(250, 6);
            // 
            // loadProjectToolStripMenuItem
            // 
            loadProjectToolStripMenuItem.Name = "loadProjectToolStripMenuItem";
            loadProjectToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            loadProjectToolStripMenuItem.Text = "Load session...";
            loadProjectToolStripMenuItem.ToolTipText = "Load a saved session (list of log files)";
            loadProjectToolStripMenuItem.Click += OnLoadProjectToolStripMenuItemClick;
            // 
            // saveProjectToolStripMenuItem
            // 
            saveProjectToolStripMenuItem.Name = "saveProjectToolStripMenuItem";
            saveProjectToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            saveProjectToolStripMenuItem.Text = "Save session...";
            saveProjectToolStripMenuItem.ToolTipText = "Save a session (all open tabs)";
            saveProjectToolStripMenuItem.Click += OnSaveProjectToolStripMenuItemClick;
            // 
            // exportBookmarksToolStripMenuItem
            // 
            exportBookmarksToolStripMenuItem.Name = "exportBookmarksToolStripMenuItem";
            exportBookmarksToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            exportBookmarksToolStripMenuItem.Text = "Export bookmarks...";
            exportBookmarksToolStripMenuItem.ToolTipText = "Write a list of bookmarks and their comments to a CSV file";
            exportBookmarksToolStripMenuItem.Click += OnExportBookmarksToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension3
            // 
            menuToolStripSeparatorExtension3.Name = "menuToolStripSeparatorExtension3";
            menuToolStripSeparatorExtension3.Size = new System.Drawing.Size(250, 6);
            // 
            // lastUsedToolStripMenuItem
            // 
            lastUsedToolStripMenuItem.Name = "lastUsedToolStripMenuItem";
            lastUsedToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            lastUsedToolStripMenuItem.Text = "Last used";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Image = LogExpert.Resources.Exit;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += OnExitToolStripMenuItemClick;
            // 
            // viewNavigateToolStripMenuItem
            // 
            viewNavigateToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { goToLineToolStripMenuItem, searchToolStripMenuItem, filterToolStripMenuItem, bookmarksToolStripMenuItem, columnFinderToolStripMenuItem, menuToolStripSeparatorExtension5, toolStripEncodingMenuItem, menuToolStripSeparatorExtension6, timeshiftToolStripMenuItem, timeshiftMenuTextBox, menuToolStripSeparatorExtension4, copyMarkedLinesIntoNewTabToolStripMenuItem });
            viewNavigateToolStripMenuItem.Name = "viewNavigateToolStripMenuItem";
            viewNavigateToolStripMenuItem.Size = new System.Drawing.Size(96, 19);
            viewNavigateToolStripMenuItem.Text = "View/Navigate";
            // 
            // goToLineToolStripMenuItem
            // 
            goToLineToolStripMenuItem.Name = "goToLineToolStripMenuItem";
            goToLineToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.G;
            goToLineToolStripMenuItem.Size = new System.Drawing.Size(189, 30);
            goToLineToolStripMenuItem.Text = "Go to line...";
            goToLineToolStripMenuItem.Click += OnGoToLineToolStripMenuItemClick;
            // 
            // searchToolStripMenuItem
            // 
            searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            searchToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            searchToolStripMenuItem.Size = new System.Drawing.Size(189, 30);
            searchToolStripMenuItem.Text = "Search...";
            searchToolStripMenuItem.Click += OnSearchToolStripMenuItemClick;
            // 
            // filterToolStripMenuItem
            // 
            filterToolStripMenuItem.Image = LogExpert.Resources.Filter;
            filterToolStripMenuItem.Name = "filterToolStripMenuItem";
            filterToolStripMenuItem.ShortcutKeys = Keys.F4;
            filterToolStripMenuItem.Size = new System.Drawing.Size(189, 30);
            filterToolStripMenuItem.Text = "Filter";
            filterToolStripMenuItem.Click += OnFilterToolStripMenuItemClick;
            // 
            // bookmarksToolStripMenuItem
            // 
            bookmarksToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toggleBookmarkToolStripMenuItem, jumpToNextToolStripMenuItem, jumpToPrevToolStripMenuItem, showBookmarkListToolStripMenuItem });
            bookmarksToolStripMenuItem.Name = "bookmarksToolStripMenuItem";
            bookmarksToolStripMenuItem.Size = new System.Drawing.Size(189, 30);
            bookmarksToolStripMenuItem.Text = "Bookmarks";
            // 
            // toggleBookmarkToolStripMenuItem
            // 
            toggleBookmarkToolStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            toggleBookmarkToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            toggleBookmarkToolStripMenuItem.Image = LogExpert.Resources.Bookmark_add;
            toggleBookmarkToolStripMenuItem.Name = "toggleBookmarkToolStripMenuItem";
            toggleBookmarkToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F2;
            toggleBookmarkToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            toggleBookmarkToolStripMenuItem.Text = "Toggle Bookmark";
            toggleBookmarkToolStripMenuItem.Click += OnToggleBookmarkToolStripMenuItemClick;
            // 
            // jumpToNextToolStripMenuItem
            // 
            jumpToNextToolStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            jumpToNextToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            jumpToNextToolStripMenuItem.Image = LogExpert.Resources.ArrowDown;
            jumpToNextToolStripMenuItem.Name = "jumpToNextToolStripMenuItem";
            jumpToNextToolStripMenuItem.ShortcutKeys = Keys.F2;
            jumpToNextToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            jumpToNextToolStripMenuItem.Text = "Jump to next";
            jumpToNextToolStripMenuItem.Click += OnJumpToNextToolStripMenuItemClick;
            // 
            // jumpToPrevToolStripMenuItem
            // 
            jumpToPrevToolStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            jumpToPrevToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            jumpToPrevToolStripMenuItem.Image = LogExpert.Resources.ArrowUp;
            jumpToPrevToolStripMenuItem.Name = "jumpToPrevToolStripMenuItem";
            jumpToPrevToolStripMenuItem.ShortcutKeys = Keys.Shift | Keys.F2;
            jumpToPrevToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            jumpToPrevToolStripMenuItem.Text = "Jump to prev";
            jumpToPrevToolStripMenuItem.Click += OnJumpToPrevToolStripMenuItemClick;
            // 
            // showBookmarkListToolStripMenuItem
            // 
            showBookmarkListToolStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            showBookmarkListToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            showBookmarkListToolStripMenuItem.Name = "showBookmarkListToolStripMenuItem";
            showBookmarkListToolStripMenuItem.ShortcutKeys = Keys.F6;
            showBookmarkListToolStripMenuItem.Size = new System.Drawing.Size(253, 30);
            showBookmarkListToolStripMenuItem.Text = "Bookmark list";
            showBookmarkListToolStripMenuItem.Click += OnShowBookmarkListToolStripMenuItemClick;
            // 
            // columnFinderToolStripMenuItem
            // 
            columnFinderToolStripMenuItem.CheckOnClick = true;
            columnFinderToolStripMenuItem.Name = "columnFinderToolStripMenuItem";
            columnFinderToolStripMenuItem.ShortcutKeys = Keys.F8;
            columnFinderToolStripMenuItem.Size = new System.Drawing.Size(189, 30);
            columnFinderToolStripMenuItem.Text = "Column finder";
            columnFinderToolStripMenuItem.Click += OnColumnFinderToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension5
            // 
            menuToolStripSeparatorExtension5.Name = "menuToolStripSeparatorExtension5";
            menuToolStripSeparatorExtension5.Size = new System.Drawing.Size(186, 6);
            // 
            // toolStripEncodingMenuItem
            // 
            toolStripEncodingMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripEncodingASCIIItem, toolStripEncodingANSIItem, toolStripEncodingISO88591Item, toolStripEncodingUTF8Item, toolStripEncodingUTF16Item });
            toolStripEncodingMenuItem.Name = "toolStripEncodingMenuItem";
            toolStripEncodingMenuItem.Size = new System.Drawing.Size(189, 30);
            toolStripEncodingMenuItem.Text = "Encoding";
            // 
            // toolStripEncodingASCIIItem
            // 
            toolStripEncodingASCIIItem.BackColor = System.Drawing.SystemColors.Control;
            toolStripEncodingASCIIItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            toolStripEncodingASCIIItem.Name = "toolStripEncodingASCIIItem";
            toolStripEncodingASCIIItem.Size = new System.Drawing.Size(132, 22);
            toolStripEncodingASCIIItem.Tag = "";
            toolStripEncodingASCIIItem.Text = "ASCII";
            toolStripEncodingASCIIItem.Click += OnASCIIToolStripMenuItemClick;
            // 
            // toolStripEncodingANSIItem
            // 
            toolStripEncodingANSIItem.BackColor = System.Drawing.SystemColors.Control;
            toolStripEncodingANSIItem.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            toolStripEncodingANSIItem.Name = "toolStripEncodingANSIItem";
            toolStripEncodingANSIItem.Size = new System.Drawing.Size(132, 22);
            toolStripEncodingANSIItem.Tag = "";
            toolStripEncodingANSIItem.Text = "ANSI";
            toolStripEncodingANSIItem.Click += OnANSIToolStripMenuItemClick;
            // 
            // toolStripEncodingISO88591Item
            // 
            toolStripEncodingISO88591Item.BackColor = System.Drawing.SystemColors.Control;
            toolStripEncodingISO88591Item.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            toolStripEncodingISO88591Item.Name = "toolStripEncodingISO88591Item";
            toolStripEncodingISO88591Item.Size = new System.Drawing.Size(132, 22);
            toolStripEncodingISO88591Item.Text = "ISO-8859-1";
            toolStripEncodingISO88591Item.Click += OnISO88591ToolStripMenuItemClick;
            // 
            // toolStripEncodingUTF8Item
            // 
            toolStripEncodingUTF8Item.BackColor = System.Drawing.SystemColors.Control;
            toolStripEncodingUTF8Item.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            toolStripEncodingUTF8Item.Name = "toolStripEncodingUTF8Item";
            toolStripEncodingUTF8Item.Size = new System.Drawing.Size(132, 22);
            toolStripEncodingUTF8Item.Text = "UTF8";
            toolStripEncodingUTF8Item.Click += OnUTF8ToolStripMenuItemClick;
            // 
            // toolStripEncodingUTF16Item
            // 
            toolStripEncodingUTF16Item.BackColor = System.Drawing.SystemColors.Control;
            toolStripEncodingUTF16Item.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            toolStripEncodingUTF16Item.Name = "toolStripEncodingUTF16Item";
            toolStripEncodingUTF16Item.Size = new System.Drawing.Size(132, 22);
            toolStripEncodingUTF16Item.Text = "Unicode";
            toolStripEncodingUTF16Item.Click += OnUTF16ToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension6
            // 
            menuToolStripSeparatorExtension6.Name = "menuToolStripSeparatorExtension6";
            menuToolStripSeparatorExtension6.Size = new System.Drawing.Size(186, 6);
            // 
            // timeshiftToolStripMenuItem
            // 
            timeshiftToolStripMenuItem.CheckOnClick = true;
            timeshiftToolStripMenuItem.Name = "timeshiftToolStripMenuItem";
            timeshiftToolStripMenuItem.Size = new System.Drawing.Size(189, 30);
            timeshiftToolStripMenuItem.Text = "Timeshift";
            timeshiftToolStripMenuItem.ToolTipText = "If supported by the columnizer, you can set an offset to the displayed log time";
            timeshiftToolStripMenuItem.CheckStateChanged += OnTimeShiftToolStripMenuItemCheckStateChanged;
            // 
            // timeshiftMenuTextBox
            // 
            timeshiftMenuTextBox.BorderStyle = BorderStyle.FixedSingle;
            timeshiftMenuTextBox.Enabled = false;
            timeshiftMenuTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            timeshiftMenuTextBox.Name = "timeshiftMenuTextBox";
            timeshiftMenuTextBox.Size = new System.Drawing.Size(100, 23);
            timeshiftMenuTextBox.Text = "+00:00:00.000";
            timeshiftMenuTextBox.ToolTipText = "Time offset (hh:mm:ss.fff)";
            timeshiftMenuTextBox.KeyDown += OnTimeShiftMenuTextBoxKeyDown;
            // 
            // menuToolStripSeparatorExtension4
            // 
            menuToolStripSeparatorExtension4.Name = "menuToolStripSeparatorExtension4";
            menuToolStripSeparatorExtension4.Size = new System.Drawing.Size(186, 6);
            // 
            // copyMarkedLinesIntoNewTabToolStripMenuItem
            // 
            copyMarkedLinesIntoNewTabToolStripMenuItem.Name = "copyMarkedLinesIntoNewTabToolStripMenuItem";
            copyMarkedLinesIntoNewTabToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.T;
            copyMarkedLinesIntoNewTabToolStripMenuItem.Size = new System.Drawing.Size(189, 30);
            copyMarkedLinesIntoNewTabToolStripMenuItem.Text = "Copy to Tab";
            copyMarkedLinesIntoNewTabToolStripMenuItem.ToolTipText = "Copies all selected lines into a new tab page";
            copyMarkedLinesIntoNewTabToolStripMenuItem.Click += OnCopyMarkedLinesIntoNewTabToolStripMenuItemClick;
            // 
            // optionToolStripMenuItem
            // 
            optionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { columnizerToolStripMenuItem, hilightingToolStripMenuItem1, menuToolStripSeparatorExtension7, settingsToolStripMenuItem, menuToolStripSeparatorExtension9, cellSelectModeToolStripMenuItem, alwaysOnTopToolStripMenuItem, hideLineColumnToolStripMenuItem, menuToolStripSeparatorExtension8, lockInstanceToolStripMenuItem });
            optionToolStripMenuItem.Name = "optionToolStripMenuItem";
            optionToolStripMenuItem.Size = new System.Drawing.Size(61, 19);
            optionToolStripMenuItem.Text = "Options";
            optionToolStripMenuItem.DropDownOpening += OnOptionToolStripMenuItemDropDownOpening;
            // 
            // columnizerToolStripMenuItem
            // 
            columnizerToolStripMenuItem.Name = "columnizerToolStripMenuItem";
            columnizerToolStripMenuItem.Size = new System.Drawing.Size(224, 30);
            columnizerToolStripMenuItem.Text = "Columnizer...";
            columnizerToolStripMenuItem.ToolTipText = "Splits various kinds of logfiles into fixed columns";
            columnizerToolStripMenuItem.Click += OnSelectFilterToolStripMenuItemClick;
            // 
            // hilightingToolStripMenuItem1
            // 
            hilightingToolStripMenuItem1.Name = "hilightingToolStripMenuItem1";
            hilightingToolStripMenuItem1.Size = new System.Drawing.Size(224, 30);
            hilightingToolStripMenuItem1.Text = "Highlighting and triggers...";
            hilightingToolStripMenuItem1.Click += OnHighlightingToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension7
            // 
            menuToolStripSeparatorExtension7.Name = "menuToolStripSeparatorExtension7";
            menuToolStripSeparatorExtension7.Size = new System.Drawing.Size(221, 6);
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Image = LogExpert.Resources.Settings;
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new System.Drawing.Size(224, 30);
            settingsToolStripMenuItem.Text = "Settings...";
            settingsToolStripMenuItem.Click += OnSettingsToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension9
            // 
            menuToolStripSeparatorExtension9.Name = "menuToolStripSeparatorExtension9";
            menuToolStripSeparatorExtension9.Size = new System.Drawing.Size(221, 6);
            // 
            // cellSelectModeToolStripMenuItem
            // 
            cellSelectModeToolStripMenuItem.CheckOnClick = true;
            cellSelectModeToolStripMenuItem.Name = "cellSelectModeToolStripMenuItem";
            cellSelectModeToolStripMenuItem.Size = new System.Drawing.Size(224, 30);
            cellSelectModeToolStripMenuItem.Text = "Cell select mode";
            cellSelectModeToolStripMenuItem.ToolTipText = "Switches between foll row selection and single cell selection mode";
            cellSelectModeToolStripMenuItem.Click += OnCellSelectModeToolStripMenuItemClick;
            // 
            // alwaysOnTopToolStripMenuItem
            // 
            alwaysOnTopToolStripMenuItem.CheckOnClick = true;
            alwaysOnTopToolStripMenuItem.Name = "alwaysOnTopToolStripMenuItem";
            alwaysOnTopToolStripMenuItem.Size = new System.Drawing.Size(224, 30);
            alwaysOnTopToolStripMenuItem.Text = "Always on top";
            alwaysOnTopToolStripMenuItem.Click += OnAlwaysOnTopToolStripMenuItemClick;
            // 
            // hideLineColumnToolStripMenuItem
            // 
            hideLineColumnToolStripMenuItem.CheckOnClick = true;
            hideLineColumnToolStripMenuItem.Name = "hideLineColumnToolStripMenuItem";
            hideLineColumnToolStripMenuItem.Size = new System.Drawing.Size(224, 30);
            hideLineColumnToolStripMenuItem.Text = "Hide line column";
            hideLineColumnToolStripMenuItem.Click += OnHideLineColumnToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension8
            // 
            menuToolStripSeparatorExtension8.Name = "menuToolStripSeparatorExtension8";
            menuToolStripSeparatorExtension8.Size = new System.Drawing.Size(221, 6);
            // 
            // lockInstanceToolStripMenuItem
            // 
            lockInstanceToolStripMenuItem.Name = "lockInstanceToolStripMenuItem";
            lockInstanceToolStripMenuItem.Size = new System.Drawing.Size(224, 30);
            lockInstanceToolStripMenuItem.Text = "Lock instance";
            lockInstanceToolStripMenuItem.ToolTipText = "When enabled all new launched LogExpert instances will redirect to this window";
            lockInstanceToolStripMenuItem.Click += OnLockInstanceToolStripMenuItemClick;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { configureToolStripMenuItem, configureToolStripSeparator });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 19);
            toolsToolStripMenuItem.Text = "Tools";
            toolsToolStripMenuItem.ToolTipText = "Launch external tools (configure in the settings)";
            toolsToolStripMenuItem.DropDownItemClicked += OnToolsToolStripMenuItemDropDownItemClicked;
            // 
            // configureToolStripMenuItem
            // 
            configureToolStripMenuItem.Name = "configureToolStripMenuItem";
            configureToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            configureToolStripMenuItem.Text = "Configure...";
            configureToolStripMenuItem.Click += OnConfigureToolStripMenuItemClick;
            // 
            // configureToolStripSeparator
            // 
            configureToolStripSeparator.Name = "configureToolStripSeparator";
            configureToolStripSeparator.Size = new System.Drawing.Size(133, 6);
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showHelpToolStripMenuItem, menuToolStripSeparatorExtension11, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size(44, 19);
            helpToolStripMenuItem.Text = "Help";
            // 
            // showHelpToolStripMenuItem
            // 
            showHelpToolStripMenuItem.Name = "showHelpToolStripMenuItem";
            showHelpToolStripMenuItem.ShortcutKeys = Keys.F1;
            showHelpToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            showHelpToolStripMenuItem.Text = "Show help";
            showHelpToolStripMenuItem.Click += OnShowHelpToolStripMenuItemClick;
            // 
            // menuToolStripSeparatorExtension11
            // 
            menuToolStripSeparatorExtension11.Name = "menuToolStripSeparatorExtension11";
            menuToolStripSeparatorExtension11.Size = new System.Drawing.Size(145, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += OnAboutToolStripMenuItemClick;
            // 
            // debugToolStripMenuItem
            // 
            debugToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            debugToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { dumpLogBufferInfoToolStripMenuItem, dumpBufferDiagnosticToolStripMenuItem, runGCToolStripMenuItem, gCInfoToolStripMenuItem, throwExceptionGUIThreadToolStripMenuItem, throwExceptionbackgroundThToolStripMenuItem, throwExceptionBackgroundThreadToolStripMenuItem, loglevelToolStripMenuItem, disableWordHighlightModeToolStripMenuItem });
            debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            debugToolStripMenuItem.Size = new System.Drawing.Size(54, 19);
            debugToolStripMenuItem.Text = "Debug";
            // 
            // dumpLogBufferInfoToolStripMenuItem
            // 
            dumpLogBufferInfoToolStripMenuItem.Name = "dumpLogBufferInfoToolStripMenuItem";
            dumpLogBufferInfoToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            dumpLogBufferInfoToolStripMenuItem.Text = "Dump LogBuffer info";
            dumpLogBufferInfoToolStripMenuItem.Click += OnDumpLogBufferInfoToolStripMenuItemClick;
            // 
            // dumpBufferDiagnosticToolStripMenuItem
            // 
            dumpBufferDiagnosticToolStripMenuItem.Name = "dumpBufferDiagnosticToolStripMenuItem";
            dumpBufferDiagnosticToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            dumpBufferDiagnosticToolStripMenuItem.Text = "Dump buffer diagnostic";
            dumpBufferDiagnosticToolStripMenuItem.Click += OnDumpBufferDiagnosticToolStripMenuItemClick;
            // 
            // runGCToolStripMenuItem
            // 
            runGCToolStripMenuItem.Name = "runGCToolStripMenuItem";
            runGCToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            runGCToolStripMenuItem.Text = "Run GC";
            runGCToolStripMenuItem.Click += OnRunGCToolStripMenuItemClick;
            // 
            // gCInfoToolStripMenuItem
            // 
            gCInfoToolStripMenuItem.Name = "gCInfoToolStripMenuItem";
            gCInfoToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            gCInfoToolStripMenuItem.Text = "Dump GC info";
            gCInfoToolStripMenuItem.Click += OnGCInfoToolStripMenuItemClick;
            // 
            // throwExceptionGUIThreadToolStripMenuItem
            // 
            throwExceptionGUIThreadToolStripMenuItem.Name = "throwExceptionGUIThreadToolStripMenuItem";
            throwExceptionGUIThreadToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            throwExceptionGUIThreadToolStripMenuItem.Text = "Throw exception (GUI Thread)";
            throwExceptionGUIThreadToolStripMenuItem.Click += OnThrowExceptionGUIThreadToolStripMenuItemClick;
            // 
            // throwExceptionbackgroundThToolStripMenuItem
            // 
            throwExceptionbackgroundThToolStripMenuItem.Name = "throwExceptionbackgroundThToolStripMenuItem";
            throwExceptionbackgroundThToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            throwExceptionbackgroundThToolStripMenuItem.Text = "Throw exception (Async delegate)";
            throwExceptionbackgroundThToolStripMenuItem.Click += OnThrowExceptionBackgroundThToolStripMenuItemClick;
            // 
            // throwExceptionBackgroundThreadToolStripMenuItem
            // 
            throwExceptionBackgroundThreadToolStripMenuItem.Name = "throwExceptionBackgroundThreadToolStripMenuItem";
            throwExceptionBackgroundThreadToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            throwExceptionBackgroundThreadToolStripMenuItem.Text = "Throw exception (background thread)";
            throwExceptionBackgroundThreadToolStripMenuItem.Click += OnThrowExceptionBackgroundThreadToolStripMenuItemClick;
            // 
            // loglevelToolStripMenuItem
            // 
            loglevelToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { warnToolStripMenuItem, infoToolStripMenuItem, debugToolStripMenuItem1 });
            loglevelToolStripMenuItem.Name = "loglevelToolStripMenuItem";
            loglevelToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            loglevelToolStripMenuItem.Text = "Loglevel";
            loglevelToolStripMenuItem.DropDownOpening += OnLogLevelToolStripMenuItemDropDownOpening;
            loglevelToolStripMenuItem.Click += OnLogLevelToolStripMenuItemClick;
            // 
            // warnToolStripMenuItem
            // 
            warnToolStripMenuItem.Name = "warnToolStripMenuItem";
            warnToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            warnToolStripMenuItem.Text = "Warn";
            warnToolStripMenuItem.Click += OnWarnToolStripMenuItemClick;
            // 
            // infoToolStripMenuItem
            // 
            infoToolStripMenuItem.Name = "infoToolStripMenuItem";
            infoToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            infoToolStripMenuItem.Text = "Info";
            infoToolStripMenuItem.Click += OnInfoToolStripMenuItemClick;
            // 
            // debugToolStripMenuItem1
            // 
            debugToolStripMenuItem1.Name = "debugToolStripMenuItem1";
            debugToolStripMenuItem1.Size = new System.Drawing.Size(109, 22);
            debugToolStripMenuItem1.Text = "Debug";
            debugToolStripMenuItem1.Click += OnDebugToolStripMenuItemClick;
            // 
            // disableWordHighlightModeToolStripMenuItem
            // 
            disableWordHighlightModeToolStripMenuItem.CheckOnClick = true;
            disableWordHighlightModeToolStripMenuItem.Name = "disableWordHighlightModeToolStripMenuItem";
            disableWordHighlightModeToolStripMenuItem.Size = new System.Drawing.Size(274, 22);
            disableWordHighlightModeToolStripMenuItem.Text = "Disable word highlight mode";
            disableWordHighlightModeToolStripMenuItem.Click += OnDisableWordHighlightModeToolStripMenuItemClick;
            // 
            // host
            // 
            host.AccessibleName = "host";
            host.AutoSize = true;
            host.BackColor = System.Drawing.Color.Transparent;
            host.Location = new System.Drawing.Point(9, 1);
            host.Name = "host";
            host.Size = new System.Drawing.Size(80, 22);
            host.TabIndex = 7;
            host.Text = "Follow tail";
            host.UseVisualStyleBackColor = false;
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
            toolStripContainer.ContentPanel.Size = new System.Drawing.Size(1603, 881);
            toolStripContainer.Dock = DockStyle.Fill;
            // 
            // toolStripContainer.LeftToolStripPanel
            // 
            toolStripContainer.LeftToolStripPanel.Enabled = false;
            toolStripContainer.LeftToolStripPanelVisible = false;
            toolStripContainer.Location = new System.Drawing.Point(0, 0);
            toolStripContainer.Margin = new Padding(0);
            toolStripContainer.Name = "toolStripContainer";
            // 
            // toolStripContainer.RightToolStripPanel
            // 
            toolStripContainer.RightToolStripPanel.Enabled = false;
            toolStripContainer.RightToolStripPanelVisible = false;
            toolStripContainer.Size = new System.Drawing.Size(1603, 954);
            toolStripContainer.TabIndex = 13;
            toolStripContainer.Text = "toolStripContainer1";
            // 
            // toolStripContainer.TopToolStripPanel
            // 
            toolStripContainer.TopToolStripPanel.Controls.Add(externalToolsToolStrip);
            toolStripContainer.TopToolStripPanel.Controls.Add(mainMenuStrip);
            toolStripContainer.TopToolStripPanel.Controls.Add(buttonToolStrip);
            // 
            // dockPanel
            // 
            dockPanel.ActiveAutoHideContent = null;
            dockPanel.DefaultFloatWindowSize = new System.Drawing.Size(600, 400);
            dockPanel.Dock = DockStyle.Fill;
            dockPanel.DockBackColor = System.Drawing.SystemColors.Control;
            dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            dockPanel.ForeColor = System.Drawing.SystemColors.Control;
            dockPanel.Location = new System.Drawing.Point(0, 0);
            dockPanel.Margin = new Padding(0);
            dockPanel.Name = "dockPanel";
            dockPanel.ShowDocumentIcon = true;
            dockPanel.Size = new System.Drawing.Size(1603, 881);
            dockPanelGradient1.EndColor = System.Drawing.SystemColors.Control;
            dockPanelGradient1.StartColor = System.Drawing.SystemColors.Control;
            autoHideStripSkin1.DockStripGradient = dockPanelGradient1;
            tabGradient1.EndColor = System.Drawing.SystemColors.Control;
            tabGradient1.StartColor = System.Drawing.SystemColors.Control;
            tabGradient1.TextColor = System.Drawing.SystemColors.ControlText;
            autoHideStripSkin1.TabGradient = tabGradient1;
            autoHideStripSkin1.TextFont = new System.Drawing.Font("Segoe UI", 9F);
            tabGradient2.EndColor = System.Drawing.SystemColors.Control;
            tabGradient2.StartColor = System.Drawing.SystemColors.Control;
            tabGradient2.TextColor = System.Drawing.SystemColors.ControlText;
            dockPaneStripGradient1.ActiveTabGradient = tabGradient2;
            dockPanelGradient2.EndColor = System.Drawing.SystemColors.Control;
            dockPanelGradient2.StartColor = System.Drawing.SystemColors.Control;
            dockPaneStripGradient1.DockStripGradient = dockPanelGradient2;
            tabGradient3.EndColor = System.Drawing.SystemColors.ControlLight;
            tabGradient3.StartColor = System.Drawing.SystemColors.ControlLight;
            tabGradient3.TextColor = System.Drawing.SystemColors.ControlText;
            dockPaneStripGradient1.InactiveTabGradient = tabGradient3;
            dockPaneStripSkin1.DocumentGradient = dockPaneStripGradient1;
            dockPaneStripSkin1.TextFont = new System.Drawing.Font("Segoe UI", 9F);
            tabGradient4.EndColor = System.Drawing.SystemColors.ActiveCaption;
            tabGradient4.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            tabGradient4.StartColor = System.Drawing.SystemColors.GradientActiveCaption;
            tabGradient4.TextColor = System.Drawing.SystemColors.ActiveCaptionText;
            dockPaneStripToolWindowGradient1.ActiveCaptionGradient = tabGradient4;
            tabGradient5.EndColor = System.Drawing.SystemColors.Control;
            tabGradient5.StartColor = System.Drawing.SystemColors.Control;
            tabGradient5.TextColor = System.Drawing.SystemColors.ControlText;
            dockPaneStripToolWindowGradient1.ActiveTabGradient = tabGradient5;
            dockPanelGradient3.EndColor = System.Drawing.SystemColors.ControlLight;
            dockPanelGradient3.StartColor = System.Drawing.SystemColors.ControlLight;
            dockPaneStripToolWindowGradient1.DockStripGradient = dockPanelGradient3;
            tabGradient6.EndColor = System.Drawing.SystemColors.InactiveCaption;
            tabGradient6.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            tabGradient6.StartColor = System.Drawing.SystemColors.GradientInactiveCaption;
            tabGradient6.TextColor = System.Drawing.SystemColors.InactiveCaptionText;
            dockPaneStripToolWindowGradient1.InactiveCaptionGradient = tabGradient6;
            tabGradient7.EndColor = System.Drawing.Color.Transparent;
            tabGradient7.StartColor = System.Drawing.Color.Transparent;
            tabGradient7.TextColor = System.Drawing.SystemColors.Control;
            dockPaneStripToolWindowGradient1.InactiveTabGradient = tabGradient7;
            dockPaneStripSkin1.ToolWindowGradient = dockPaneStripToolWindowGradient1;
            dockPanel.Theme = new VS2015LightTheme();
            dockPanel.Theme.Skin.DockPaneStripSkin = dockPaneStripSkin1;
            dockPanel.Theme.Skin.AutoHideStripSkin = autoHideStripSkin1;
            dockPanel.TabIndex = 14;
            dockPanel.ActiveContentChanged += OnDockPanelActiveContentChanged;
            // 
            // externalToolsToolStrip
            // 
            externalToolsToolStrip.AllowMerge = false;
            externalToolsToolStrip.Dock = DockStyle.None;
            externalToolsToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            externalToolsToolStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            externalToolsToolStrip.Location = new System.Drawing.Point(8, 0);
            externalToolsToolStrip.Name = "externalToolsToolStrip";
            externalToolsToolStrip.Size = new System.Drawing.Size(32, 19);
            externalToolsToolStrip.TabIndex = 8;
            externalToolsToolStrip.ItemClicked += OnExternalToolsToolStripItemClicked;
            // 
            // buttonToolStrip
            // 
            buttonToolStrip.AllowMerge = false;
            buttonToolStrip.Dock = DockStyle.None;
            buttonToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            buttonToolStrip.Items.AddRange(new ToolStripItem[] { toolStripButtonOpen, lineToolStripSeparatorExtension1, toolStripButtonSearch, toolStripButtonFilter, lineToolStripSeparatorExtension2, toolStripButtonBookmark, toolStripButtonUp, toolStripButtonDown, lineToolStripSeparatorExtension3, toolStripButtonBubbles, lineToolStripSeparatorExtension4, toolStripButtonTail, lineToolStripSeparatorExtension5, groupsComboBoxHighlightGroups });
            buttonToolStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            buttonToolStrip.Location = new System.Drawing.Point(3, 42);
            buttonToolStrip.Name = "buttonToolStrip";
            buttonToolStrip.Size = new System.Drawing.Size(406, 31);
            buttonToolStrip.TabIndex = 7;
            // 
            // toolStripButtonOpen
            // 
            toolStripButtonOpen.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonOpen.Image = LogExpert.Resources.File_open;
            toolStripButtonOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonOpen.Name = "toolStripButtonOpen";
            toolStripButtonOpen.Size = new System.Drawing.Size(28, 28);
            toolStripButtonOpen.Text = "Open File";
            toolStripButtonOpen.ToolTipText = "Open file";
            toolStripButtonOpen.Click += OnToolStripButtonOpenClick;
            // 
            // lineToolStripSeparatorExtension1
            // 
            lineToolStripSeparatorExtension1.Name = "lineToolStripSeparatorExtension1";
            lineToolStripSeparatorExtension1.Size = new System.Drawing.Size(6, 23);
            // 
            // toolStripButtonSearch
            // 
            toolStripButtonSearch.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonSearch.Image = LogExpert.Resources.Search;
            toolStripButtonSearch.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonSearch.Name = "toolStripButtonSearch";
            toolStripButtonSearch.Size = new System.Drawing.Size(28, 28);
            toolStripButtonSearch.Text = "Search";
            toolStripButtonSearch.ToolTipText = "Search";
            toolStripButtonSearch.Click += OnToolStripButtonSearchClick;
            // 
            // toolStripButtonFilter
            // 
            toolStripButtonFilter.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonFilter.Image = LogExpert.Resources.Filter;
            toolStripButtonFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonFilter.Name = "toolStripButtonFilter";
            toolStripButtonFilter.Size = new System.Drawing.Size(28, 28);
            toolStripButtonFilter.Text = "Filter";
            toolStripButtonFilter.ToolTipText = "Filter window";
            toolStripButtonFilter.Click += OnToolStripButtonFilterClick;
            // 
            // lineToolStripSeparatorExtension2
            // 
            lineToolStripSeparatorExtension2.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            lineToolStripSeparatorExtension2.Name = "lineToolStripSeparatorExtension2";
            lineToolStripSeparatorExtension2.Size = new System.Drawing.Size(6, 23);
            // 
            // toolStripButtonBookmark
            // 
            toolStripButtonBookmark.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonBookmark.Image = LogExpert.Resources.Bookmark_add;
            toolStripButtonBookmark.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonBookmark.Name = "toolStripButtonBookmark";
            toolStripButtonBookmark.Size = new System.Drawing.Size(28, 28);
            toolStripButtonBookmark.Text = "Toggle Bookmark";
            toolStripButtonBookmark.ToolTipText = "Toggle bookmark";
            toolStripButtonBookmark.Click += OnToolStripButtonBookmarkClick;
            // 
            // toolStripButtonUp
            // 
            toolStripButtonUp.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonUp.Image = LogExpert.Resources.ArrowUp;
            toolStripButtonUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonUp.Name = "toolStripButtonUp";
            toolStripButtonUp.Size = new System.Drawing.Size(28, 28);
            toolStripButtonUp.Text = "Previous Bookmark";
            toolStripButtonUp.ToolTipText = "Go to previous bookmark";
            toolStripButtonUp.Click += OnToolStripButtonUpClick;
            // 
            // toolStripButtonDown
            // 
            toolStripButtonDown.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonDown.Image = LogExpert.Resources.ArrowDown;
            toolStripButtonDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonDown.Name = "toolStripButtonDown";
            toolStripButtonDown.Size = new System.Drawing.Size(28, 28);
            toolStripButtonDown.Text = "Next Bookmark";
            toolStripButtonDown.ToolTipText = "Go to next bookmark";
            toolStripButtonDown.Click += OnToolStripButtonDownClick;
            // 
            // lineToolStripSeparatorExtension3
            // 
            lineToolStripSeparatorExtension3.Name = "lineToolStripSeparatorExtension3";
            lineToolStripSeparatorExtension3.Size = new System.Drawing.Size(6, 23);
            // 
            // toolStripButtonBubbles
            // 
            toolStripButtonBubbles.CheckOnClick = true;
            toolStripButtonBubbles.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonBubbles.Image = LogExpert.Resources.bookmark_bubbles;
            toolStripButtonBubbles.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
            toolStripButtonBubbles.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonBubbles.Name = "toolStripButtonBubbles";
            toolStripButtonBubbles.Size = new System.Drawing.Size(28, 28);
            toolStripButtonBubbles.Text = "Show bookmark bubbles";
            toolStripButtonBubbles.Click += OnToolStripButtonBubblesClick;
            // 
            // lineToolStripSeparatorExtension4
            // 
            lineToolStripSeparatorExtension4.Name = "lineToolStripSeparatorExtension4";
            lineToolStripSeparatorExtension4.Size = new System.Drawing.Size(6, 23);
            // 
            // toolStripButtonTail
            // 
            toolStripButtonTail.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonTail.Image = (System.Drawing.Image)resources.GetObject("toolStripButtonTail.Image");
            toolStripButtonTail.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButtonTail.Name = "toolStripButtonTail";
            toolStripButtonTail.Size = new System.Drawing.Size(27, 19);
            toolStripButtonTail.Text = "tail";
            // 
            // lineToolStripSeparatorExtension5
            // 
            lineToolStripSeparatorExtension5.Name = "lineToolStripSeparatorExtension5";
            lineToolStripSeparatorExtension5.Size = new System.Drawing.Size(6, 23);
            // 
            // groupsComboBoxHighlightGroups
            // 
            groupsComboBoxHighlightGroups.DropDownStyle = ComboBoxStyle.DropDownList;
            groupsComboBoxHighlightGroups.DropDownWidth = 250;
            groupsComboBoxHighlightGroups.FlatStyle = FlatStyle.Standard;
            groupsComboBoxHighlightGroups.Name = "groupsComboBoxHighlightGroups";
            groupsComboBoxHighlightGroups.Size = new System.Drawing.Size(150, 23);
            groupsComboBoxHighlightGroups.ToolTipText = "Select the current highlight settings for the log file (right-click to open highlight settings)";
            groupsComboBoxHighlightGroups.DropDownClosed += OnHighlightGroupsComboBoxDropDownClosed;
            groupsComboBoxHighlightGroups.SelectedIndexChanged += OnHighlightGroupsComboBoxSelectedIndexChanged;
            groupsComboBoxHighlightGroups.MouseUp += OnHighlightGroupsComboBoxMouseUp;
            // 
            // checkBoxFollowTail
            // 
            checkBoxFollowTail.AutoSize = true;
            checkBoxFollowTail.Location = new System.Drawing.Point(663, 985);
            checkBoxFollowTail.Margin = new Padding(4, 7, 4, 7);
            checkBoxFollowTail.Name = "checkBoxFollowTail";
            checkBoxFollowTail.Size = new System.Drawing.Size(80, 19);
            checkBoxFollowTail.TabIndex = 14;
            checkBoxFollowTail.Text = "Follow tail";
            checkBoxFollowTail.UseVisualStyleBackColor = true;
            checkBoxFollowTail.Click += OnFollowTailCheckBoxClick;
            // 
            // tabContextMenuStrip
            // 
            tabContextMenuStrip.ForeColor = System.Drawing.SystemColors.ControlText;
            tabContextMenuStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            tabContextMenuStrip.Items.AddRange(new ToolStripItem[] { closeThisTabToolStripMenuItem, closeOtherTabsToolStripMenuItem, closeAllTabsToolStripMenuItem, tabColorToolStripMenuItem, tabRenameToolStripMenuItem, copyPathToClipboardToolStripMenuItem, findInExplorerToolStripMenuItem, truncateFileToolStripMenuItem });
            tabContextMenuStrip.Name = "tabContextMenuStrip";
            tabContextMenuStrip.Size = new System.Drawing.Size(197, 158);
            // 
            // closeThisTabToolStripMenuItem
            // 
            closeThisTabToolStripMenuItem.BackColor = System.Drawing.SystemColors.Control;
            closeThisTabToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlText;
            closeThisTabToolStripMenuItem.Name = "closeThisTabToolStripMenuItem";
            closeThisTabToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            closeThisTabToolStripMenuItem.Text = "Close this tab";
            closeThisTabToolStripMenuItem.Click += OnCloseThisTabToolStripMenuItemClick;
            // 
            // closeOtherTabsToolStripMenuItem
            // 
            closeOtherTabsToolStripMenuItem.Name = "closeOtherTabsToolStripMenuItem";
            closeOtherTabsToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            closeOtherTabsToolStripMenuItem.Text = "Close other tabs";
            closeOtherTabsToolStripMenuItem.ToolTipText = "Close all tabs except of this one";
            closeOtherTabsToolStripMenuItem.Click += OnCloseOtherTabsToolStripMenuItemClick;
            // 
            // closeAllTabsToolStripMenuItem
            // 
            closeAllTabsToolStripMenuItem.Name = "closeAllTabsToolStripMenuItem";
            closeAllTabsToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            closeAllTabsToolStripMenuItem.Text = "Close all tabs";
            closeAllTabsToolStripMenuItem.ToolTipText = "Close all tabs";
            closeAllTabsToolStripMenuItem.Click += OnCloseAllTabsToolStripMenuItemClick;
            // 
            // tabColorToolStripMenuItem
            // 
            tabColorToolStripMenuItem.Name = "tabColorToolStripMenuItem";
            tabColorToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            tabColorToolStripMenuItem.Text = "Tab color...";
            tabColorToolStripMenuItem.ToolTipText = "Sets the tab color";
            tabColorToolStripMenuItem.Click += OnTabColorToolStripMenuItemClick;
            // 
            // tabRenameToolStripMenuItem
            // 
            tabRenameToolStripMenuItem.Name = "tabRenameToolStripMenuItem";
            tabRenameToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            tabRenameToolStripMenuItem.Text = "Tab rename...";
            tabRenameToolStripMenuItem.ToolTipText = "Set the text which is shown on the tab";
            tabRenameToolStripMenuItem.Click += OnTabRenameToolStripMenuItemClick;
            // 
            // copyPathToClipboardToolStripMenuItem
            // 
            copyPathToClipboardToolStripMenuItem.Name = "copyPathToClipboardToolStripMenuItem";
            copyPathToClipboardToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            copyPathToClipboardToolStripMenuItem.Text = "Copy path to clipboard";
            copyPathToClipboardToolStripMenuItem.ToolTipText = "The complete file name (incl. path) is copied to clipboard";
            copyPathToClipboardToolStripMenuItem.Click += OnCopyPathToClipboardToolStripMenuItemClick;
            // 
            // findInExplorerToolStripMenuItem
            // 
            findInExplorerToolStripMenuItem.Name = "findInExplorerToolStripMenuItem";
            findInExplorerToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            findInExplorerToolStripMenuItem.Text = "Find in Explorer";
            findInExplorerToolStripMenuItem.ToolTipText = "Opens an Explorer window and selects the log file";
            findInExplorerToolStripMenuItem.Click += OnFindInExplorerToolStripMenuItemClick;
            // 
            // truncateFileToolStripMenuItem
            // 
            this.truncateFileToolStripMenuItem.Name = "truncateFileToolStripMenuItem";
            this.truncateFileToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.truncateFileToolStripMenuItem.Text = "Truncate File";
            this.truncateFileToolStripMenuItem.ToolTipText = "Try to truncate the file opened in tab";
            this.truncateFileToolStripMenuItem.Click += new System.EventHandler(this.TruncateFileToolStripMenuItem_Click);
            // 
            // dragControlDateTime
            // 
            dragControlDateTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            dragControlDateTime.BackColor = System.Drawing.SystemColors.Control;
            dragControlDateTime.DateTime = new System.DateTime(0L);
            dragControlDateTime.DragOrientation = DragOrientationsEnum.Vertical;
            dragControlDateTime.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dragControlDateTime.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            dragControlDateTime.HoverColor = System.Drawing.Color.LightGray;
            dragControlDateTime.Location = new System.Drawing.Point(1017, 977);
            dragControlDateTime.Margin = new Padding(0);
            dragControlDateTime.MaxDateTime = new System.DateTime(9999, 12, 31, 23, 59, 59, 999);
            dragControlDateTime.MinDateTime = new System.DateTime(0L);
            dragControlDateTime.Name = "dragControlDateTime";
            dragControlDateTime.Size = new System.Drawing.Size(313, 38);
            dragControlDateTime.TabIndex = 14;
            dragControlDateTime.ValueChanged += OnDateTimeDragControlValueChanged;
            dragControlDateTime.ValueDragged += OnDateTimeDragControlValueDragged;
            // 
            // LogTabWindow
            // 
            AllowDrop = true;
            ClientSize = new System.Drawing.Size(1603, 1017);
            Controls.Add(checkBoxFollowTail);
            Controls.Add(dragControlDateTime);
            Controls.Add(toolStripContainer);
            Controls.Add(statusStrip);
            DoubleBuffered = true;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
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
        private System.Windows.Forms.ToolStripMenuItem hilightingToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem cellSelectModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox timeshiftMenuTextBox;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnTopToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bookmarksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggleBookmarkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem jumpToNextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem jumpToPrevToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripEncodingMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripEncodingASCIIItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripEncodingANSIItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripEncodingUTF8Item;
        private System.Windows.Forms.ToolStripMenuItem toolStripEncodingUTF16Item;
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
        private CheckBox host;
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
        private ToolStripMenuItem loadProjectToolStripMenuItem;
        private ToolStripMenuItem saveProjectToolStripMenuItem;
        private ToolStripButton toolStripButtonBubbles;
        private ToolStripMenuItem copyPathToClipboardToolStripMenuItem;
        private ToolStripMenuItem findInExplorerToolStripMenuItem;
        private ToolStripMenuItem truncateFileToolStripMenuItem;
        private ToolStripMenuItem exportBookmarksToolStripMenuItem;
        private ToolStripComboBox groupsComboBoxHighlightGroups;
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
        private ToolStripMenuItem warnToolStripMenuItem;
        private ToolStripMenuItem infoToolStripMenuItem;
        private ToolStripMenuItem debugToolStripMenuItem1;
        private ToolStripMenuItem disableWordHighlightModeToolStripMenuItem;
        private ToolStripMenuItem multifileMaskToolStripMenuItem;
        private ToolStripMenuItem multiFileEnabledStripMenuItem;
        private ToolStripMenuItem toolStripEncodingISO88591Item;
        private ToolStripMenuItem lockInstanceToolStripMenuItem;
        private ToolStripMenuItem newFromClipboardToolStripMenuItem;
        private ToolStripMenuItem openURIToolStripMenuItem;
        private ToolStripMenuItem columnFinderToolStripMenuItem;
        private DockPanel dockPanel;
        private ToolStripMenuItem tabRenameToolStripMenuItem;
        private LineToolStripSeparatorExtension lineToolStripSeparatorExtension1;
        private LineToolStripSeparatorExtension lineToolStripSeparatorExtension2;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension1;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension2;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension3;
        private LineToolStripSeparatorExtension lineToolStripSeparatorExtension3;
        private LineToolStripSeparatorExtension lineToolStripSeparatorExtension4;
        private LineToolStripSeparatorExtension lineToolStripSeparatorExtension5;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension5;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension6;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension4;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension7;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension9;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension8;
        private MenuToolStripSeparatorExtension configureToolStripSeparator;
        private MenuToolStripSeparatorExtension menuToolStripSeparatorExtension11;
    }
}

