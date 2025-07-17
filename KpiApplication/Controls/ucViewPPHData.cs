using DevExpress.Utils;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucViewPPHData : DevExpress.XtraEditors.XtraUserControl
    {
        private BindingList<IEPPHDataForUser_Model> iepphDataList;
        private readonly IEPPHData_DAL iePPHData_DAL = new IEPPHData_DAL();

        public ucViewPPHData()
        {
            InitializeComponent();
            dgvPPHData.CellMerge += dgvPPHData_CellMerge;
        }

        private async void ucViewPPHData_Load(object sender, EventArgs e)
        {
            await LoadDataAsync("Loading...");
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await LoadDataAsync("Refreshing data...");
        }

        /// <summary>
        /// Tải dữ liệu và cấu hình lại GridView
        /// </summary>
        private async Task LoadDataAsync(string splashMessage)
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                FetchData,
                ConfigureGridView,
                splashMessage);
        }

        private BindingList<IEPPHDataForUser_Model> FetchData()
            => iePPHData_DAL.GetIEPPHDataForUser();

        private void ConfigureGridView(BindingList<IEPPHDataForUser_Model> data)
        {
            iepphDataList = data;
            gridControl1.DataSource = iepphDataList;

            // Cấu hình cơ bản của GridView
            dgvPPHData.OptionsBehavior.Editable = false;
            dgvPPHData.OptionsView.AllowCellMerge = true;

            // Sử dụng GridViewHelper chuẩn hóa hiển thị
            GridViewHelper.ApplyDefaultFormatting(dgvPPHData);
            GridViewHelper.EnableWordWrapForGridView(dgvPPHData);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvPPHData);
            GridViewHelper.EnableCopyFunctionality(dgvPPHData);

            // Ẩn các cột không cần hiển thị
            GridViewHelper.HideColumns(dgvPPHData,
                "OutsourcingStitchingBool",
                "OutsourcingAssemblingBool",
                "OutsourcingStockFittingBool",
                "OutsourcingStitching",
                "OutsourcingAssembling",
                "OutsourcingStockFitting");

            // Đặt lại caption
            GridViewHelper.SetColumnCaptions(dgvPPHData, new Dictionary<string, string>
            {
                ["IEPPHValue"] = "IE PPH",
                ["IsSigned"] = "Production Sign",
                ["AdjustOperatorNo"] = "Adjust\nOperator",
                ["TargetOutputPC"] = "Target\nOutput",
                ["StageName"] = "Process",
                ["THTValue"] = "THT",
                ["TypeName"] = "Type",
                ["PersonIncharge"] = "Person\nIncharge"
            });

            // Đặt độ rộng cố định nếu cần
            GridViewHelper.SetColumnFixedWidth(dgvPPHData, new Dictionary<string, int>
            {
                ["NoteForPC"] = 220,
                ["ModelName"] = 220
            });
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
                MessageBoxHelper.ShowInfo("No data to export.");
                return;
            }

            var result = MessageBoxHelper.ShowConfirm("Do you want to include the TCT column in the export?", "Excel Export Options");
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

                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    () =>
                    {
                        var convertedData = iepphDataList
                            .Select(item => new IETotal_Model
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
                        return true;
                    },
                    _ => { },
                    "Exporting..."
                );

                MessageBoxHelper.ShowInfo("Excel export completed successfully!");

                try
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch
                {
                    MessageBoxHelper.ShowError("Cannot open exported file.");
                }
            }
        }
    }
}
