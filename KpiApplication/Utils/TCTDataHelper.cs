using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KpiApplication.Utils
{
    public static class TCTDataHelper
    {
        public static bool IsAllFieldsNull(TCTData_Pivoted item)
        {
            return string.IsNullOrWhiteSpace(item.Type)
                && !item.Cutting.HasValue
                && !item.Stitching.HasValue
                && !item.Assembly.HasValue
                && !item.StockFitting.HasValue;
        }

        public static List<TCTData_Model> GetUpdatedModels(
            TCTData_Pivoted oldRow,
            TCTData_Pivoted newRow,
            DateTime? updateTime = null)
        {
            var updated = new List<TCTData_Model>();
            string modelName = newRow.ModelName;
            string type = newRow.Type;
            string notes = newRow.Notes;
            DateTime now = updateTime ?? DateTime.Now;

            Action<string, double?, double?> addIfChanged = (process, oldVal, newVal) =>
            {
                if (oldVal != newVal)
                {
                    updated.Add(new TCTData_Model
                    {
                        ModelName = modelName,
                        Type = type,
                        Process = process,
                        TCTValue = newVal,
                        LastUpdatedAt = now,
                        Notes = notes
                    });
                }
            };

            addIfChanged("Cutting", oldRow.Cutting, newRow.Cutting);
            addIfChanged("Stitching", oldRow.Stitching, newRow.Stitching);
            addIfChanged("Assembly", oldRow.Assembly, newRow.Assembly);
            addIfChanged("Stock Fitting", oldRow.StockFitting, newRow.StockFitting);

            if ((notes ?? "") != (oldRow.Notes ?? "") && updated.Count == 0)
            {
                updated.Add(new TCTData_Model
                {
                    ModelName = modelName,
                    Type = type,
                    Process = "Cutting",
                    TCTValue = oldRow.Cutting,
                    LastUpdatedAt = now,
                    Notes = notes
                });
            }

            return updated;
        }
    }
}
