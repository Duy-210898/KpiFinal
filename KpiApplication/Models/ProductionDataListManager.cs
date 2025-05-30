using KpiApplication.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class ProductionDataListManager
{
    public ProductionDataListManager(List<ProductionData> initialData)
    {
        SetRawData(initialData);
    }

    public void SetRawData(List<ProductionData> newData)
    {
        RawData = new BindingList<ProductionData>(newData ?? new List<ProductionData>());
        ReMerge();
    }
    public bool TryGetBackupItems(int groupId, out List<ProductionData> backupList)
    {
        if (_backupStorage.TryGetValue(groupId, out var blist))
        {
            backupList = blist.ToList();
            return true;
        }
        backupList = null;
        return false;
    }

    public BindingList<ProductionData> MergedList { get; private set; } = new BindingList<ProductionData>();
    public BindingList<ProductionData> RawData { get; private set; } = new BindingList<ProductionData>();

    private readonly Dictionary<int, List<ProductionData>> _backupStorage = new Dictionary<int, List<ProductionData>>();

    private void ReMerge()
    {
        var newMerged = ProductionDataMerger.Merge(RawData, _backupStorage);

        MergedList.RaiseListChangedEvents = false;
        MergedList.Clear();

        foreach (var item in newMerged)
        {
            MergedList.Add(item);
        }

        MergedList.RaiseListChangedEvents = true;
        MergedList.ResetBindings();
    }

    public void MergeItems(List<ProductionData> items)
    {
        if (items == null || !items.Any()) return;

        int newGroupId = ProductionDataMerger.GenerateNewGroupID(RawData);
        ProductionDataMerger.MergeItems(items, newGroupId, _backupStorage);
        ReMerge();
    }

    public void UnmergeItems(int groupId, List<int> selectedIDs)
    {
        ProductionDataMerger.UnmergeItems(groupId, selectedIDs, RawData, _backupStorage);
        ReMerge();
    }
}
