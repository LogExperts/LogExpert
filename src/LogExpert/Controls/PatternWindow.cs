﻿using LogExpert.Core.Classes;
using LogExpert.Core.Entities.EventArgs;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LogExpert.Controls
{
    internal partial class PatternWindow : Form
    {
        #region Fields

        private readonly List<List<PatternBlock>> blockList = [];
        private PatternBlock currentBlock;
        private List<PatternBlock> currentList;

        private readonly LogWindow.LogWindow logWindow;
        private PatternArgs patternArgs = new();

        #endregion

        #region cTor

        public PatternWindow()
        {
            InitializeComponent();
        }

        public PatternWindow(LogWindow.LogWindow logWindow)
        {
            this.logWindow = logWindow;
            InitializeComponent();
            recalcButton.Enabled = false;
        }

        #endregion

        #region Properties

        public int Fuzzy
        {
            set { fuzzyKnobControl.Value = value; }
            get { return fuzzyKnobControl.Value; }
        }

        public int MaxDiff
        {
            set { maxDiffKnobControl.Value = value; }
            get { return maxDiffKnobControl.Value; }
        }

        public int MaxMisses
        {
            set { maxMissesKnobControl.Value = value; }
            get { return maxMissesKnobControl.Value; }
        }

        public int Weight
        {
            set { weigthKnobControl.Value = value; }
            get { return weigthKnobControl.Value; }
        }

        #endregion

        #region Public methods

        public void SetBlockList(List<PatternBlock> flatBlockList, PatternArgs patternArgs)
        {
            this.patternArgs = patternArgs;
            blockList.Clear();
            List<PatternBlock> singeList = [];
            //int blockId = -1;
            for (int i = 0; i < flatBlockList.Count; ++i)
            {
                PatternBlock block = flatBlockList[i];
                singeList.Add(block);
                //if (block.blockId != blockId)
                //{
                //  singeList = new List<PatternBlock>();
                //  PatternBlock selfRefBlock = new PatternBlock();
                //  selfRefBlock.targetStart = block.startLine;
                //  selfRefBlock.targetEnd = block.endLine;
                //  selfRefBlock.blockId = block.blockId;
                //  singeList.Add(selfRefBlock);
                //  singeList.Add(block);
                //  this.blockList.Add(singeList);
                //  blockId = block.blockId;
                //}
                //else
                //{
                //  singeList.Add(block);
                //}
            }
            blockList.Add(singeList);
            Invoke(new MethodInvoker(SetBlockListGuiStuff));
        }


        public void SetColumnizer(ILogLineColumnizer columnizer)
        {
            logWindow.SetColumnizer(columnizer, patternHitsDataGridView);
            logWindow.SetColumnizer(columnizer, contentDataGridView);
            patternHitsDataGridView.Columns[0].Width = 20;
            contentDataGridView.Columns[0].Width = 20;

            DataGridViewTextBoxColumn blockInfoColumn = new();
            blockInfoColumn.HeaderText = "Weight";
            blockInfoColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            blockInfoColumn.Resizable = DataGridViewTriState.False;
            blockInfoColumn.DividerWidth = 1;
            blockInfoColumn.ReadOnly = true;
            blockInfoColumn.Width = 50;

            DataGridViewTextBoxColumn contentInfoColumn = new();
            contentInfoColumn.HeaderText = "Diff";
            contentInfoColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            contentInfoColumn.Resizable = DataGridViewTriState.False;
            contentInfoColumn.DividerWidth = 1;
            contentInfoColumn.ReadOnly = true;
            contentInfoColumn.Width = 50;

            patternHitsDataGridView.Columns.Insert(1, blockInfoColumn);
            contentDataGridView.Columns.Insert(1, contentInfoColumn);
        }

        public void SetFont(string fontName, float fontSize)
        {
            Font font = new(new FontFamily(fontName), fontSize);
            int lineSpacing = font.FontFamily.GetLineSpacing(FontStyle.Regular);
            float lineSpacingPixel = font.Size * lineSpacing / font.FontFamily.GetEmHeight(FontStyle.Regular);

            patternHitsDataGridView.DefaultCellStyle.Font = font;
            contentDataGridView.DefaultCellStyle.Font = font;
            //this.lineHeight = font.Height + 4;
            patternHitsDataGridView.RowTemplate.Height = font.Height + 4;
            contentDataGridView.RowTemplate.Height = font.Height + 4;
        }

        #endregion

        #region Private Methods

        private void SetBlockListGuiStuff()
        {
            patternHitsDataGridView.RowCount = 0;
            blockCountLabel.Text = "0";
            contentDataGridView.RowCount = 0;
            blockLinesLabel.Text = "0";
            recalcButton.Enabled = true;
            setRangeButton.Enabled = true;
            if (blockList.Count > 0)
            {
                SetCurrentList(blockList[0]);
            }
        }

        private void SetCurrentList(List<PatternBlock> patternList)
        {
            patternHitsDataGridView.RowCount = 0;
            currentList = patternList;
            patternHitsDataGridView.RowCount = currentList.Count;
            patternHitsDataGridView.Refresh();
            blockCountLabel.Text = "" + currentList.Count;
        }

        private int GetLineForHitGrid(int rowIndex)
        {
            int line;
            line = currentList[rowIndex].targetStart;
            return line;
        }

        private int GetLineForContentGrid(int rowIndex)
        {
            int line;
            line = currentBlock.targetStart + rowIndex;
            return line;
        }

        #endregion

        #region Events handler

        private void patternHitsDataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (currentList == null || e.RowIndex < 0)
            {
                return;
            }
            int rowIndex = GetLineForHitGrid(e.RowIndex);
            int colIndex = e.ColumnIndex;
            if (colIndex == 1)
            {
                e.Value = currentList[e.RowIndex].weigth;
            }
            else
            {
                if (colIndex > 1)
                {
                    colIndex--; // correct the additional inserted col
                }
                e.Value = logWindow.GetCellValue(rowIndex, colIndex);
            }
        }

        private void patternHitsDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (currentList == null || e.RowIndex < 0)
            {
                return;
            }
            if (e.ColumnIndex == 1)
            {
                e.PaintBackground(e.CellBounds, false);
                int selCount = patternArgs.endLine - patternArgs.startLine;
                int maxWeight = patternArgs.maxDiffInBlock * selCount + selCount;
                if (maxWeight > 0)
                {
                    int width = (int)((double)(int)e.Value / (double)maxWeight * (double)e.CellBounds.Width);
                    Rectangle rect = new(e.CellBounds.X, e.CellBounds.Y, width, e.CellBounds.Height);
                    int alpha = 90 + (int)((double)(int)e.Value / (double)maxWeight * (double)165);
                    Color color = Color.FromArgb(alpha, 170, 180, 150);
                    Brush brush = new SolidBrush(color);
                    rect.Inflate(-2, -1);
                    e.Graphics.FillRectangle(brush, rect);
                    brush.Dispose();
                }
                e.PaintContent(e.CellBounds);
                e.Handled = true;
            }
            else
            {
                DataGridView gridView = (DataGridView)sender;
                int rowIndex = GetLineForHitGrid(e.RowIndex);
                logWindow.CellPainting(gridView, rowIndex, e);
            }
        }

        private void patternHitsDataGridView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //if (this.currentList == null || patternHitsDataGridView.CurrentRow == null)
            //  return;
            //int rowIndex = GetLineForHitGrid(patternHitsDataGridView.CurrentRow.Index);

            //this.logWindow.SelectLogLine(rowIndex);
        }

        private void patternHitsDataGridView_CurrentCellChanged(object sender, EventArgs e)
        {
            if (currentList == null || patternHitsDataGridView.CurrentRow == null)
            {
                return;
            }
            if (patternHitsDataGridView.CurrentRow.Index > currentList.Count - 1)
            {
                return;
            }
            contentDataGridView.RowCount = 0;
            currentBlock = currentList[patternHitsDataGridView.CurrentRow.Index];
            contentDataGridView.RowCount = currentBlock.targetEnd - currentBlock.targetStart + 1;
            contentDataGridView.Refresh();
            contentDataGridView.CurrentCell = contentDataGridView.Rows[0].Cells[0];
            blockLinesLabel.Text = "" + contentDataGridView.RowCount;
        }

        private void contentDataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (currentBlock == null || e.RowIndex < 0)
            {
                return;
            }
            int rowIndex = GetLineForContentGrid(e.RowIndex);
            int colIndex = e.ColumnIndex;
            if (colIndex == 1)
            {
                QualityInfo qi;
                if (currentBlock.qualityInfoList.TryGetValue(rowIndex, out qi))
                {
                    e.Value = qi.quality;
                }
                else
                {
                    e.Value = "";
                }
            }
            else
            {
                if (colIndex != 0)
                {
                    colIndex--; // adjust the inserted column
                }
                e.Value = logWindow.GetCellValue(rowIndex, colIndex);
            }
        }

        private void contentDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (currentBlock == null || e.RowIndex < 0)
            {
                return;
            }
            DataGridView gridView = (DataGridView)sender;
            int rowIndex = GetLineForContentGrid(e.RowIndex);
            logWindow.CellPainting(gridView, rowIndex, e);
        }

        private void contentDataGridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (currentBlock == null || contentDataGridView.CurrentRow == null)
            {
                return;
            }
            int rowIndex = GetLineForContentGrid(contentDataGridView.CurrentRow.Index);

            logWindow.SelectLogLine(rowIndex);
        }

        private void recalcButton_Click(object sender, EventArgs e)
        {
            patternArgs.fuzzy = fuzzyKnobControl.Value;
            patternArgs.maxDiffInBlock = maxDiffKnobControl.Value;
            patternArgs.maxMisses = maxMissesKnobControl.Value;
            patternArgs.minWeight = weigthKnobControl.Value;
            logWindow.PatternStatistic(patternArgs);
            recalcButton.Enabled = false;
            setRangeButton.Enabled = false;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void contentDataGridView_ColumnDividerDoubleClick(object sender,
            DataGridViewColumnDividerDoubleClickEventArgs e)
        {
            e.Handled = true;
            contentDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void patternHitsDataGridView_ColumnDividerDoubleClick(object sender,
            DataGridViewColumnDividerDoubleClickEventArgs e)
        {
            e.Handled = true;
            patternHitsDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void setRangeButton_Click(object sender, EventArgs e)
        {
            logWindow.PatternStatisticSelectRange(patternArgs);
            recalcButton.Enabled = true;
            rangeLabel.Text = "Start: " + patternArgs.startLine + "\r\nEnd: " + patternArgs.endLine;
        }

        #endregion
    }
}