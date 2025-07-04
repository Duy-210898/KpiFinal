using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace KpiApplication.DataAccess
{
    public class TCT_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        public static void SaveTCTImportList(List<TCTImport_Model> list)
        {
            if (list == null || list.Count == 0) return;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
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

                        var table = new DataTable();
                        table.Columns.Add("ModelName", typeof(string));
                        table.Columns.Add("TypeName", typeof(string));
                        table.Columns.Add("Process", typeof(string));
                        table.Columns.Add("TCTValue", typeof(double));
                        table.Columns.Add("Notes", typeof(string));

                        foreach (var item in list)
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

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = @"
                        INSERT INTO TCTData (ModelName, TypeName, Process, TCTValue, Notes)
                        SELECT t.ModelName, t.TypeName, t.Process, t.TCTValue, t.Notes
                        FROM #TempTCTData t
                        WHERE NOT EXISTS (
                            SELECT 1 FROM TCTData d 
                            WHERE d.ModelName = t.ModelName 
                              AND d.TypeName = t.TypeName
                              AND d.Process = t.Process
                        );";
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                    catch (Exception)
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

                string checkSql = @"
            SELECT COUNT(*) FROM TCTData
            WHERE ModelName = @ModelName AND TypeName = @TypeName AND Process = @Process";

                string updateSql = @"
            UPDATE TCTData
            SET TCTValue = @TCTValue, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy, Notes = @Notes
            WHERE ModelName = @ModelName AND TypeName = @TypeName AND Process = @Process";

                string insertSql = @"
            INSERT INTO TCTData (ModelName, TypeName, Process, TCTValue, Notes, CreatedBy)
            VALUES (@ModelName, @TypeName, @Process, @TCTValue, @Notes, @CreatedBy)";

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = checkSql;
                    cmd.Parameters.AddWithValue("@ModelName", data.ModelName);
                    cmd.Parameters.AddWithValue("@TypeName", data.Type);
                    cmd.Parameters.AddWithValue("@Process", data.Process);

                    int exists = (int)cmd.ExecuteScalar();
                    cmd.Parameters.Clear();

                    if (exists > 0)
                        cmd.CommandText = updateSql;
                    else
                        cmd.CommandText = insertSql;

                    cmd.Parameters.AddWithValue("@ModelName", data.ModelName);
                    cmd.Parameters.AddWithValue("@TypeName", data.Type);
                    cmd.Parameters.AddWithValue("@Process", data.Process);
                    cmd.Parameters.AddWithValue("@TCTValue", data.TCTValue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", data.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedAt", data.LastUpdatedAt ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                    cmd.Parameters.AddWithValue("@CreatedBy", updatedBy);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteTCT(string modelName, string typeName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"DELETE FROM TCTData WHERE ModelName = @ModelName AND TypeName = @TypeName";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ModelName", modelName);
                    cmd.Parameters.AddWithValue("@TypeName", typeName);
                    cmd.ExecuteNonQuery();
                }
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
    }
}
