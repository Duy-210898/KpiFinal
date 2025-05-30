using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucWeeklyPlan : DevExpress.XtraEditors.XtraUserControl
    {
        private readonly WeeklyPlan_DAL weeklyPlan_DAL = new WeeklyPlan_DAL();
        private BindingList<WeeklyPlanData> weeklyPlanDataList;

        public ucWeeklyPlan()
        {
            InitializeComponent();
            this.Load += ucWeeklyPlan_Load;
        }

        private BindingList<WeeklyPlanData> FetchData()
        {
            return weeklyPlan_DAL.GetWeeklyPlanData();
        }

        private void LoadDataToGrid(BindingList<WeeklyPlanData> data)
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
        private async void ucWeeklyPlan_Load(object sender, EventArgs e)
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                FetchData,
                data =>
                {
                    LoadDataToGrid(data);
                    ConfigureGridAfterDataBinding();
                },
                "Loading"
            );
        }

        private void ConfigureGridAfterDataBinding()
        {
            GridViewHelper.ApplyDefaultFormatting(dgvWeeklyPlan);
            GridViewHelper.ApplyRowStyleAlternateColors(dgvWeeklyPlan, Color.AliceBlue, Color.White);
            GridViewHelper.EnableWordWrapForGridView(dgvWeeklyPlan);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvWeeklyPlan);
            GridViewHelper.EnableCopyFunctionality(dgvWeeklyPlan);
        }
        private async Task<(int inserted, int duplicated)> ImportWeeklyPlanFilesAsync(string[] filePaths)
        {
            int totalInserted = 0;
            int totalDuplicated = 0;

            var existingKeys = new HashSet<(string ArticleName, string ModelName, int Week, int Month, int Year)>(
                weeklyPlan_DAL.GetAllKeys());

            var existingArticles = weeklyPlan_DAL.GetAllArticles();
            var existingArticleNames = existingArticles
                .Select(a => a.ArticleName ?? string.Empty)
                .ToHashSet();

            foreach (string filePath in filePaths)
            {
                var importedData = await Task.Run(() => ExcelImporter.ImportWeeklyPlanFromExcel(filePath));

                var newArticles = importedData
                    .Where(x => !string.IsNullOrWhiteSpace(x.ArticleName) && !existingArticleNames.Contains(x.ArticleName))
                    .Select(x => (ArticleName: x.ArticleName, ModelName: x.ModelName ?? string.Empty))
                    .Distinct()
                    .ToList();

                if (newArticles.Count > 0)
                {
                    var insertedArticles = weeklyPlan_DAL.BulkInsertArticles(newArticles);
                    var newArticleIDs = insertedArticles.Select(x => x.ArticleID).ToList();
                    var insertedIEIDs = weeklyPlan_DAL.InsertArticleIDsToIE_PPH_Data(newArticleIDs);
                    weeklyPlan_DAL.InsertIEIDsToProductionStages(insertedIEIDs);

                    foreach (var art in newArticles)
                        existingArticleNames.Add(art.ArticleName);
                }

                var newItems = importedData
                    .Where(item => !string.IsNullOrWhiteSpace(item.ArticleName)
                        && !existingKeys.Contains((
                            item.ArticleName,
                            item.ModelName ?? string.Empty,
                            item.Week ?? 0,
                            item.Month ?? 0,
                            item.Year ?? 0)))
                    .ToList();

                int duplicatedCount = importedData.Count - newItems.Count;
                int insertedCount = newItems.Count;

                foreach (var newItem in newItems)
                {
                    existingKeys.Add((
                        newItem.ArticleName ?? string.Empty,
                        newItem.ModelName ?? string.Empty,
                        newItem.Week ?? 0,
                        newItem.Month ?? 0,
                        newItem.Year ?? 0));
                }

                if (newItems.Count > 0)
                    weeklyPlan_DAL.BulkInsertWeeklyPlans(newItems);

                totalInserted += insertedCount;
                totalDuplicated += duplicatedCount;
            }

            return (totalInserted, totalDuplicated);
        }
        private async Task LoadDataAsync()
        {
            var data = await Task.Run(() => FetchData());
            LoadDataToGrid(data);
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
                    int inserted = 0, duplicated = 0;

                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        async () =>
                        {
                            (inserted, duplicated) = await ImportWeeklyPlanFilesAsync(openFileDialog.FileNames);
                        },
                        "Importing..."
                    );

                    XtraMessageBox.Show(
                        $"✔️ Đã nhập tổng cộng {inserted} dòng mới.\n⚠️ Bỏ qua tổng cộng {duplicated} dòng trùng.",
                        "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"Có lỗi xảy ra khi nhập dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

