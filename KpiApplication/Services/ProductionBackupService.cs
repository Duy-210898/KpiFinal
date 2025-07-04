using System.Collections.Generic;
using System.Linq;

namespace KpiApplication.Models
{
    public class ProductionBackupService
    {
        private readonly Dictionary<int, List<ProductionData_Model>> _backups = new Dictionary<int, List<ProductionData_Model>>();

        public void AddBackup(int groupId, List<ProductionData_Model> backup)
        {
            _backups[groupId] = backup.Select(d => d.Clone()).ToList();
        }

        public bool TryGetBackup(int groupId, out List<ProductionData_Model> backup)
        {
            return _backups.TryGetValue(groupId, out backup);
        }

        public void RemoveBackup(int groupId)
        {
            _backups.Remove(groupId);
        }

        public void Clear()
        {
            _backups.Clear();
        }
    }
}
