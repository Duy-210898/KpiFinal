using DevExpress.XtraEditors;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucViewTCTData : XtraUserControl, ISupportLoadAsync
    {
        private BindingList<TCTData_Model> tctData_Models = new BindingList<TCTData_Model>();
        private BindingList<TCTData_Pivoted> pivotedDataOriginal = new BindingList<TCTData_Pivoted>();

        public ucViewTCTData()
        {
            InitializeComponent();
            ApplyLocalizedText();
        }
        private void ApplyLocalizedText()
        {
            btnExport.Caption = Lang.Export;
            btnRefresh.Caption = Lang.Refresh;
        }

        public async Task LoadDataAsync()
        {
            try
            {
                UseWaitCursor = true;

                var result = await Task.Run(() => TCT_DAL.GetAllTCTData());
                tctData_Models = new BindingList<TCTData_Model>(result);

                var pivotedData = PivoteHelper.PivotTCTData(tctData_Models.ToList());
                pivotedDataOriginal = new BindingList<TCTData_Pivoted>(
                    pivotedData.Select(x => x.Clone()).ToList()
                );

                LoadDataToGrid(pivotedDataOriginal, true);
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
        private void LoadDataToGrid(BindingList<TCTData_Pivoted> data, bool editable)
        {
            gridControl.DataSource = data;
            TCTGridHelper.ApplyGridSettings(dgvTCT, editable);
            TCTGridHelper.SetCaptions(dgvTCT);
            TCTGridHelper.AutoAdjustColumnWidths(dgvTCT);
        }
        private void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
                saveDialog.Title = Lang.ExportTCTTitle;
                saveDialog.FileName = $"TCTData_{DateTime.Now:yyyyMMdd}.xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var originalData = (gridControl.DataSource as BindingList<TCTData_Pivoted>)?.ToList();

                        var filteredData = originalData
                            .Where(x => !IsAllFieldsNull(x))
                            .ToList();

                        gridControl.DataSource = filteredData;

                        dgvTCT.ExportToXlsx(saveDialog.FileName);

                        gridControl.DataSource = originalData;

                        MessageBoxHelper.ShowInfo(Lang.ExportSuccess);
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowError(Lang.ExportFailed, ex);
                    }
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
