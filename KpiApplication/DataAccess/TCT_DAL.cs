using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Diagnostics;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.ComponentModel;

namespace KpiApplication.DataAccess
{
    public class TCT_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        public static (int updated, int inserted) SaveTCTImportList(List<TCTImport_Model> list, string updatedBy)
        {
            if (list == null || list.Count == 0) return (0, 0);

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Tạo bảng tạm
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = @"
IF OBJECT_ID('tempdb..#TempTCTData') IS NOT NULL
    DROP TABLE #TempTCTData;

CREATE TABLE #TempTCTData (
    ModelName NVARCHAR(200),
    TypeName NVARCHAR(200),
    Process NVARCHAR(200),
    TCTValue FLOAT,
    Notes NVARCHAR(MAX)
);";
                            cmd.ExecuteNonQuery();
                        }

                        // Đổ dữ liệu vào bảng tạm
                        var table = new DataTable();
                        table.Columns.Add("ModelName", typeof(string));
                        table.Columns.Add("TypeName", typeof(string));
                        table.Columns.Add("Process", typeof(string));
                        table.Columns.Add("TCTValue", typeof(double));
                        table.Columns.Add("Notes", typeof(string));

                        var distinctList = list
                            .GroupBy(x => new { x.ModelName, x.Process })
                            .Select(g =>
                            {
                                var lastItem = g.Last();
                                return new TCTImport_Model
                                {
                                    ModelName = g.Key.ModelName,
                                    Process = g.Key.Process,
                                    Type = lastItem.Type,
                                    TCT = lastItem.TCT,
                                    Notes = lastItem.Notes
                                };
                            }).ToList();

                        foreach (var item in distinctList)
                        {
                            table.Rows.Add(
                                item.ModelName ?? (object)DBNull.Value,
                                item.Type ?? (object)DBNull.Value,
                                item.Process ?? (object)DBNull.Value,
                                item.TCT ?? (object)DBNull.Value,
                                item.Notes ?? (object)DBNull.Value);
                        }

                        using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
                        {
                            bulkCopy.DestinationTableName = "#TempTCTData";
                            bulkCopy.ColumnMappings.Add("ModelName", "ModelName");
                            bulkCopy.ColumnMappings.Add("TypeName", "TypeName");
                            bulkCopy.ColumnMappings.Add("Process", "Process");
                            bulkCopy.ColumnMappings.Add("TCTValue", "TCTValue");
                            bulkCopy.ColumnMappings.Add("Notes", "Notes");

                            bulkCopy.WriteToServer(table);
                        }

                        int updatedCount = 0;
                        int insertedCount = 0;

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = @"
UPDATE T
SET 
    T.TypeName = S.TypeName,
    T.TCTValue = S.TCTValue,
    T.Notes = S.Notes,
    T.UpdatedAt = GETDATE(),
    T.UpdatedBy = @UpdatedBy
FROM TCTData T
INNER JOIN #TempTCTData S
    ON T.ModelName = S.ModelName AND T.Process = S.Process;";
                            cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                            updatedCount = cmd.ExecuteNonQuery();
                        }

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = @"
INSERT INTO TCTData (ModelName, TypeName, Process, TCTValue, Notes, CreatedBy, CreatedAt)
SELECT 
    S.ModelName, S.TypeName, S.Process, S.TCTValue, S.Notes, @UpdatedBy, GETDATE()
FROM #TempTCTData S
WHERE NOT EXISTS (
    SELECT 1 FROM TCTData T
    WHERE T.ModelName = S.ModelName AND T.Process = S.Process
);";
                            cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                            insertedCount = cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return (updatedCount, insertedCount);
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }
        public static void InsertOrUpdateTCT(TCTData_Model data, string updatedBy)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                bool recordExists;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT COUNT(*) 
                FROM TCTData 
                WHERE RTRIM(ModelName) = RTRIM(@ModelName)
                  AND RTRIM(TypeName) = RTRIM(@TypeName)
                  AND RTRIM(Process) = RTRIM(@Process)";
                    cmd.Parameters.AddWithValue("@ModelName", (data.ModelName ?? "").Trim());
                    cmd.Parameters.AddWithValue("@TypeName", (data.Type ?? "").Trim());
                    cmd.Parameters.AddWithValue("@Process", (data.Process ?? "").Trim());

                    recordExists = (int)cmd.ExecuteScalar() > 0;
                }

                string sql;
                if (data.Process == "NotesOnly")
                {
                    sql = @"
                UPDATE TCTData
                SET Notes = @Notes,
                    UpdatedBy = @UpdatedBy,
                    UpdatedAt = @UpdatedAt
                WHERE RTRIM(ModelName) = RTRIM(@ModelName)
                  AND RTRIM(TypeName) = RTRIM(@TypeName)";
                }
                else if (recordExists)
                {
                    sql = @"
                UPDATE TCTData
                SET TCTValue = @TCTValue,
                    Notes = @Notes,
                    UpdatedBy = @UpdatedBy,
                    UpdatedAt = @UpdatedAt
                WHERE RTRIM(ModelName) = RTRIM(@ModelName)
                  AND RTRIM(TypeName) = RTRIM(@TypeName)
                  AND RTRIM(Process) = RTRIM(@Process)";
                }
                else
                {
                    sql = @"
                INSERT INTO TCTData
                (ModelName, TypeName, Process, TCTValue, Notes, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
                VALUES
                (@ModelName, @TypeName, @Process, @TCTValue, @Notes, @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt)";
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    DateTime now = data.LastUpdatedAt ?? DateTime.Now;

                    cmd.Parameters.AddWithValue("@ModelName", data.ModelName ?? "");
                    cmd.Parameters.AddWithValue("@TypeName", data.Type ?? "");
                    cmd.Parameters.AddWithValue("@Process", data.Process ?? "");
                    cmd.Parameters.AddWithValue("@TCTValue", data.TCTValue != null ? (object)data.TCTValue : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", data.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                    cmd.Parameters.AddWithValue("@UpdatedAt", now);

                    if (!recordExists && data.Process != "NotesOnly")
                    {
                        cmd.Parameters.AddWithValue("@CreatedBy", updatedBy);
                        cmd.Parameters.AddWithValue("@CreatedAt", now);
                    }

                    Debug.WriteLine($"[DAL] {(recordExists ? "Update" : "Insert")} {data.ModelName} - {data.Type} - {data.Process}");
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static bool ModelExists(string modelName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM TCTData WHERE ModelName = @ModelName";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ModelName", modelName);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public static void DeleteTCT(string modelName, string typeName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query;
                SqlCommand cmd;

                if (string.IsNullOrWhiteSpace(typeName))
                {
                    // Nếu typeName rỗng thì so sánh với IS NULL hoặc ''
                    query = @"
                DELETE FROM TCTData 
                WHERE RTRIM(LTRIM(ModelName)) = RTRIM(LTRIM(@ModelName)) 
                  AND (TypeName IS NULL OR RTRIM(LTRIM(TypeName)) = '')";
                    cmd = new SqlCommand(query, conn);
                }
                else
                {
                    query = @"
                DELETE FROM TCTData 
                WHERE RTRIM(LTRIM(ModelName)) = RTRIM(LTRIM(@ModelName)) 
                  AND RTRIM(LTRIM(TypeName)) = RTRIM(LTRIM(@TypeName))";
                    cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TypeName", typeName);
                }

                cmd.Parameters.AddWithValue("@ModelName", modelName);
                int rows = cmd.ExecuteNonQuery();
                Debug.WriteLine($"[DeleteTCT] Xóa {rows} dòng với ModelName='{modelName}', TypeName='{typeName}'");
            }
        }

        public static List<TCTData_Model> GetAllTCTData()
        {
            var list = new List<TCTData_Model>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT TCTID, ModelName, TypeName, Process, TCTValue, UpdatedAt, Notes
                FROM TCTData
                ORDER BY ModelName, TypeName, Process";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TCTData_Model
                            {
                                TCTID = reader["TCTID"] != DBNull.Value ? Convert.ToInt32(reader["TCTID"]) : 0,
                                ModelName = reader["ModelName"]?.ToString(),
                                Type = reader["TypeName"]?.ToString(),
                                Process = reader["Process"]?.ToString(),
                                TCTValue = reader["TCTValue"] != DBNull.Value ? Convert.ToDouble(reader["TCTValue"]) : (double?)null,
                                LastUpdatedAt = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]) : (DateTime?)null,
                                Notes = reader["Notes"]?.ToString()
                            };

                            list.Add(item);
                        }
                    }
                }
            }

            return list;
        }
        public static int InsertMissingModelNames(List<string> modelNames, string createdBy)
        {
            if (modelNames == null || modelNames.Count == 0)
                return 0;

            int insertedCount = 0;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                foreach (var model in modelNames)
                {
                    if (string.IsNullOrWhiteSpace(model))
                        continue;

                    string sql = @"
IF NOT EXISTS (SELECT 1 FROM TCTData WHERE RTRIM(ModelName) = RTRIM(@ModelName))
BEGIN
    INSERT INTO TCTData (ModelName, CreatedBy, CreatedAt)
    VALUES (@ModelName, @CreatedBy, GETDATE())
END";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ModelName", model);
                        cmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                        int result = cmd.ExecuteNonQuery(); 
                        if (result > 0)
                            insertedCount++;
                    }
                }
            }

            return insertedCount;
        }
    }
}
