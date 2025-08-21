using DevExpress.Utils;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace KpiApplication.Services
{
    public class DocumentServices
    {
        private readonly BonusDocument_DAL _docDal;
        private readonly Article_DAL _articleDal;

        private readonly LruCache<int, byte[]> _docCache = new LruCache<int, byte[]>(20, TimeSpan.FromMinutes(10));
        private readonly LruCache<int, Image> _imageCache = new LruCache<int, Image>(10, TimeSpan.FromMinutes(10));
        private readonly LruCache<string, List<BonusDocument_Model>> _docListCache
            = new LruCache<string, List<BonusDocument_Model>>(50, TimeSpan.FromMinutes(10));

        public LruCache<int, Image> ImageCache => _imageCache;

        public DocumentServices()
        {
            _docDal = new BonusDocument_DAL();
            _articleDal = new Article_DAL();
        }

        #region Cache Operations

        public void ClearCache()
        {
            _docListCache.Clear(); // Xóa cache danh sách
            _docCache.Clear();     // Xóa cache file bytes
            _imageCache.Clear();   // Xóa cache ảnh
        }

        public void RemoveDocumentFromCache(int docId, string modelName = null)
        {
            _docCache.Remove(docId);
            _imageCache.Remove(docId);

            if (!string.IsNullOrEmpty(modelName))
                _docListCache.Remove(modelName); 
        }
        #endregion

        #region Query

        public List<string> GetModelNames()
        {
            return _articleDal.GetDistinctModelNames();
        }

        public List<BonusDocument_Model> GetDocumentsByModel(string modelName, List<string> documentType)
        {
            return _docDal.GetMetadataByModelName(modelName, documentType);
        }
        public List<BonusDocument_Model> GetDocumentsByModelCached(string modelName, List<string> documentTypes)
        {
            if (_docListCache.TryGetValue(modelName, out var cachedList))
                return cachedList;

            var list = _docDal.GetMetadataByModelName(modelName, documentTypes) ?? new List<BonusDocument_Model>();
            _docListCache.AddOrUpdate(modelName, list);

            return list;
        }

        public List<Article_Model> GetArticlesByModel(string modelName)
        {
            return _articleDal.GetByModelName(modelName);
        }

        public BonusDocument_Model GetDocumentById(int docId)
        {
            return _docDal.GetById(docId);
        }

        public bool DocumentExists(string modelName, string fileName)
        {
            return _docDal.Exists(modelName, fileName);
        }

        #endregion

        #region Document Data

        public byte[] GetDocumentBytes(int docId)
        {
            var doc = _docDal.GetById(docId);
            return doc?.PdfData;
        }

        public byte[] GetDocumentBytesWithCache(int docId)
        {
            if (_docCache.TryGetValue(docId, out var data))
                return data;

            var bytes = GetDocumentBytes(docId);
            if (bytes?.Length > 0)
                _docCache.AddOrUpdate(docId, bytes);

            return bytes;
        }

        #endregion

        #region Save / Update / Rename / Delete

        public void RenameDocument(BonusDocument_Model doc, string newFileName, int userId)
        {
            if (doc == null || string.IsNullOrWhiteSpace(newFileName))
                throw new ArgumentException("Invalid document or file name");

            _docDal.RenameFileNameById(doc.Id, newFileName, DateTime.Now, userId);
        }

        public BonusDocument_Model SaveOrUpdateDocument(
            string modelName, string fileName, string documentType, byte[] data, int userId)
        {
            if (string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(fileName) || data == null)
                throw new ArgumentException("Invalid input for saving document.");

            var doc = new BonusDocument_Model
            {
                ModelName = modelName,
                FileName = fileName,
                DocumentType = documentType,
                PdfData = data,
                CreatedBy = userId
            };

            if (_docDal.Exists(modelName, fileName))
            {
                var existing = _docDal.GetMetadataByModelName(modelName)
                                      ?.FirstOrDefault(x => x.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    doc.Id = existing.Id;
                    doc.UpdatedAt = DateTime.Now;
                    doc.UpdatedBy = userId;
                    _docDal.Update(doc);
                }
            }
            else
            {
                _docDal.Insert(doc);
            }

            return _docDal.GetMetadataByModelName(modelName)
                          ?.FirstOrDefault(x => x.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        public void DeleteDocument(int docId)
        {
            _docDal.Delete(docId);
            RemoveDocumentFromCache(docId);
        }

        #endregion

        #region Export / Validation

        public static void ExportDocumentToFile(MemoryStream stream, string filePath)
        {
            if (stream == null || string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Invalid stream or file path.");

            File.WriteAllBytes(filePath, stream.ToArray());
        }
        public List<string> GetModelNamesHavingDocuments()
        {
            return _articleDal.GetModelNameExistFile();
        }

        public static bool IsValidFileName(string name, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = Lang.FileNameCannotBeEmpty;
                return false;
            }

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = Lang.InvalidFileNameCharacters;
                return false;
            }

            return true;
        }
        public static string GetDocumentTooltip(BonusDocument_Model item)
        {
            if (item == null) return string.Empty;

            string tooltip = $"📄 {Lang.FileName}: {item.FileName}\n🕒 {Lang.CreatedAt}: {item.CreatedAt:yyyy-MM-dd}\n👤 {Lang.CreatedBy}: {item.CreatedByName}";
            if (item.UpdatedAt.HasValue && !string.IsNullOrWhiteSpace(item.UpdatedByName))
            {
                tooltip += $"\n✏️ {Lang.UpdatedAt}: {item.UpdatedAt:yyyy-MM-dd}\n👤 {Lang.UpdatedBy}: {item.UpdatedByName}";
            }

            return tooltip;
        }


        public static readonly Dictionary<string, string> FileFilters = new Dictionary<string, string>()
        {
            [".pdf"] = "PDF File (*.pdf)|*.pdf",
            [".jpg"] = "JPEG Image (*.jpg)|*.jpg",
            [".jpeg"] = "JPEG Image (*.jpeg)|*.jpeg",
            [".png"] = "PNG Image (*.png)|*.png",
            [".bmp"] = "Bitmap Image (*.bmp)|*.bmp"
        };

        #endregion
    }
}
