using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.Common;
using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Utils
{
    public static class TCTGridHelper
    {
        public static void ApplyGridSettings(GridView view, bool editable)
        {
            view.OptionsBehavior.Editable = editable;
            view.OptionsView.EnableAppearanceEvenRow = true;
            view.OptionsView.EnableAppearanceOddRow = true;
            view.OptionsView.RowAutoHeight = true;
            view.OptionsView.ColumnAutoWidth = false;

            GridViewHelper.ApplyDefaultFormatting(view);
            GridViewHelper.EnableWordWrapForGridView(view);
            GridViewHelper.EnableCopyFunctionality(view);
        }

        public static void SetCaptions(GridView view)
        {
            GridViewHelper.SetColumnCaptions(view, new Dictionary<string, string>
            {
                ["Type"] = Lang.Type,
                ["ModelName"] = Lang.ModelName,
                ["Cutting"] = Lang.Cutting,
                ["Stitching"] = Lang.Stitching,
                ["Assembly"] = Lang.Assembly,
                ["StockFitting"] = Lang.StockFitting,
                ["TotalTCT"] = Lang.TotalTCT,
                ["LastUpdatedAt"] = Lang.LastUpdatedAt,
                ["Notes"] = Lang.Notes
            });
        }

        public static void AutoAdjustColumnWidths(GridView view)
        {
            foreach (GridColumn column in view.Columns)
            {
                if (column.FieldName != "Notes")
                    column.BestFit();
            }

            int totalColumnWidth = view.Columns.Where(c => c.Visible).Sum(c => c.Width);
            int viewWidth = view.ViewRect.Width - SystemInformation.VerticalScrollBarWidth;
            int remaining = viewWidth - totalColumnWidth;

            if (remaining > 0)
            {
                var stretchColumn = view.Columns["Notes"];
                if (stretchColumn != null)
                    stretchColumn.Width += remaining;
            }
        }

        public static bool IsAllFieldsNull(TCTData_Pivoted item) =>
            string.IsNullOrWhiteSpace(item.Type)
            && item.Cutting == null
            && item.Stitching == null
            && item.Assembly == null
            && item.StockFitting == null;
    }
}
