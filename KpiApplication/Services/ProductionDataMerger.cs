using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using KpiApplication.Models;
public class ProductionDataMerger
{
    public static BindingList<ProductionData> Merge(BindingList<ProductionData> rawList, Dictionary<int, List<ProductionData>> backupStorage)
    {
        if (rawList == null || rawList.Count == 0)
            return new BindingList<ProductionData>();

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

        return new BindingList<ProductionData>(finalList.ToList());
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
    public static ProductionData MergeItems(List<ProductionData> items, int groupId, Dictionary<int, List<ProductionData>> backupStorage)
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

    // Phương thức để unmerge các items
    private static ProductionData CreateMergedItem(List<ProductionData> items, int groupId)
    {
        var baseItem = items.OrderByDescending(x => x.Quantity ?? 0).First();
        var merged = baseItem.Clone();
        merged.ModelName = string.Join("\n", items.Select(x => x.ModelName).Where(x => !string.IsNullOrWhiteSpace(x)));
        merged.Article = string.Join("\n", items.Select(x => x.Article).Where(x => !string.IsNullOrWhiteSpace(x)));
        merged.LargestOutput = baseItem.ModelName; 
        merged.Quantity = items.Sum(x => x.Quantity ?? 0);
        merged.TotalWorker = items.Select(x => x.TotalWorker).FirstOrDefault(x => x.HasValue);
        merged.WorkingTime = items.Select(x => x.WorkingTime).FirstOrDefault(x => x.HasValue);
        merged.IEPPH = items.Select(x => x.IEPPH).FirstOrDefault(x => x.HasValue);
        merged.IsVisible = true;
        merged.IsMerged = true;
        merged.MergeGroupID = groupId;
        merged.Recalculate();
        return merged;
    }
    private static void BackupIfNeeded(int groupId, List<ProductionData> items, Dictionary<int, List<ProductionData>> backupStorage)
    {
        if (!backupStorage.ContainsKey(groupId))
        {
            backupStorage[groupId] = items.Select(x => x.Clone()).ToList();
        }
    }

    public static void UnmergeItems(int groupId, List<int> selectedIDs, BindingList<ProductionData> currentItems, Dictionary<int, List<ProductionData>> backupStorage)
    {
        if (!backupStorage.ContainsKey(groupId)) return;

        var originalItems = backupStorage[groupId];

        // Chỉ Unmerge các dòng được chọn trong group
        var itemsToUnmerge = currentItems
            .Where(item => item.MergeGroupID == groupId && selectedIDs.Contains(item.ProductionID))
            .ToList();

        foreach (var item in itemsToUnmerge)
        {
            var original = originalItems.FirstOrDefault(o => o.ProductionID == item.ProductionID);
            if (original != null)
            {
                System.Diagnostics.Debug.WriteLine($"Unmerge item ProductionID={item.ProductionID}, setting IsMerged=false");

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
                System.Diagnostics.Debug.WriteLine($"Warning: không tìm thấy original item cho ProductionID={item.ProductionID} trong backup");
            }
        }

        // Kiểm tra toàn bộ nhóm còn dòng nào IsMerged true không
        bool anyStillMerged = currentItems.Any(x => x.MergeGroupID == groupId && x.IsMerged);

        System.Diagnostics.Debug.WriteLine($"Nhóm {groupId} còn dòng IsMerged = true? {anyStillMerged}");

        if (!anyStillMerged)
        {
            backupStorage.Remove(groupId);
        }
    }
    public static int GenerateNewGroupID(BindingList<ProductionData> data)
    {
        return (data.Max(x => x.MergeGroupID ?? 0) + 1);
    }
}
