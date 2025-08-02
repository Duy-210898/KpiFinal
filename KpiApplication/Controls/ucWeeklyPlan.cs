using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using KpiApplication.Common;
using KpiApplication.Forms;
using KpiApplication.Models;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucWeeklyPlan : XtraUserControl, ISupportLoadAsync
    {
        private readonly WeeklyPlanService weeklyPlanService = new WeeklyPlanService();
        private BindingList<WeeklyPlanData_Model> weeklyPlanDataList;

        public ucWeeklyPlan()
        {
            InitializeComponent();
        }

        private void LoadDataToGrid(BindingList<WeeklyPlanData_Model> data)
        {
            weeklyPlanDataList = data;
            gridControl1.DataSource = weeklyPlanDataList;
            gridControl1.UseEmbeddedNavigator = true;

            dgvWeeklyPlan.BeginUpdate();
            try
            {
                dgvWeeklyPlan.ClearGrouping();
                dgvWeeklyPlan.GroupCount = 3;
                dgvWeeklyPlan.Columns["Year"].GroupIndex = 0;
                dgvWeeklyPlan.Columns["Month"].GroupIndex = 1;
                dgvWeeklyPlan.Columns["Week"].GroupIndex = 2;

                dgvWeeklyPlan.CollapseAllGroups();
                dgvWeeklyPlan.ExpandGroupLevel(0);
                dgvWeeklyPlan.ExpandGroupLevel(1);
            }
            finally
            {
                dgvWeeklyPlan.EndUpdate();
            }
        }

        private void ConfigureGridAfterDataBinding()
        {
            GridViewHelper.ApplyDefaultFormatting(dgvWeeklyPlan);
            GridViewHelper.EnableWordWrapForGridView(dgvWeeklyPlan);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvWeeklyPlan);
            GridViewHelper.EnableCopyFunctionality(dgvWeeklyPlan);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                UseWaitCursor = true;

                var data = await Task.Run(() => weeklyPlanService.GetWeeklyPlanData());

                LoadDataToGrid(data);
                ConfigureGridAfterDataBinding();
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


        private async void btnImport_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = "C:\\",
                Filter = "Excel Files|*.xlsx",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    var (inserted, duplicated) = await weeklyPlanService.ImportWeeklyPlanFilesAsync(
                        openFileDialog.FileNames,
                        sheetNames =>
                        {
                            using (var form = new SheetSelectionForm(sheetNames))
                            {
                                if (form.ShowDialog() == DialogResult.OK)
                                    return form.SelectedSheet;
                            }
                            return null;
                        });

                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        () => weeklyPlanService.GetWeeklyPlanData(),
                        data =>
                        {
                            LoadDataToGrid(data);
                            ConfigureGridAfterDataBinding();
                        },
                        "Reloading data..."
                    );

                    MessageBoxHelper.ShowInfo(
                        $"✔️ Successfully imported {inserted} new rows.\n⚠️ Skipped {duplicated} duplicated rows.");
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError("❌ Error during data import", ex);
                }
            }
        }
    }
}
