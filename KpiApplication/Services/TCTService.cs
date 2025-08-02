using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace KpiApplication.Services
{
    public static class TCTService
    {
        public static List<TCTData_Model> GetAllTCTData()
        {
            return TCT_DAL.GetAllTCTData();
        }
        public static List<TCTData_Pivoted> Pivot(List<TCTData_Model> models)
        {
            return PivoteHelper.PivotTCTData(models);
        }
        public static (BindingList<TCTData_Pivoted> original, List<TCTData_Pivoted> snapshot) CreatePivotSnapshot(List<TCTData_Pivoted> pivoted)
        {
            var original = new BindingList<TCTData_Pivoted>(pivoted.Select(x => x.Clone()).ToList());
            var snapshot = pivoted.Select(x => x.Clone()).ToList();
            return (original, snapshot);
        }
        public static void Insert(TCTData_Pivoted row, string updatedBy)
        {
            var insertList = BuildInsertList(row);
            if (!insertList.Any())
            {
                insertList.Add(new TCTData_Model
                {
                    ModelName = row.ModelName,
                    Type = row.Type,
                    Process = null,
                    TCTValue = null,
                    Notes = row.Notes,
                    LastUpdatedAt = null
                });
            }

            var now = DateTime.Now;
            foreach (var item in insertList)
            {
                item.LastUpdatedAt = now;
                TCT_DAL.InsertOrUpdateTCT(item, updatedBy);
            }
        }
        public static bool IsAllFieldsNull(TCTData_Pivoted item)
        {
            return string.IsNullOrWhiteSpace(item.Type)
                && item.Cutting == null
                && item.Stitching == null
                && item.Assembly == null
                && item.StockFitting == null;
        }


        public static bool HasTCTChanged(TCTData_Pivoted oldRow, TCTData_Pivoted newRow)
        {
            return !AlmostEqual(oldRow.Cutting, newRow.Cutting)
                || !AlmostEqual(oldRow.Stitching, newRow.Stitching)
                || !AlmostEqual(oldRow.Assembly, newRow.Assembly)
                || !AlmostEqual(oldRow.StockFitting, newRow.StockFitting)
                || !StringEquals(oldRow.Notes, newRow.Notes)
                || !StringEquals(oldRow.ModelName, newRow.ModelName)
                || !StringEquals(oldRow.Type, newRow.Type);
        }

        public static void Update(TCTData_Pivoted row, TCTData_Pivoted oldRow, string updatedBy, out bool hasChanged)
        {
            var changes = new List<TCTData_Model>();
            bool changed = false;

            void AddIfChanged(string process, double? oldVal, double? newVal)
            {
                if (!AlmostEqual(oldVal, newVal))
                {
                    changes.Add(CreateModel(row, process, newVal));
                    changed = true;
                }
            }

            AddIfChanged("Cutting", oldRow.Cutting, row.Cutting);
            AddIfChanged("Stitching", oldRow.Stitching, row.Stitching);
            AddIfChanged("Assembly", oldRow.Assembly, row.Assembly);
            AddIfChanged("Stock Fitting", oldRow.StockFitting, row.StockFitting);

            bool metadataChanged = !StringEquals(oldRow.ModelName, row.ModelName)
                                || !StringEquals(oldRow.Type, row.Type)
                                || !StringEquals(oldRow.Notes, row.Notes);

            if (metadataChanged && changes.Count == 0)
            {
                foreach (var process in new[] { "Cutting", "Stitching", "Assembly", "Stock Fitting" })
                {
                    var val = GetProcessValue(row, process);
                    if (val.HasValue)
                    {
                        changes.Add(CreateModel(row, process, val));
                        changed = true;
                    }
                }
            }

            foreach (var item in changes)
            {
                TCT_DAL.InsertOrUpdateTCT(item, updatedBy);
            }

            hasChanged = changed;
        }

        private static TCTData_Model CreateModel(TCTData_Pivoted row, string process, double? value)
        {
            return new TCTData_Model
            {
                ModelName = row.ModelName,
                Type = row.Type,
                Process = process,
                TCTValue = value,
                LastUpdatedAt = DateTime.Now,
                Notes = row.Notes
            };
        }

        private static List<TCTData_Model> BuildInsertList(TCTData_Pivoted row)
        {
            var list = new List<TCTData_Model>();
            AddIfHasValue(list, row.ModelName, row.Type, "Cutting", row.Cutting, row.Notes);
            AddIfHasValue(list, row.ModelName, row.Type, "Stitching", row.Stitching, row.Notes);
            AddIfHasValue(list, row.ModelName, row.Type, "Assembly", row.Assembly, row.Notes);
            AddIfHasValue(list, row.ModelName, row.Type, "Stock Fitting", row.StockFitting, row.Notes);
            return list;
        }

        private static void AddIfHasValue(List<TCTData_Model> list, string modelName, string type, string process, double? value, string notes)
        {
            if (!string.IsNullOrWhiteSpace(modelName) && value.HasValue)
            {
                list.Add(new TCTData_Model
                {
                    ModelName = modelName,
                    Type = type,
                    Process = process,
                    TCTValue = value.Value,
                    Notes = notes
                });
            }
        }
        public static Dictionary<string, TCTData_Pivoted> BuildSnapshotLookup(IEnumerable<TCTData_Pivoted> snapshot)
        {
            return snapshot.ToDictionary(x => MakeKey(x.ModelName, x.Type), x => x);
        }

        public static HashSet<string> BuildModelTypeKeySet(IEnumerable<TCTData_Pivoted> data)
        {
            return new HashSet<string>(data.Select(x => MakeKey(x.ModelName, x.Type)));
        }
        public static TCTData_Pivoted GetSnapshotRow(Dictionary<string, TCTData_Pivoted> lookup, string modelName, string type)
        {
            lookup.TryGetValue(MakeKey(modelName, type), out var row);
            return row;
        }

        public static void UpdateSnapshotRow(
        Dictionary<string, TCTData_Pivoted> lookup,
        List<TCTData_Pivoted> snapshotList,
        TCTData_Pivoted updatedRow)
        {
            string oldKey = MakeKey(updatedRow.OriginalModelName, updatedRow.OriginalType);
            string newKey = MakeKey(updatedRow.ModelName, updatedRow.Type);

            if (lookup.TryGetValue(oldKey, out var snapshotRow))
            {
                snapshotRow.ModelName = updatedRow.ModelName;
                snapshotRow.Type = updatedRow.Type;
                snapshotRow.Cutting = updatedRow.Cutting;
                snapshotRow.Stitching = updatedRow.Stitching;
                snapshotRow.Assembly = updatedRow.Assembly;
                snapshotRow.StockFitting = updatedRow.StockFitting;
                snapshotRow.Notes = updatedRow.Notes;
                snapshotRow.LastUpdatedAt = updatedRow.LastUpdatedAt;

                if (!oldKey.Equals(newKey))
                {
                    lookup.Remove(oldKey);
                    lookup[newKey] = snapshotRow;
                }
            }
            else
            {
                var clone = updatedRow.Clone();
                snapshotList.Add(clone);
                lookup[newKey] = clone;
            }
        }
        public enum TCTUpdateResult
        {
            Inserted,
            Updated,
            Unchanged
        }
        public static async Task<bool> InsertAsync(TCTData_Pivoted row, string updatedBy)
        {
            try
            {
                await Task.Run(() => Insert(row, updatedBy));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> UpdateAsync(TCTData_Pivoted newRow, TCTData_Pivoted oldRow, string updatedBy)
        {
            try
            {
                return await Task.Run(() =>
                {
                    Update(newRow, oldRow, updatedBy, out bool changed);
                    return changed;
                });
            }
            catch
            {
                return false;
            }
        }
        public class TCTImportResult
        {
            public int Inserted { get; set; }
            public int Updated { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
        public static TCTImportResult ImportFromExcelFile(string filePath, string sheetName, string username)
        {
            List<string> errors;
            var unpivotedList = new List<TCTImport_Model>();
            var tctItems = ExcelImporter.ReadTCTItemsFromExcel(filePath, sheetName, out errors);

            foreach (var item in tctItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ModelName))
                    continue;

                AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Cutting", item.Cutting);
                AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Stitching", item.Stitching);
                AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Assembly", item.Assembly);
                AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Stock Fitting", item.Stockfitting);
            }

            if (unpivotedList.Count == 0)
                throw new Exception(Lang.NoValidTCTDataFound);

            var result = TCT_DAL.SaveTCTImportList(unpivotedList, username);
            return new TCTImportResult
            {
                Inserted = result.inserted,
                Updated = result.updated,
                Errors = errors
            };
        }

        public static TCTUpdateResult HandleInsertOrUpdate(
    TCTData_Pivoted updatedRow,
    HashSet<string> modelTypeKeySet,
    Dictionary<string, TCTData_Pivoted> snapshotLookup,
    List<TCTData_Pivoted> snapshotList,
    string updatedBy)
        {
            string newKey = MakeKey(updatedRow.ModelName, updatedRow.Type);
            string oldKey = MakeKey(updatedRow.OriginalModelName, updatedRow.OriginalType);
            var oldRow = GetSnapshotRow(snapshotLookup, updatedRow.OriginalModelName, updatedRow.OriginalType);

            bool isNew = !modelTypeKeySet.Contains(newKey) || oldRow == null;

            if (isNew)
            {
                Insert(updatedRow, updatedBy);
                modelTypeKeySet.Add(newKey);
                return TCTUpdateResult.Inserted;
            }

            if (HasTCTChanged(oldRow, updatedRow))
            {
                Update(updatedRow, oldRow, updatedBy, out bool changed);
                if (changed)
                {
                    UpdateSnapshotRow(snapshotLookup, snapshotList, updatedRow);
                    if (!oldKey.Equals(newKey))
                    {
                        modelTypeKeySet.Remove(oldKey);
                        modelTypeKeySet.Add(newKey);
                    }
                    return TCTUpdateResult.Updated;
                }
            }

            return TCTUpdateResult.Unchanged;
        }

        public static void AddIfHasValue(List<TCTImport_Model> list, string modelName, string type, string process, double? value)
        {
            if (value.HasValue)
            {
                list.Add(new TCTImport_Model
                {
                    ModelName = modelName,
                    Type = type,
                    Process = process,
                    TCT = value.Value
                });
            }
        }

        public static string MakeKey(string modelName, string type)
    => $"{modelName ?? ""}|{type ?? ""}";


        private static double? GetProcessValue(TCTData_Pivoted row, string process)
        {
            switch (process)
            {
                case "Cutting": return row.Cutting;
                case "Stitching": return row.Stitching;
                case "Assembly": return row.Assembly;
                case "Stock Fitting": return row.StockFitting;
                default: return null;
            }
        }

        private static bool AlmostEqual(double? a, double? b)
            => Math.Abs((a ?? 0) - (b ?? 0)) < 0.0001;

        private static bool StringEquals(string a, string b)
            => string.Equals(a?.Trim(), b?.Trim(), StringComparison.Ordinal);
    }
}
