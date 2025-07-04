using KpiApplication.Models;
using System.Collections.Generic;
using System.Linq;

namespace KpiApplication.Utils
{
    public class PivoteHelper
    {
        public static List<TCTData_Pivoted> PivotTCTData(List<TCTData_Model> originalList)
        {
            return originalList
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
        }
    }
}
