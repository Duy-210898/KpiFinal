using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KpiApplication.Services
{
    public static class ProductionDataMapper
    {
        public static List<ProductionData> MapFromReader(SqlDataReader reader)
        {
            var productionDataList = new List<ProductionData>();

            while (reader.Read())
            {
                var data = new ProductionData
                {
                    ProductionID = reader["ProductionID"] != DBNull.Value ? Convert.ToInt32(reader["ProductionID"]) : 0,
                    ScanDate = reader["SCAN_DATE"] != DBNull.Value ? (DateTime?)reader["SCAN_DATE"] : null,
                    DepartmentCode = reader["DEPARTMENT_CODE"] != DBNull.Value ? reader["DEPARTMENT_CODE"].ToString() : "",
                    LineName = reader["LineName"] != DBNull.Value ? reader["LineName"].ToString() : "",
                    Process = MapProcess(reader["Process"] != DBNull.Value ? reader["Process"].ToString() : ""),
                    Article = reader["ArticleName"] != DBNull.Value ? reader["ArticleName"].ToString() : "",
                    ModelName = reader["ModelName"] != DBNull.Value ? reader["ModelName"].ToString() : "",
                    TypeName = reader["TypeName"] != DBNull.Value ? reader["TypeName"].ToString() : "",
                    Quantity = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0,
                    TotalWorker = reader["TOTAL_WORKER"] != DBNull.Value ? (int?)Convert.ToInt32(reader["TOTAL_WORKER"]) : null,
                    IEPPH = reader["IE_PPH_Value"] != DBNull.Value ? (double?)Math.Round(Convert.ToDouble(reader["IE_PPH_Value"]), 1) : null,
                    WorkingTime = reader["Working_Time"] != DBNull.Value ? (double?)Math.Round(Convert.ToDouble(reader["Working_Time"]), 1) : null,
                    Factory = reader["Factory"] != DBNull.Value ? reader["Factory"].ToString() : "",
                    Plant = reader["Plant"] != DBNull.Value ? reader["Plant"].ToString() : "",
                    IsMerged = reader["IsMerged"] != DBNull.Value ? Convert.ToBoolean(reader["IsMerged"]) : false,
                    MergeGroupID = reader["MergeGroupID"] != DBNull.Value ? (int?)Convert.ToInt32(reader["MergeGroupID"]) : null
                };

                productionDataList.Add(data);
            }

            return productionDataList;
        }

        private static string MapProcess(string code)
        {
            switch (code)
            {
                case "A": return "Packing";
                case "C": return "Cutting";
                case "S": return "Stitching";
                case "L": return "Assembling";
                case "T": return "Stock Fitting";
                default: return code;
            }
        }
    }
}
