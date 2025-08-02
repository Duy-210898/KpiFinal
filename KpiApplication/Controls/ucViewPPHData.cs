using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Models;
using KpiApplication.Excel;
using KpiApplication.Services;
using KpiApplication.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

namespace KpiApplication.Controls
{
    public partial class ucViewPPHData : DevExpress.XtraEditors.XtraUserControl, ISupportLoadAsync
    {
        private BindingList<IEPPHDataForUser_Model> iepphDataList;
        private readonly IEPPHData_DAL iePPHData_DAL = new IEPPHData_DAL();

        public ucViewPPHData()
        {
            InitializeComponent();
            ApplyLocalizedText();
            dgvPPHData.CellMerge += dgvPPHData_CellMerge;
        }

        private void ApplyLocalizedText()
        {
            btnExport.Caption = Lang.Export;
            btnRefresh.Caption = Lang.Refresh;
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                UseWaitCursor = true;
                var data = await Task.Run(FetchData);
                ConfigureGridView(data);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.LoadDataFailed, ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private BindingList<IEPPHDataForUser_Model> FetchData()
            => iePPHData_DAL.GetIEPPHDataForUser();

        private void ConfigureGridView(BindingList<IEPPHDataForUser_Model> data)
        {
            iepphDataList = data;
            gridControl1.DataSource = iepphDataList;

            dgvPPHData.OptionsBehavior.Editable = false;
            dgvPPHData.OptionsView.AllowCellMerge = true;
            dgvPPHData.OptionsView.RowAutoHeight = true;
            dgvPPHData.OptionsView.ColumnAutoWidth = false;

            GridViewHelper.ApplyDefaultFormatting(dgvPPHData);
            GridViewHelper.EnableWordWrapForGridView(dgvPPHData);
            GridViewHelper.EnableCopyFunctionality(dgvPPHData);

            GridViewHelper.HideColumns(dgvPPHData,
                "OutsourcingStitchingBool",
                "OutsourcingAssemblingBool",
                "OutsourcingStockFittingBool",
                "OutsourcingStitching",
                "OutsourcingAssembling",
                "OutsourcingStockFitting");

            GridViewHelper.SetColumnCaptions(dgvPPHData, new Dictionary<string, string>
            {
                ["IEPPHValue"] = "IE PPH",
                ["ArticleName"] = Lang.ArticleName,
                ["IsSigned"] = Lang.ProductionSign,
                ["DataStatus"] = Lang.DataStatus,
                ["NoteForPC"] = Lang.NoteForPC,
                ["Process"] = Lang.Process,
                ["ModelName"] = Lang.ModelName,
                ["PCSend"] = Lang.PCSend,
                ["AdjustOperatorNo"] = Lang.AdjustOperator,
                ["TargetOutputPC"] = Lang.TargetOutput,
                ["StageName"] = Lang.Process,
                ["THTValue"] = "THT",
                ["TypeName"] = Lang.Type,
                ["PersonIncharge"] = Lang.PersonIncharge
            });

            var fixedWidths = new Dictionary<string, int>
            {
                ["NoteForPC"] = 220,
                ["ModelName"] = 220
            };

            foreach (GridColumn column in dgvPPHData.Columns)
            {
                if (fixedWidths.TryGetValue(column.FieldName, out int fixedWidth))
                {
                    column.Width = fixedWidth;
                }
                else
                {
                    column.BestFit();
                }
            }

            int totalWidth = dgvPPHData.Columns
                .Where(c => c.Visible)
                .Sum(c => c.Width);

            int gridViewWidth = dgvPPHData.ViewRect.Width - SystemInformation.VerticalScrollBarWidth;
            int remaining = gridViewWidth - totalWidth;

            if (remaining > 0)
            {
                var stretchColumn = dgvPPHData.Columns["NoteForPC"];
                if (stretchColumn != null)
                    stretchColumn.Width += remaining;
            }
        }

        private void dgvPPHData_CellMerge(object sender, CellMergeEventArgs e)
        {
            string[] mergeableColumns =
            {
                "ArticleName", "ModelName", "PCSend",
                "PersonIncharge", "OutsourcingAssembling", "NoteForPC",
                "OutsourcingStitching", "OutsourcingStockFitting", "DataStatus"
            };

            if (!mergeableColumns.Contains(e.Column.FieldName))
            {
                e.Merge = false;
                e.Handled = true;
                return;
            }

            dgvPPHData.Columns["ModelName"].AppearanceCell.TextOptions.VAlignment = VertAlignment.Center;

            var val1 = dgvPPHData.GetRowCellValue(e.RowHandle1, "ArticleName")?.ToString();
            var val2 = dgvPPHData.GetRowCellValue(e.RowHandle2, "ArticleName")?.ToString();

            e.Merge = val1 == val2;
            e.Handled = true;
        }

        private async void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (iepphDataList == null || iepphDataList.Count == 0)
            {
                MessageBoxHelper.ShowInfo(Lang.NoDataToExport);
                return;
            }

            var result = MessageBoxHelper.ShowConfirmYesNoCancel(
                Lang.IncludeTCTInExport,
                Lang.ExcelExportOptions);

            if (result == DialogResult.Cancel)
                return;

            bool includeTCT = (result == DialogResult.Yes);

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveDialog.FileName = "PPHData.xlsx";

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return;

                string originalPath = saveDialog.FileName;
                string filePath = originalPath;
                string dir = Path.GetDirectoryName(originalPath);
                string baseName = Path.GetFileNameWithoutExtension(originalPath);
                string ext = Path.GetExtension(originalPath);

                int count = 1;
                while (File.Exists(filePath))
                {
                    filePath = Path.Combine(dir, $"{baseName}_{count}{ext}");
                    count++;
                }

                await Task.Run(() =>
                {
                    var convertedData = iepphDataList.Select(item => new IETotal_Model
                    {
                        ArticleName = item.ArticleName,
                        ModelName = item.ModelName,
                        Process = item.Process,
                        IEPPHValue = item.IEPPHValue,
                        THTValue = item.THTValue,
                        TargetOutputPC = item.TargetOutputPC,
                        AdjustOperatorNo = item.AdjustOperatorNo,
                        IsSigned = item.IsSigned,
                        TypeName = item.TypeName,
                        PersonIncharge = item.PersonIncharge,
                        NoteForPC = item.NoteForPC
                    }).ToList();

                    ExcelExporter.ExportIETotalPivoted(convertedData, filePath, includeTCT);
                });

                MessageBoxHelper.ShowInfo(Lang.ExcelExportSuccess);

                try
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch
                {
                    MessageBoxHelper.ShowError(Lang.CannotOpenExportedFile);
                }
            }
        }
    }
}