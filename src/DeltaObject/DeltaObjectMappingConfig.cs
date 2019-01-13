using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DeltaObject
{
    public class DeltaObjectMappingConfig
    {
        public static void ClearMappings()
        {
            DeltaObjectMappings.ClearMappings();
        }
    }

    public class DeltaObjectMappingConfig<TPatch, TTarget>
    {
        private DeltaObjectMappingConfig()
        {

        }

        public static DeltaObjectMappingConfig<TPatch, TTarget> GlobalConfig()
        {
            return new DeltaObjectMappingConfig<TPatch, TTarget>();
        }

        public DeltaObjectMappingConfig<TPatch, TTarget> Map<TPatchProperty, TTargetProperty>(Expression<Func<TPatch, TPatchProperty>> patchProperty,
            Expression<Func<TTarget, TTargetProperty>> targetProperty, Func<TPatchProperty, TTargetProperty> mapFunction = null)
        {
            var mapping = DeltaObjectMappings.GetMapping<TPatch, TTarget>();
            mapping.AddPropertyMapping(patchProperty, targetProperty, mapFunction);
            return this;
        }

        public DeltaObjectMappingConfig<TPatch, TTarget> Ignore<TPatchProperty>(Expression<Func<TPatch, TPatchProperty>> patchProperty)
        {
            var mapping = DeltaObjectMappings.GetMapping<TPatch, TTarget>();
            mapping.AddIgnoredProperty(patchProperty);
            return this;
        }

        public DeltaObjectMappingConfig<TPatch, TTarget> IgnoreNonMapped()
        {
            var mapping = DeltaObjectMappings.GetMapping<TPatch, TTarget>();
            mapping.IgnoreNonMapped();
            return this;
        }

        public static void RemoveMappings()
        {
            DeltaObjectMappings.RemoveMapping<TPatch, TTarget>();
        }
    }

    internal static class DeltaObjectMappings
    {
        private static ConcurrentDictionary<(Type, Type), dynamic> _mappings;

        static DeltaObjectMappings()
        {
            _mappings = new ConcurrentDictionary<(Type, Type), dynamic>();
        }

        public static DeltaObjectMapping<TPatch, TTarget> GetMapping<TPatch, TTarget>()
        {
            var key = (typeof(TPatch), typeof(TTarget));
            if (!_mappings.ContainsKey(key))
                _mappings.TryAdd(key, new DeltaObjectMapping<TPatch, TTarget>());
            return _mappings[key];
        }

        public static void RemoveMapping<TPatch, TTarget>()
        {
            var key = (typeof(TPatch), typeof(TTarget));
            _mappings.TryRemove(key, out var result);
        }

        public static void ClearMappings()
        {
            _mappings.Clear();
        }
    }

    internal class DeltaObjectMapping<TPatch, TTarget>
    {
        private bool _ignoreNonMapped;
        private readonly List<string> _ignoredProperties;
        private readonly Dictionary<string, (string propertyName, dynamic mapFunction)> _propertiesMapping;

        public DeltaObjectMapping()
        {
            _ignoredProperties = new List<string>();
            _propertiesMapping = new Dictionary<string, (string propertyName, dynamic mapFunction)>(StringComparer.InvariantCultureIgnoreCase);
        }

        private static bool TryGetPropertyName<TEntity, TValue>(Expression<Func<TEntity, TValue>> property, out string propertyName)
        {
            propertyName = string.Empty;
            var member = property.Body as MemberExpression;
            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null) return false;

            propertyName = propInfo.Name;
            return true;
        }

        public void IgnoreNonMapped()
        {
            _ignoreNonMapped = true;
        }

        public void AddPropertyMapping<TPatchProperty, TTargetProperty>(Expression<Func<TPatch, TPatchProperty>> patchProperty,
            Expression<Func<TTarget, TTargetProperty>> targetProperty, Func<TPatchProperty, TTargetProperty> mapFunction = null)
        {
            if (!TryGetPropertyName(patchProperty, out var patchPropertyName) || !TryGetPropertyName(targetProperty, out var targetPropertyName))
                return;

            if (_propertiesMapping.ContainsKey(patchPropertyName))
            {
                _propertiesMapping.Remove(patchPropertyName);
            }
            _propertiesMapping.Add(patchPropertyName, (targetPropertyName, mapFunction));
        }

        public void AddIgnoredProperty<TPatchProperty>(Expression<Func<TPatch, TPatchProperty>> patchProperty)
        {
            if (!TryGetPropertyName(patchProperty, out var patchPropertyName))
                return;

            if (_ignoredProperties.Any(p => p.Equals(patchPropertyName, StringComparison.CurrentCultureIgnoreCase)))
                return;

            _ignoredProperties.Add(patchPropertyName);
        }

        public bool TryGetMappedPropertyName(string patchProperty, out (string propertyName, dynamic mapFunction) targetPropertyMap)
        {
            targetPropertyMap = (string.Empty, null);

            if (_ignoredProperties.Any(p => p.Equals(patchProperty, StringComparison.CurrentCultureIgnoreCase)))
                return false;

            if (!_propertiesMapping.ContainsKey(patchProperty) && _ignoreNonMapped)
                return false;

            if (_propertiesMapping.ContainsKey(patchProperty))
                targetPropertyMap = (_propertiesMapping[patchProperty].propertyName, _propertiesMapping[patchProperty].mapFunction);
            else
                targetPropertyMap = (patchProperty, null);

            return true;
        }

    }
}