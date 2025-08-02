using DevExpress.Export;
using DevExpress.Utils;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
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
            "TargetOutputPC", "AdjustOperatorNo", "IEPPHValue", "THTValue", "TargetIE", "OperatorAdjust", "ReferenceOperator",
            "FinalOperator", "TCTValue"
        };
        public static void StretchRemainingSpace(GridView gridView, string fixedColumnName, int fixedWidth)
        {
            gridView.BeginUpdate();

            gridView.OptionsView.ColumnAutoWidth = false;

            // Đặt chiều rộng cố định cho cột cần giữ cứng
            var fixedCol = gridView.Columns.ColumnByFieldName(fixedColumnName);
            if (fixedCol != null && fixedCol.Visible)
                fixedCol.Width = fixedWidth;

            // Các cột khác BestFit
            foreach (GridColumn col in gridView.Columns)
            {
                if (col.Visible && col.FieldName != fixedColumnName)
                {
                    col.BestFit();
                }
            }

            // Tính tổng chiều rộng hiện tại
            int totalWidth = gridView.Columns
                .Where(c => c.Visible)
                .Sum(c => c.Width);

            int viewWidth = gridView.ViewRect.Width - SystemInformation.VerticalScrollBarWidth;
            int remaining = viewWidth - totalWidth;

            // Nếu còn dư không gian thì giãn vào fixedColumn
            if (remaining > 0 && fixedCol != null)
            {
                fixedCol.Width += remaining;
            }

            gridView.EndUpdate();
        }
        public static void ConfigureGrid(GridView view, GridControl control, IEnumerable<GridColumn> fixedCols = null, string[] readOnlyColumns = null)
        {
            view.OptionsView.AllowCellMerge = true;
            view.OptionsView.RowAutoHeight = true;
            view.OptionsView.ColumnAutoWidth = false;

            view.OptionsSelection.MultiSelect = true;
            view.OptionsSelection.MultiSelectMode = GridMultiSelectMode.RowSelect;

            ApplyDefaultFormatting(view);
            EnableWordWrapForGridView(view);

            if (fixedCols != null)
            {
                foreach (var col in fixedCols)
                {
                    col.Fixed = FixedStyle.Left;
                }
            }

            if (readOnlyColumns != null)
            {
                foreach (string colName in readOnlyColumns)
                {
                    var column = view.Columns.ColumnByFieldName(colName);
                    if (column != null)
                    {
                        column.OptionsColumn.AllowEdit = false;
                        column.OptionsColumn.ReadOnly = true;
                        column.ColumnEdit = null;
                    }
                }
            }

            EnableCopyFunctionality(view);
        }

        public static void SetColumnCaptions(GridView view, Dictionary<string, string> captions)
        {
            foreach (var kvp in captions)
            {
                if (view.Columns[kvp.Key] != null)
                    view.Columns[kvp.Key].Caption = kvp.Value;
            }
        }

        public static void ApplyColumnWidths(GridView view, Dictionary<string, int> fixedWidthColumns)
        {
            foreach (GridColumn col in view.Columns)
            {
                if (fixedWidthColumns.TryGetValue(col.FieldName, out int width))
                {
                    col.Width = width;
                    col.OptionsColumn.FixedWidth = true;
                }
                else
                {
                    col.BestFit();
                }
            }
        }

        public static void HideColumns(GridView view, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                var col = view.Columns[name];
                if (col != null)
                    col.Visible = false;
            }
        }

        public static void ReorderColumns(GridView view, string[] desiredOrder)
        {
            int index = 0;
            foreach (var field in desiredOrder)
            {
                var col = view.Columns[field];
                if (col != null)
                    col.VisibleIndex = index++;
            }
        }

        public static void SetupMemoEditColumn(GridControl gridControl, GridView view, string columnName)
        {
            var col = view.Columns[columnName];
            if (col == null) return;

            var memoEdit = new RepositoryItemMemoEdit
            {
                WordWrap = true, 
                AcceptsReturn = false
            };
            gridControl.RepositoryItems.Add(memoEdit);
            col.ColumnEdit = memoEdit;

            // Căn giữa theo chiều dọc để không bị lệch
            col.AppearanceCell.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

            // Đảm bảo GridView hỗ trợ tự động điều chỉnh chiều cao dòng
            view.OptionsView.RowAutoHeight = true;
        }

        public static void SetupComboBoxColumn(GridControl gridControl, GridView view, string columnName, IEnumerable<string> items)
        {
            var col = view.Columns[columnName];
            if (col == null) return;

            var combo = new RepositoryItemComboBox
            {
                TextEditStyle = TextEditStyles.Standard, 
                AutoComplete = true                     
            };

            combo.Items.AddRange(items
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToArray());

            gridControl.RepositoryItems.Add(combo);
            col.ColumnEdit = combo;

            col.OptionsEditForm.Visible = DevExpress.Utils.DefaultBoolean.True;
            col.OptionsEditForm.UseEditorColRowSpan = false;
        }
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
            gridView.OptionsView.EnableAppearanceEvenRow = true;
            gridView.OptionsView.EnableAppearanceOddRow = true;

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

        public static void FixColumns(GridView gridView, params string[] columnNames)
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
        public static void SetColumnFixedWidth(GridView gridView, Dictionary<string, int> columnWidths)
        {
            foreach (var pair in columnWidths)
            {
                var column = gridView.Columns[pair.Key];
                if (column == null) continue;

                column.Width = pair.Value;
                column.OptionsColumn.FixedWidth = true;
            }
        }
        public static void ZoomGrid(GridView gridView, float zoomFactor)
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
