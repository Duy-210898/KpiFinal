using DevExpress.XtraEditors;
using KpiApplication.DataAccess;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucViewTCTData : XtraUserControl
    {
        private List<TCTData_Model> tctData_Models = new List<TCTData_Model>();
        private List<TCTData_Pivoted> pivotedDataOriginal;

        public ucViewTCTData()
        {
            InitializeComponent();
        }

        private async void ucTCTData_Load(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                () => TCT_DAL.GetAllTCTData(),
                data =>
                {
                    tctData_Models = data;
                    pivotedDataOriginal = PivoteHelper.PivotTCTData(tctData_Models)
                        .Select(x => x.Clone()).ToList();

                    LoadDataToGrid(pivotedDataOriginal);
                },
                "Loading..."
            );
        }

        private void LoadDataToGrid(List<TCTData_Pivoted> data)
        {
            gridControl.DataSource = data;
            dgvTCT.OptionsView.EnableAppearanceEvenRow = true;
            dgvTCT.OptionsView.EnableAppearanceOddRow = true;
            dgvTCT.OptionsBehavior.Editable = false;
            GridViewHelper.ApplyDefaultFormatting(dgvTCT);
            GridViewHelper.EnableWordWrapForGridView(dgvTCT);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvTCT);
            GridViewHelper.EnableCopyFunctionality(dgvTCT);
            dgvTCT.BestFitColumns();
        }

        private void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
                saveDialog.Title = "Export TCT Data to Excel";
                saveDialog.FileName = $"TCTData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (saveDialog.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var originalData = gridControl.DataSource as List<TCTData_Pivoted>;
                    if (originalData == null) return;

                    var filteredData = originalData
                        .Where(x => !IsAllFieldsNull(x))
                        .ToList();

                    gridControl.DataSource = filteredData;

                    dgvTCT.ExportToXlsx(saveDialog.FileName);

                    // Restore original data after export
                    gridControl.DataSource = originalData;

                    MessageBoxHelper.ShowInfo("✅ Excel export successful!");
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError("❌ Error during Excel export", ex);
                }
            }
        }

        private bool IsAllFieldsNull(TCTData_Pivoted item) =>
            string.IsNullOrWhiteSpace(item.Type)
            && item.Cutting == null
            && item.Stitching == null
            && item.Assembly == null
            && item.StockFitting == null;
    }
}
