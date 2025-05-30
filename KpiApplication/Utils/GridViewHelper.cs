using DevExpress.Export;
using DevExpress.Utils;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KpiApplication.Utils
{
    public static class GridViewHelper
    {
        private static readonly HashSet<string> NumericFields = new HashSet<string>()
        {
            "Quantity", "Target", "TotalWorker", "WorkingTime",
            "IEPPH", "OutputRate", "PPHRate", "TotalWorkingHours", "ActualPPH",
            "TargetOutputPC", "AdjustOperatorNo", "IEPPHValue", "THTValue"
        };

        public static void ApplyDefaultFormatting(GridView gridView, List<string> editableColumns = null)
        {
            if (gridView == null) return;
            if (editableColumns == null)
                editableColumns = new List<string>();

            foreach (GridColumn col in gridView.Columns)
            {
                col.AppearanceCell.TextOptions.HAlignment =
                    IsNumericColumn(col.FieldName) ? HorzAlignment.Center : HorzAlignment.Near;

                col.OptionsColumn.AllowEdit = editableColumns.Contains(col.FieldName);
            }
            gridView.Appearance.HeaderPanel.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            gridView.OptionsView.ColumnHeaderAutoHeight = DevExpress.Utils.DefaultBoolean.True;
            gridView.OptionsBehavior.Editable = true;
            gridView.OptionsBehavior.AutoPopulateColumns = false;
            gridView.OptionsView.ShowAutoFilterRow = true;
            gridView.OptionsView.ColumnAutoWidth = false;
            gridView.OptionsDetail.EnableMasterViewMode = false;
            gridView.Appearance.HeaderPanel.BackColor = Color.LightGray;

            gridView.CustomDrawCell += (s, e) =>
            {
                if (e.RowHandle >= 0 && e.Column.FieldName == "OutputRate")
                {
                    var view = s as GridView;
                    string cellValue = view?.GetRowCellValue(e.RowHandle, e.Column)?.ToString();

                    if (!string.IsNullOrEmpty(cellValue) && cellValue.EndsWith("%"))
                    {
                        if (double.TryParse(cellValue.TrimEnd('%'), out double percent) && percent < 90)
                        {
                            e.Appearance.ForeColor = Color.Red;
                        }
                    }
                }
            };
        }
        public static void ApplyRowStyleAlternateColors(GridView gridView, Color evenRowColor, Color oddRowColor)
        {
            if (gridView == null) return;

            gridView.RowStyle += (s, e) =>
            {
                if (e.RowHandle >= 0)
                {
                    e.Appearance.BackColor = (e.RowHandle % 2 == 0) ? evenRowColor : oddRowColor;
                    e.Appearance.TextOptions.VAlignment = VertAlignment.Center;
                }
            };
        }

        public static void FixColumns( GridView gridView, params string[] columnNames)
        {
            foreach (var colName in columnNames)
            {
                var col = gridView.Columns[colName];
                if (col != null)
                    col.Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Left;
            }
        }


        private static bool IsNumericColumn(string fieldName) => NumericFields.Contains(fieldName);

        public static void AdjustGridColumnWidthsAndRowHeight(GridView gridView)
        {
            if (gridView == null) return;

            gridView.BestFitColumns();
            int totalWidth = gridView.GridControl.Width;

            SetColumnWidth(gridView, "ModelName", (int)(totalWidth * 0.2));
            gridView.OptionsView.ColumnAutoWidth = true;
        }

        private static void SetColumnWidth(GridView gridView, string columnName, int width)
        {
            gridView.Columns.ColumnByFieldName(columnName)?.SetWidth(width);
        }

        private static void SetWidth(this GridColumn column, int width)
        {
            if (column != null)
                column.Width = width;
        }

        public static void EnableCopyFunctionality(GridView gridView)
        {
            if (gridView == null || gridView.GridControl == null) return;

            gridView.OptionsClipboard.CopyColumnHeaders = DefaultBoolean.False;

            gridView.GridControl.KeyDown += (sender, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    int rowHandle = gridView.FocusedRowHandle;
                    var column = gridView.FocusedColumn;

                    if (rowHandle >= 0 && column != null)
                    {
                        var value = gridView.GetRowCellValue(rowHandle, column);
                        if (value != null)
                            Clipboard.SetText(value.ToString());
                    }

                    e.Handled = true;
                }
            };
        }
        public static void SetColumnCaptions(GridView gridView, Dictionary<string, string> captions)
        {
            foreach (var pair in captions)
            {
                var column = gridView.Columns.ColumnByFieldName(pair.Key);
                if (column != null)
                {
                    column.Caption = pair.Value;
                }
            }
        }


        public static void HideColumns(GridView gridView, params string[] columnNames)
        {
            if (gridView == null || columnNames == null) return;

            var nameSet = new HashSet<string>(columnNames);
            foreach (var col in gridView.Columns.Cast<GridColumn>().Where(c => nameSet.Contains(c.FieldName)))
                col.Visible = false;
        }
        public static void ZoomGrid( GridView gridView, float zoomFactor)
        {
            var currentFont = gridView.Appearance.Row.Font;
            var newSize = Math.Max(6f, currentFont.Size * zoomFactor);
            var newFont = new Font(currentFont.FontFamily, newSize);

            gridView.Appearance.Row.Font = newFont;
            gridView.Appearance.HeaderPanel.Font = newFont;
            gridView.Appearance.GroupRow.Font = newFont;
            gridView.Appearance.FocusedRow.Font = newFont;
            gridView.Appearance.SelectedRow.Font = newFont;

            gridView.RowHeight = (int)(newSize * 2.2f);

            gridView.BeginUpdate();
            foreach (GridColumn col in gridView.Columns)
            {
                col.BestFit();
            }
            gridView.EndUpdate();

            gridView.LayoutChanged();
        }

        public static void EnableWordWrapForGridView(GridView gridView)
        {
            if (gridView == null) return;

            var memoEdit = new RepositoryItemMemoEdit { WordWrap = true };
            var wrapColumns = new[] { "ModelName", "Article", "Notes" };

            foreach (var col in gridView.Columns.Cast<GridColumn>())
            {
                if (wrapColumns.Contains(col.FieldName))
                {
                    col.ColumnEdit = memoEdit;
                }
            }

            gridView.OptionsView.RowAutoHeight = true;
        }
    }
}
