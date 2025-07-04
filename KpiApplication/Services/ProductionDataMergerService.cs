using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using KpiApplication.Models;
public class ProductionDataMergeráaaaa
{
    public static BindingList<ProductionData_Model> Merge(BindingList<ProductionData_Model> rawList, Dictionary<int, List<ProductionData_Model>> backupStorage)
    {
        if (rawList == null || rawList.Count == 0) 
            return new BindingList<ProductionData_Model>();

        var mergedList = rawList
            .Where(d => d.IsMerged == true)
            .GroupBy(d => d.MergeGroupID)
            .Select(g =>
            {
                var sorted = g.OrderByDescending(x => x.Quantity ?? 0).ToList();
                int groupId = sorted.First().MergeGroupID ?? 0;

                BackupIfNeeded(groupId, sorted, backupStorage);
                return CreateMergedItem(sorted, groupId);
            });

        var unmergedList = rawList.Where(d => d.IsMerged != true && d.IsVisible == true);

        var finalList = mergedList
            .Concat(unmergedList)
            .OrderBy(x => x.ScanDate)
            .ThenBy(x => x.Factory)
            .ThenBy(x => ExtractPrefix(x.LineName))
            .ThenBy(x => ExtractSuffixNumber(x.LineName));

        return new BindingList<ProductionData_Model>(finalList.ToList());
    }

    private static string ExtractPrefix(string lineName)
    {
        if (string.IsNullOrEmpty(lineName)) return "";
        return new string(lineName.TakeWhile(c => !char.IsDigit(c)).ToArray());
    }

    private static int ExtractSuffixNumber(string lineName)
    {
        if (string.IsNullOrEmpty(lineName)) return 0;
        var numberPart = new string(lineName.SkipWhile(c => !char.IsDigit(c)).ToArray());
        return int.TryParse(numberPart, out int number) ? number : 0;
    }


    // Phương thức để merge các items
    public static ProductionData_Model MergeItems(List<ProductionData_Model> items, int groupId, Dictionary<int, List<ProductionData_Model>> backupStorage)
    {
        if (!items.Any()) return null;

        BackupIfNeeded(groupId, items, backupStorage);

        foreach (var item in items)
        {
            item.IsMerged = true;
            item.MergeGroupID = groupId;
            item.IsVisible = false;
        }

        var mergedItem = CreateMergedItem(items, groupId);
        return mergedItem;
    }

    private static ProductionData_Model CreateMergedItem(List<ProductionData_Model> items, int groupId)
    {
        var baseItem = items.OrderByDescending(x => x.Quantity ?? 0).First();
        var merged = baseItem.Clone();
        merged.ModelName = string.Join("\n", items.Select(x => x.ModelName).Where(x => !string.IsNullOrWhiteSpace(x)));
        merged.Article = string.Join("\n", items.Select(x => x.Article).Where(x => !string.IsNullOrWhiteSpace(x)));
        merged.LargestOutput = baseItem.ModelName;
        merged.Quantity = items.Sum(x => x.Quantity ?? 0);
        merged.TotalWorker = items.Select(x => x.TotalWorker).FirstOrDefault(x => x.HasValue);
        merged.WorkingTime = items.Select(x => x.WorkingTime).FirstOrDefault(x => x.HasValue);

        // 🛠 Xử lý IEPPH: ưu tiên dòng có Quantity lớn nhất, nếu null thì tìm dòng khác
        merged.IEPPH = baseItem.IEPPH ?? items
            .Where(x => x.IEPPH.HasValue && x != baseItem)
            .OrderByDescending(x => x.Quantity ?? 0)
            .Select(x => x.IEPPH)
            .FirstOrDefault();

        merged.IsVisible = true;
        merged.IsMerged = true;
        merged.MergeGroupID = groupId;
        return merged;
    }
    private static void BackupIfNeeded(int groupId, List<ProductionData_Model> items, Dictionary<int, List<ProductionData_Model>> backupStorage)
    {
        if (!backupStorage.ContainsKey(groupId))
        {
            backupStorage[groupId] = items.Select(x => x.Clone()).ToList();
        }
    }

    public static void UnmergeItems(int groupId, List<int> selectedIDs, BindingList<ProductionData_Model> currentItems, Dictionary<int, List<ProductionData_Model>> backupStorage)
    {
        if (!backupStorage.ContainsKey(groupId))
        {
            Debug.WriteLine($"Backup for groupId {groupId} not found. Cannot unmerge.");
            return;
        }

        var originalItems = backupStorage[groupId];
        var currentGroupItems = currentItems.Where(item => item.MergeGroupID == groupId).ToList();

        // 🔎 Kiểm tra số lượng dòng trong backup và hiện tại
        if (originalItems.Count != currentGroupItems.Count)
        {
            Debug.WriteLine($"Warning: Backup count ({originalItems.Count}) and current items count ({currentGroupItems.Count}) for groupId {groupId} do not match.");
        }

        var itemsToUnmerge = currentGroupItems
            .Where(item => selectedIDs.Contains(item.ProductionID))
            .ToList();

        foreach (var item in itemsToUnmerge)
        {
            var original = originalItems.FirstOrDefault(o => o.ProductionID == item.ProductionID);
            if (original != null)
            {
                Debug.WriteLine($"Unmerging item ProductionID={item.ProductionID}, resetting fields.");
                item.ModelName = original.ModelName;
                item.Article = original.Article;
                item.LargestOutput = original.LargestOutput;
                item.Quantity = original.Quantity;
                item.TotalWorker = original.TotalWorker;
                item.WorkingTime = original.WorkingTime;
                item.IEPPH = original.IEPPH;
                item.IsMerged = false;
                item.IsVisible = true;
                item.MergeGroupID = null;
            }
            else
            {
                Debug.WriteLine($"Warning: Could not find original data for ProductionID={item.ProductionID} in backup.");
            }
        }

        // Nếu không còn dòng nào được merge, xóa backup để giải phóng bộ nhớ
        bool anyStillMerged = currentItems.Any(x => x.MergeGroupID == groupId && x.IsMerged);
        if (!anyStillMerged)
        {
            backupStorage.Remove(groupId);
        }
    }
    public static int GenerateNewGroupID(BindingList<ProductionData_Model> data)
    {
        return (data.Max(x => x.MergeGroupID ?? 0) + 1);
    }
}