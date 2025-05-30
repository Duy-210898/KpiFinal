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
        public static void SaveTCTImportList(List<TCTImport> list)
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
                            TCTValue FLOAT
                        );";
                            cmd.ExecuteNonQuery();
                        }

                        var table = new DataTable();
                        table.Columns.Add("ModelName", typeof(string));
                        table.Columns.Add("TypeName", typeof(string));
                        table.Columns.Add("Process", typeof(string));
                        table.Columns.Add("TCTValue", typeof(double));

                        foreach (var item in list)
                        {
                            table.Rows.Add(
                                item.ModelName ?? (object)DBNull.Value,
                                item.Type ?? (object)DBNull.Value,
                                item.Process ?? (object)DBNull.Value,
                                item.TCT ?? (object)DBNull.Value);
                        }

                        using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
                        {
                            bulkCopy.DestinationTableName = "#TempTCTData";
                            bulkCopy.ColumnMappings.Add("ModelName", "ModelName");
                            bulkCopy.ColumnMappings.Add("TypeName", "TypeName");
                            bulkCopy.ColumnMappings.Add("Process", "Process");
                            bulkCopy.ColumnMappings.Add("TCTValue", "TCTValue");

                            bulkCopy.WriteToServer(table);
                        }

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = @"
                        INSERT INTO TCTData (ModelName, TypeName, Process, TCTValue)
                        SELECT t.ModelName, t.TypeName, t.Process, t.TCTValue
                        FROM #TempTCTData t
                        WHERE NOT EXISTS (
                            SELECT 1 FROM TCTData d WHERE d.ModelName = t.ModelName AND d.Process = t.Process
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

    }
}
