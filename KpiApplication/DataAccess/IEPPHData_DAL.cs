using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace KpiApplication.DataAccess
{
    public class IEPPHData_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;
        public int? GetProcessID(string processName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT ProcessID FROM Process WHERE ProcessNAme = @ProcessName", conn))
                {
                    cmd.Parameters.AddWithValue("@ProcessName", processName);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : (int?)null;
                }
            }
        }

        public bool UpdateIEPPHData_IE_PPH_Data_Part(IETotal data)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
            UPDATE [IE_PPH_Data]
            SET 
                Person_Incharge = @PersonIncharge,
                NoteForPC = @NoteForPC,
                PCSend = @PCSend,
                Outsourcing_Assembling = @OA,
                Outsourcing_Stitching = @OS,
                Outsourcing_StockFitting = @OF,
                DataStatus = @DataStatus
            WHERE IE_ID = @IE_ID", conn))
                {
                    // Person Incharge (string)
                    cmd.Parameters.AddWithValue("@PersonIncharge", !string.IsNullOrEmpty(data.PersonIncharge) ? (object)data.PersonIncharge : DBNull.Value);

                    // Note For PC (string)
                    cmd.Parameters.AddWithValue("@NoteForPC", !string.IsNullOrEmpty(data.NoteForPC) ? (object)data.NoteForPC : DBNull.Value);

                    // PC Send (string)
                    cmd.Parameters.AddWithValue("@PCSend", !string.IsNullOrEmpty(data.PCSend) ? (object)data.PCSend : DBNull.Value);

                    // Outsourcing flags (bool)
                    cmd.Parameters.AddWithValue("@OA", data.OutsourcingAssemblingBool);
                    cmd.Parameters.AddWithValue("@OS", data.OutsourcingStitchingBool);
                    cmd.Parameters.AddWithValue("@OF", data.OutsourcingStockFittingBool);

                    // Data Status (string)
                    cmd.Parameters.AddWithValue("@DataStatus", !string.IsNullOrEmpty(data.Status) ? (object)data.Status : DBNull.Value);

                    // IE_ID (primary key)
                    cmd.Parameters.AddWithValue("@IE_ID", data.IEID);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool UpdateIEPPHData_Articles_Part(IETotal data)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"UPDATE [Articles]
            SET ArticleName = @ArticleName,
                ModelName = @ModelName
            WHERE ArticleID = @ArticleID", conn))
                {
                    cmd.Parameters.AddWithValue("@ArticleName", data.ArticleName != null ? (object)data.ArticleName : DBNull.Value);
                    cmd.Parameters.AddWithValue("@ModelName", data.ModelName != null ? (object)data.ModelName : DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArticleID", data.ArticleID);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool UpdateIEPPHData_Production_Stages_Part(IETotal data)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
            UPDATE [Production_Stages]
            SET 
                Target_Output_PC = @Target,
                Adjust_Operator_No = @AdjustNo,
                TypeID = @TypeID,
                IsSigned = @IsSigned,
                ReferenceModel = @RefModel,
                OperatorAdjust = @OperatorAdjust,
                ReferenceOperator = @RefOperator,
                Notes = @Notes
            WHERE IE_PPH_ID = @IE_ID AND ProcessID = @ProcessID", conn))
                {
                    // Đảm bảo xử lý nullable cho int và string
                    cmd.Parameters.AddWithValue("@Target", data.TargetOutputPC.HasValue ? (object)data.TargetOutputPC.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@AdjustNo", data.AdjustOperatorNo.HasValue ? (object)data.AdjustOperatorNo.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TypeID", data.TypeID.HasValue ? (object)data.TypeID.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsSigned", !string.IsNullOrEmpty(data.IsSigned) ? (object)data.IsSigned : DBNull.Value);
                    cmd.Parameters.AddWithValue("@RefModel", !string.IsNullOrEmpty(data.ReferenceModel) ? (object)data.ReferenceModel : DBNull.Value);
                    cmd.Parameters.AddWithValue("@OperatorAdjust", data.OperatorAdjust.HasValue ? (object)data.OperatorAdjust.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@RefOperator", data.ReferenceOperator.HasValue ? (object)data.ReferenceOperator.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", !string.IsNullOrEmpty(data.Notes) ? (object)data.Notes : DBNull.Value);

                    // Không nullable nên gán trực tiếp
                    cmd.Parameters.AddWithValue("@IE_ID", data.IEID);
                    cmd.Parameters.AddWithValue("@ProcessID", data.ProcessID);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        public int? GetTypeIDByTypeName(string typeName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"SELECT TypeID FROM ArtType WHERE TypeName = @TypeName", conn))
                {
                    cmd.Parameters.AddWithValue("@TypeName", typeName);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : (int?)null;
                }
            }
        }


        public BindingList<IETotal> GetIEPPHData()
        {
            var ieTotal = new BindingList<IETotal>();

            try
            {
                // Bước 1: Lấy dữ liệu TCTData
                var tctDataList = GetAllTCTData();

                // Tạo dictionary để tra cứu nhanh TCTValue theo (ModelName, Process, Type)
                var tctDict = tctDataList.ToDictionary(
                    t => (
                        Model: t.ModelName?.Trim().ToLowerInvariant() ?? "",
                        Process: t.Process?.Trim().ToLowerInvariant() ?? "",
                        Type: t.Type?.Trim().ToLowerInvariant() ?? ""
                    ),
                    t => t.TCTValue
                );

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    A.ArticleID,
                    A.ArticleName,
                    A.ModelName,

                    IE.IE_ID,
                    IE.PCSend,
                    IE.Person_Incharge,
                    IE.NoteForPC,
                    IE.Outsourcing_Assembling,
                    IE.Outsourcing_Stitching,
                    IE.Outsourcing_StockFitting,
                    IE.DataStatus,

                    AT.TypeName,
                    AT.TypeID,

                    P.ProcessID,
                    P.ProcessName,

                    PS.StageID,
                    PS.Target_Output_PC,
                    PS.Adjust_Operator_No,
                    PS.IsSigned,
                    PS.ReferenceModel,
                    PS.OperatorAdjust,
                    PS.ReferenceOperator,
                    PS.Notes

                    FROM Articles A
                    LEFT JOIN IE_PPH_Data IE ON IE.ArticleID = A.ArticleID
                    LEFT JOIN Production_Stages PS ON PS.IE_PPH_ID = IE.IE_ID
                    LEFT JOIN ArtType AT ON PS.TypeID = AT.TypeID
                    LEFT JOIN Process P ON P.ProcessID = PS.ProcessID
                    ORDER BY A.ArticleName, P.ProcessID            
";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 300;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var data = new IETotal
                                {
                                    ArticleID = reader["ArticleID"] as int? ?? 0,
                                    ArticleName = reader["ArticleName"] as string,
                                    ModelName = reader["ModelName"] as string,

                                    IEID = reader["IE_ID"] as int? ?? 0,
                                    PCSend = reader["PCSend"] as string,
                                    PersonIncharge = reader["Person_Incharge"] as string,
                                    NoteForPC = reader["NoteForPC"] as string,

                                    OutsourcingAssemblingBool = reader["Outsourcing_Assembling"] as bool? ?? false,
                                    OutsourcingStitchingBool = reader["Outsourcing_Stitching"] as bool? ?? false,
                                    OutsourcingStockFittingBool = reader["Outsourcing_StockFitting"] as bool? ?? false,
                                    Status = reader["DataStatus"] as string,

                                    ProcessID = reader["ProcessID"] as int? ?? 0,
                                    Process = reader["ProcessName"] as string,
                                    StageID = reader["StageID"] as int? ?? 0,

                                    TargetOutputPC = reader["Target_Output_PC"] as int?,
                                    AdjustOperatorNo = reader["Adjust_Operator_No"] as int?,

                                    TypeID = reader["TypeID"] as int? ?? 0,
                                    TypeName = reader["TypeName"] as string,

                                    IsSigned = reader["IsSigned"] as string,
                                    ReferenceModel = reader["ReferenceModel"] as string,
                                    OperatorAdjust = reader["OperatorAdjust"] as int?,
                                    ReferenceOperator = reader["ReferenceOperator"] as int?,
                                    Notes = reader["Notes"] as string
                                };

                                // Tạo key chuẩn hóa để tra cứu
                                var key = (
                                    Model: data.ModelName?.Trim().ToLowerInvariant() ?? "",
                                    Process: data.Process?.Trim().ToLowerInvariant() ?? "",
                                    Type: data.TypeName?.Trim().ToLowerInvariant() ?? ""
                                );

                                if (tctDict.TryGetValue(key, out var tctValue))
                                {
                                    data.TCTValue = tctValue;
                                }

                                ieTotal.Add(data);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi khi lấy dữ liệu IEPPH: {ex.Message}");
            }

            return ieTotal;
        }

        public List<TCTData> GetAllTCTData()
        {
            var list = new List<TCTData>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT * FROM TCTData";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new TCTData
                                {
                                    TCTID = reader["TCTID"] != DBNull.Value ? Convert.ToInt32(reader["TCTID"]) : 0,
                                    ModelName = reader["ModelName"] as string ?? string.Empty,
                                    Type = reader["TypeName"] as string ?? string.Empty,
                                    Process = reader["Process"] as string ?? string.Empty,
                                    TCTValue = reader["TCTValue"] != DBNull.Value ? (double?)Convert.ToDouble(reader["TCTValue"]) : null
                                };
                                list.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi khi lấy dữ liệu TCTData: {ex.Message}");
            }

            return list;
        }

        public BindingList<IEPPHDataForUser> GetIEPPHDataForUser()
        {
            var iePPHList = new BindingList<IEPPHDataForUser>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        A.ArticleName,
                        A.ModelName,
                        IE.PCSend,
                        IE.Person_Incharge,
                        IE.NoteForPC,
                        IE.Outsourcing_Assembling,
                        IE.Outsourcing_Stitching,
                        IE.Outsourcing_StockFitting,
                        IE.DataStatus,
                        PS.ProcessID, 
                        PP.ProcessName,
                        PS.Target_Output_PC,
                        PS.Adjust_Operator_No,
                        ROUND(PS.IE_PPH_Value, 2) AS IE_PPH_Value,
                        AT.TypeName,
                        PS.IsSigned,
                        AT.TypeID
                    FROM [KPI-DATA].[dbo].[Articles] A
                    INNER JOIN [KPI-DATA].[dbo].[IE_PPH_Data] IE 
                        ON IE.ArticleID = A.ArticleID
                    INNER JOIN [KPI-DATA].[dbo].[Production_Stages] PS 
                        ON PS.IE_PPH_ID = IE.IE_ID
                    INNER JOIN [KPI-DATA].[dbo].[ArtType] AT 
                        ON PS.TypeID = AT.TypeID
                    LEFT JOIN [KPI-DATA].[dbo].[Process] PP 
                        ON PS.ProcessID = PP.ProcessID  -- Join để lấy tên công đoạn
                    ORDER BY A.ArticleName ASC, PS.ProcessID ASC, AT.TypeID ASC;
                    ";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 300;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var data = new IEPPHDataForUser
                                {
                                    ArticleName = reader["ArticleName"] as string,
                                    ModelName = reader["ModelName"] as string,
                                    PCSend = reader["PCSend"] as string,
                                    PersonIncharge = reader["Person_Incharge"] as string,
                                    NoteForPC = reader["NoteForPC"] as string,
                                    OutsourcingAssemblingBool = reader["Outsourcing_Assembling"] as bool? ?? false,
                                    OutsourcingStitchingBool = reader["Outsourcing_Stitching"] as bool? ?? false,
                                    OutsourcingStockFittingBool = reader["Outsourcing_StockFitting"] as bool? ?? false,
                                    DataStatus = reader["DataStatus"] as string,
                                    Process = reader["ProcessName"] as string,
                                    TargetOutputPC = reader["Target_Output_PC"] as int?,
                                    AdjustOperatorNo = reader["Adjust_Operator_No"] as int?,
                                    IEPPHValue = reader["IE_PPH_Value"] as double?,
                                    TypeName = reader["TypeName"] as string,
                                    IsSigned = reader["IsSigned"] as string
                                };

                                iePPHList.Add(data);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi khi lấy dữ liệu IEPPH: {ex.Message}");
            }

            return iePPHList;
        }
    }
}
