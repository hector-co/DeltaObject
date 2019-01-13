using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace DeltaObject
{
    public static class TypeExtensions
    {
        private static ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _typeProperties = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        public static IEnumerable<PropertyInfo> GetCachedProperties(this Type type)
        {
            if (_typeProperties.ContainsKey(type))
                return _typeProperties[type];

            var targetProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            _typeProperties.TryAdd(type, targetProperties);

            return targetProperties;
        }
    }
}
