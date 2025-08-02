using System;
using System.Collections.Generic;
using System.Reflection;
using KpiApplication.Models;

namespace KpiApplication.Extensions
{
    public static class ModelExtensions
    {
        public static Dictionary<string, object> GetChangedProperties(this IETotal_Model current, IETotal_Model original)
        {
            var result = new Dictionary<string, object>();
            if (original == null || current == null) return result;

            var props = typeof(IETotal_Model).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanRead) continue;

                var originalValue = prop.GetValue(original);
                var currentValue = prop.GetValue(current);

                bool changed = (originalValue == null && currentValue != null) ||
                               (originalValue != null && !originalValue.Equals(currentValue));

                if (changed)
                {
                    result[prop.Name] = currentValue;
                }
            }

            return result;
        }

        public static Dictionary<string, object> GetNonEmptyProperties(this IETotal_Model model)
        {
            var result = new Dictionary<string, object>();
            if (model == null) return result;

            var props = typeof(IETotal_Model).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanRead) continue;

                var value = prop.GetValue(model);
                if (value != null && !(value is string s && string.IsNullOrWhiteSpace(s)))
                {
                    result[prop.Name] = value;
                }
            }

            return result;
        }
    }
}
