using KpiApplication.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace KpiApplication.Utils
{
    public class PivoteHelper
    {
        public static List<TCTData_Pivoted> PivotTCTData(List<TCTData_Model> originalList)
        {
            var list = originalList
                .GroupBy(x => new { x.ModelName, x.Type })
                .Select(g =>
                {
                    var item = new TCTData_Pivoted
                    {
                        ModelName = g.Key.ModelName,
                        Type = g.Key.Type,
                        LastUpdatedAt = g.Max(x => x.LastUpdatedAt),
                        Notes = g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Notes))?.Notes
                    };

                    foreach (var entry in g)
                    {
                        var process = entry.Process?.Trim();
                        if (process == "Cutting") item.Cutting = entry.TCTValue;
                        else if (process == "Stitching") item.Stitching = entry.TCTValue;
                        else if (process == "Assembly") item.Assembly = entry.TCTValue;
                        else if (process == "Stock Fitting") item.StockFitting = entry.TCTValue;
                    }

                    return item;
                })
                .ToList();

            return new List<TCTData_Pivoted>(list);
        }

        public static BindingList<TCTData_Model> UnpivotTCTData(BindingList<TCTData_Pivoted> pivotedList)
        {
            var unpivoted = new List<TCTData_Model>();

            foreach (var item in pivotedList)
            {
                if (item.Cutting.HasValue)
                {
                    unpivoted.Add(new TCTData_Model
                    {
                        ModelName = item.ModelName,
                        Type = item.Type,
                        Process = "Cutting",
                        TCTValue = item.Cutting,
                        Notes = item.Notes,
                        LastUpdatedAt = item.LastUpdatedAt
                    });
                }

                if (item.Stitching.HasValue)
                {
                    unpivoted.Add(new TCTData_Model
                    {
                        ModelName = item.ModelName,
                        Type = item.Type,
                        Process = "Stitching",
                        TCTValue = item.Stitching,
                        Notes = item.Notes,
                        LastUpdatedAt = item.LastUpdatedAt
                    });
                }

                if (item.Assembly.HasValue)
                {
                    unpivoted.Add(new TCTData_Model
                    {
                        ModelName = item.ModelName,
                        Type = item.Type,
                        Process = "Assembly",
                        TCTValue = item.Assembly,
                        Notes = item.Notes,
                        LastUpdatedAt = item.LastUpdatedAt
                    });
                }

                if (item.StockFitting.HasValue)
                {
                    unpivoted.Add(new TCTData_Model
                    {
                        ModelName = item.ModelName,
                        Type = item.Type,
                        Process = "Stock Fitting",
                        TCTValue = item.StockFitting,
                        Notes = item.Notes,
                        LastUpdatedAt = item.LastUpdatedAt
                    });
                }
            }

            return new BindingList<TCTData_Model>(unpivoted);
        }
    }
}
