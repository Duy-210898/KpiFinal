using KpiApplication.Common;
using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace KpiApplication.Services
{
    public static class ProductionDataMapperService
    {
        // Cache map process -> string để tránh gọi Lang nhiều lần
        private static readonly Dictionary<string, string> ProcessMap = new Dictionary<string, string>()
        {
            { "A", Lang.Packing },
            { "C", Lang.Cutting },
            { "S", Lang.Stitching },
            { "L", Lang.Assembly },
            { "AC", Lang.AutoCutting },
            { "CS", Lang.ComputerStitching },
            { "T", Lang.StockFitting }
        };

        public static ProductionData_Model MapFromReaderSingleRow(SqlDataReader reader)
        {
            return new ProductionData_Model
            {
                ProductionID = reader["ProductionID"] != DBNull.Value ? Convert.ToInt32(reader["ProductionID"]) : 0,
                ScanDate = reader["SCAN_DATE"] != DBNull.Value ? (DateTime?)reader["SCAN_DATE"] : null,
                DepartmentCode = reader["DEPARTMENT_CODE"] != DBNull.Value ? reader["DEPARTMENT_CODE"].ToString() : "",
                Process = reader["Process"] != DBNull.Value ? MapProcess(reader["Process"].ToString()) : "",
                Factory = reader["Factory"] != DBNull.Value ? reader["Factory"].ToString() : "",
                Plant = reader["Plant"] != DBNull.Value ? Lang.Plant + " " + reader["Plant"].ToString() : "",
                LineName = reader["LineName"] != DBNull.Value ? reader["LineName"].ToString() : "",
                Article = reader["ArticleName"] != DBNull.Value ? reader["ArticleName"].ToString() : "",
                ModelName = reader["ModelName"] != DBNull.Value ? reader["ModelName"].ToString() : "",
                Quantity = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0,
                TotalWorker = reader["TOTAL_WORKER"] != DBNull.Value ? (int?)Convert.ToInt32(reader["TOTAL_WORKER"]) : null,
                WorkingTime = reader["Working_Time"] != DBNull.Value ? (double?)Math.Round(Convert.ToDouble(reader["Working_Time"]), 1) : null,
                TargetOfPC = reader["Target"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Target"]) : null,
                OperatorAdjust = reader["AdjustOperator"] != DBNull.Value ? (int?)Convert.ToInt32(reader["AdjustOperator"]) : null,
                IsMerged = reader["IsMerged"] != DBNull.Value ? Convert.ToBoolean(reader["IsMerged"]) : false,
                TypeName = reader["TypeName"] != DBNull.Value ? reader["TypeName"].ToString() : "",
                MergeGroupID = reader["MergeGroupID"] != DBNull.Value ? (int?)Convert.ToInt32(reader["MergeGroupID"]) : null,
            };
        }

        private static string MapProcess(string code) =>
            ProcessMap.TryGetValue(code, out var result) ? result : code;
    }
}
