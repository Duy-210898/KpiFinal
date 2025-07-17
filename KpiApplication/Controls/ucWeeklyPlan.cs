using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using KpiApplication.Utils;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucWeeklyPlan : DevExpress.XtraEditors.XtraUserControl
    {
        private readonly WeeklyPlan_DAL weeklyPlan_DAL = new WeeklyPlan_DAL();
        private BindingList<WeeklyPlanData_Model> weeklyPlanDataList;

        public ucWeeklyPlan()
        {
            InitializeComponent();
            this.Load += ucWeeklyPlan_Load;
        }

        private BindingList<WeeklyPlanData_Model> FetchData()
        {
            return weeklyPlan_DAL.GetWeeklyPlanData();
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
            GridViewHelper.EnableWordWrapForGridView(dgvWeeklyPlan);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvWeeklyPlan);
            GridViewHelper.EnableCopyFunctionality(dgvWeeklyPlan);
        }
        private async Task<(int inserted, int duplicated)> ImportWeeklyPlanFilesAsync(string[] filePaths)
        {
            int totalInserted = 0;
            int totalDuplicated = 0;

            var existingKeys = new HashSet<(string, string, int, int, int)>(weeklyPlan_DAL.GetAllKeys());
            var existingArticleNames = weeklyPlan_DAL.GetAllArticles()
                .Select(a => a.ArticleName ?? string.Empty)
                .ToHashSet();

            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);

                List<string> sheetNames;
                try
                {
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        sheetNames = package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
                    }

                    if (!sheetNames.Any())
                    {
                        MessageBoxHelper.ShowWarning(
                            $"⚠️ File '{fileName}' does not contain any sheets.");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError(
                        $"❌ Unable to read Excel file '{fileName}':\n{ex.Message}");
                    continue;
                }
                // Cho người dùng chọn sheet
                string selectedSheet = null;
                using (var form = new KpiApplication.Forms.SheetSelectionForm(sheetNames))
                {
                    if (form.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(form.SelectedSheet))
                        continue;

                    selectedSheet = form.SelectedSheet;
                }

                List<WeeklyPlanData_Model> importedData;
                try
                {
                    importedData = await Task.Run(() =>
                        ExcelImporter.ImportWeeklyPlanFromExcel(filePath, selectedSheet));
                }
                // Sheet read error
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError(
                        $"❌ Error reading sheet '{selectedSheet}' in file '{fileName}'", ex);
                    continue;
                }

                if (importedData == null || importedData.Count == 0)
                {
                    MessageBoxHelper.ShowWarning(
                        $"⚠️ Sheet '{selectedSheet}' in file '{fileName}' contains no valid data.");
                    continue;
                }
                var newArticles = importedData
                    .Where(x => !string.IsNullOrWhiteSpace(x.ArticleName) && !existingArticleNames.Contains(x.ArticleName))
                    .GroupBy(x => x.ArticleName)
                    .Select(g => (
                        ArticleName: g.Key,
                        ModelName: g.FirstOrDefault()?.ModelName ?? string.Empty
                    ))
                    .ToList();

                if (newArticles.Any())
                {
                    var insertedArticles = weeklyPlan_DAL.BulkInsertArticles(newArticles);
                    foreach (var art in newArticles)
                        existingArticleNames.Add(art.ArticleName);
                }

                // Lọc các dòng chưa có key trùng
                var newItems = importedData
                    .Where(item => !string.IsNullOrWhiteSpace(item.ArticleName))
                    .Where(item =>
                    {
                        var key = (
                            item.ArticleName ?? string.Empty,
                            item.ModelName ?? string.Empty,
                            item.Week ?? 0,
                            item.Month ?? 0,
                            item.Year ?? 0
                        );
                        if (existingKeys.Contains(key)) return false;

                        existingKeys.Add(key);
                        return true;
                    })
                    .ToList();

                int insertedCount = newItems.Count;
                int duplicatedCount = importedData.Count - insertedCount;

                if (insertedCount > 0)
                    weeklyPlan_DAL.BulkInsertWeeklyPlans(newItems);

                totalInserted += insertedCount;
                totalDuplicated += duplicatedCount;
            }

            return (totalInserted, totalDuplicated);
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

                    (inserted, duplicated) = await ImportWeeklyPlanFilesAsync(openFileDialog.FileNames);

                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        FetchData,
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

