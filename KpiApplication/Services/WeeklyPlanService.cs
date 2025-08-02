using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KpiApplication.Services
{
    public class WeeklyPlanService
    {
        private readonly WeeklyPlan_DAL weeklyPlan_DAL = new WeeklyPlan_DAL();

        public BindingList<WeeklyPlanData_Model> GetWeeklyPlanData()
        {
            return weeklyPlan_DAL.GetWeeklyPlanData();
        }

        public async Task<(int inserted, int duplicated)> ImportWeeklyPlanFilesAsync(string[] filePaths, Func<List<string>, string> selectSheetFunc)
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
                        continue;
                }
                catch
                {
                    continue;
                }

                string selectedSheet = selectSheetFunc(sheetNames);
                if (string.IsNullOrEmpty(selectedSheet))
                    continue;

                List<WeeklyPlanData_Model> importedData;
                try
                {
                    importedData = await Task.Run(() =>
                        ExcelImporter.ImportWeeklyPlanFromExcel(filePath, selectedSheet));
                }
                catch
                {
                    continue;
                }

                if (importedData == null || importedData.Count == 0)
                    continue;

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
                    weeklyPlan_DAL.BulkInsertArticles(newArticles);
                    foreach (var art in newArticles)
                        existingArticleNames.Add(art.ArticleName);
                }

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
    }
}
