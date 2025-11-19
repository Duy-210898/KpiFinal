using DevExpress.XtraGrid.Columns;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace KpiApplication.Controls
{
    public partial class ucProductionSchedules : DevExpress.XtraEditors.XtraUserControl, ISupportLoadAsync
    {
        private BindingList<ProductionSchedules_Model> productionSchedules;
        private ProductionSchedules_DAL productionSchedule_DAL = new ProductionSchedules_DAL();
        private OverlayHelper overlayHelper;
        public ucProductionSchedules()
        {
            InitializeComponent();
        }
        public async Task LoadDataAsync()
        {
            try
            {
                UseWaitCursor = true;

                var data = await Task.Run(() => FetchData());
                LoadDataToGrid(data);
                ConfigureGridAfterDataBinding();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.LoadDataError, ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }
        private BindingList<ProductionSchedules_Model> FetchData()
        {
            var data = productionSchedule_DAL.GetAllProductionSchedules();

            productionSchedules = data;
            return data;
        }
        private void LoadDataToGrid(BindingList<ProductionSchedules_Model> data)
        {
            productionSchedules = data;
            gridProductionSchedules.DataSource = productionSchedules;

        }
        private void ConfigureGridAfterDataBinding()
        {
            dgvProductionSchedules.LayoutChanged();
            dgvProductionSchedules.OptionsView.ColumnAutoWidth = false;
            dgvProductionSchedules.HorzScrollVisibility = DevExpress.XtraGrid.Views.Base.ScrollVisibility.Always;

            Task.Delay(100).ContinueWith(_ =>
            {
                BeginInvoke(new Action(() =>
                {
                    // Tự động giãn cột theo nội dung
                    dgvProductionSchedules.BestFitColumns();
                }));
            });

            dgvProductionSchedules.NewItemRowText = Lang.AddNewRowHint;
        }
    }
}
