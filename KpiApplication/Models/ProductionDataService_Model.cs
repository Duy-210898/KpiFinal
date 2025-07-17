using KpiApplication.Controls;
using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class ProductionDataService_Model
{
    private readonly ProductionBackupService _backupService = new ProductionBackupService();
    private int _nextGroupId;

    public BindingList<ProductionData_Model> RawData { get; private set; }
    public BindingList<ProductionData_Model> MergedList { get; private set; } = new BindingList<ProductionData_Model>();

    public event EventHandler MergedListChanged;

    public ProductionDataService_Model(List<ProductionData_Model> initialData)
    {
        SetRawData(initialData);
        _nextGroupId = (RawData?.Max(d => d.MergeGroupID) ?? 0) + 1;
    }
    private void BackupRawData()
    {
        var mergedGroups = RawData
            .Where(d => d.IsMerged && d.MergeGroupID.HasValue) 
            .GroupBy(d => d.MergeGroupID.Value);

        foreach (var group in mergedGroups)
        {
            int groupId = group.Key;
            var items = group.ToList();
            _backupService.AddBackup(groupId, items);
        }
    }

    public void SetRawData(List<ProductionData_Model> newData)
    {
        RawData = new BindingList<ProductionData_Model>(newData ?? new List<ProductionData_Model>());

        BackupRawData(); 

        ReMerge();
    }
    public void MergeItemsFast(List<ProductionData_Model> items)
    {
        if (items == null || items.Count == 0) return;

        int groupId = _nextGroupId++;
        _backupService.AddBackup(groupId, items);

        foreach (var item in items)
        {
            item.SetMergeInfoSilently(true, groupId, false);
        }
    }


    public void MergeItems(List<ProductionData_Model> items)
    {
        if (items == null || items.Count == 0) return;

        int groupId = _nextGroupId++;
        _backupService.AddBackup(groupId, items);

        foreach (var item in items)
        {
            item.IsMerged = true;
            item.MergeGroupID = groupId;
            item.IsVisible = false;
        }

        ReMerge();
    }

    public void UnmergeItems(int groupId, List<int> selectedIDs)
    {
        if (!_backupService.TryGetBackup(groupId, out var backup)) return;

        foreach (var item in RawData.Where(x => x.MergeGroupID == groupId && selectedIDs.Contains(x.ProductionID)))
        {
            var original = backup.FirstOrDefault(b => b.ProductionID == item.ProductionID);
            if (original != null)
            {
                item.CopyFrom(original);
                item.IsMerged = false;
                item.IsVisible = true;
                item.MergeGroupID = null;
            }
        }

        // Nếu không còn dòng nào merged thuộc group, xóa backup
        if (!RawData.Any(x => x.MergeGroupID == groupId && x.IsMerged))
            _backupService.RemoveBackup(groupId);

        ReMerge();
    }

    public void ReMerge()
    {
        var mergedItems = RawData.Where(x => x.IsMerged)
            .GroupBy(x => x.MergeGroupID)
            .Select(g => CreateMergedItem(g.ToList(), g.Key ?? 0));

        var unmergedItems = RawData.Where(x => !x.IsMerged && x.IsVisible);

        // Tính IE_Target cho từng dòng
        foreach (var item in mergedItems.Concat(unmergedItems))
        {
            CalculateIETarget(item);
        }

        var newList = mergedItems.Concat(unmergedItems)
            .OrderBy(x => x.ScanDate)
            .ThenBy(x => x.Factory)
            .ThenBy(x => ExtractLinePrefix(x.LineName))
            .ThenBy(x => ExtractLineNumber(x.LineName))
            .ToList();

        MergedList.RaiseListChangedEvents = false;
        MergedList.Clear();
        foreach (var item in newList) MergedList.Add(item);
        MergedList.RaiseListChangedEvents = true;
        MergedList.ResetBindings();

        MergedListChanged?.Invoke(this, EventArgs.Empty);
    }
    private ProductionData_Model CreateMergedItem(List<ProductionData_Model> group, int groupId)
    {
        var baseItem = group.OrderByDescending(x => x.Quantity ?? 0).First();

        // Ưu tiên lấy IEPPH từ baseItem, nếu không có thì tìm theo Quantity giảm dần
        var iepphSourceItem = baseItem.IEPPH.HasValue
            ? baseItem
            : group.Where(x => x.IEPPH.HasValue && x != baseItem)
                   .OrderByDescending(x => x.Quantity ?? 0)
                   .FirstOrDefault();

        var mergedItem = new ProductionData_Model
        {
            ProductionID = baseItem.ProductionID,
            ScanDate = baseItem.ScanDate,
            Factory = baseItem.Factory,
            Plant = baseItem.Plant,
            Process = baseItem.Process,
            LineName = baseItem.LineName,

            ModelName = string.Join("\n", group.Select(x => x.ModelName).Where(s => !string.IsNullOrWhiteSpace(s))),
            Article = string.Join("\n", group.Select(x => x.Article).Where(s => !string.IsNullOrWhiteSpace(s))),
            LargestOutput = baseItem.ModelName,

            Quantity = group.Sum(x => x.Quantity ?? 0),
            TotalWorker = baseItem.TotalWorker,
            WorkingTime = baseItem.WorkingTime,

            IEPPH = iepphSourceItem?.IEPPH,
            TypeName = iepphSourceItem?.TypeName,

            IsVisible = true,
            IsMerged = true,
            MergeGroupID = groupId
        };

        CalculateIETarget(mergedItem);

        return mergedItem;
    }

    private void CalculateIETarget(ProductionData_Model item)
    {
        // Tính TargetIE
        if (item.IEPPH.HasValue && item.TotalWorker.HasValue && item.WorkingTime.HasValue)
        {
            item.TargetIE = (int)(item.IEPPH.Value * item.TotalWorker.Value * item.WorkingTime.Value);
        }
        else
        {
            item.TargetIE = null;
        }

        // Tính OutputRate (Rate) = Quantity / TargetIE
        if (item.Quantity.HasValue && item.TargetIE.HasValue && item.TargetIE.Value > 0)
        {
            item.OutputRateValue = (double)item.Quantity.Value / item.TargetIE.Value;
        }
        else
        {
            item.OutputRateValue = null;
        }

        // Tính PPHRate = ActualPPH / IEPPH
        if (item.ActualPPH.HasValue && item.IEPPH.HasValue && item.IEPPH.Value != 0)
        {
            item.PPHRateValue = item.ActualPPH.Value / item.IEPPH.Value;
        }
        else
        {
            item.PPHRateValue = null;
        }
    }

    private string ExtractLinePrefix(string lineName) =>
        string.IsNullOrEmpty(lineName) ? "" : new string(lineName.TakeWhile(c => !char.IsDigit(c)).ToArray());

    private int ExtractLineNumber(string lineName)
    {
        if (string.IsNullOrEmpty(lineName)) return 0;
        var number = new string(lineName.SkipWhile(c => !char.IsDigit(c)).ToArray());
        return int.TryParse(number, out int result) ? result : 0;
    }
}
