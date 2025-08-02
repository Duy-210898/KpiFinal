using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using KpiApplication.Models;

namespace KpiApplication.Utils
{
    public static class IEPPHSelector
    {
        /// <summary>
        /// Trả về danh sách IETotal_Model duy nhất theo từng ArticleName + Process, ưu tiên theo TypeName
        /// </summary>
        public static BindingList<IETotal_Model> GetBestIEPPHRecords(IEnumerable<IETotal_Model> inputList)
        {
            if (inputList == null)
                return new BindingList<IETotal_Model>();

            var bestItems = inputList
                .GroupBy(x => new { x.ArticleName, x.Process })
                .Select(g => g
                    .OrderBy(x => GetTypePriority(x.TypeName))
                    .ThenBy(x => x.ModelName) // nếu cần thêm tiêu chí phụ
                    .First())
                .ToList();

            return new BindingList<IETotal_Model>(bestItems);
        }

        /// <summary>
        /// Xác định độ ưu tiên của TypeName
        /// </summary>
        public static int GetTypePriority(string typeName)
        {
            switch (typeName)
            {
                case "MassProduction":
                    return 0;
                case "First Production":
                    return 1;
                case "Production Trial":
                    return 2;
                default:
                    return 3;
            }
        }
    }
}
