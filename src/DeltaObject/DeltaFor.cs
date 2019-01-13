using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DeltaObject.Exceptions;
using Newtonsoft.Json;

namespace DeltaObject
{
    [JsonConverter(typeof(DeltaForConvertert))]
    public class DeltaFor<T>
    {
        private static ConcurrentDictionary<string, Type> DeltaPropertyTypesForT;

        static DeltaFor()
        {
            DeltaPropertyTypesForT = new ConcurrentDictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
            var properties = typeof(T).GetCachedProperties();
            foreach (var prop in properties)
            {
                var deltaPropType = typeof(DeltaProperty<>);
                var propType = prop.PropertyType;
                var deltaPropGenericType = deltaPropType.MakeGenericType(propType);
                DeltaPropertyTypesForT.TryAdd(prop.Name, deltaPropGenericType);
            }
        }

        private readonly Dictionary<string, dynamic> _properties;

        public DeltaFor()
        {
            _properties = new Dictionary<string, dynamic>(StringComparer.InvariantCultureIgnoreCase);
        }

        private string GetPropertyName<TValue>(Expression<Func<T, TValue>> property)
        {
            MemberExpression member = property.Body as MemberExpression;
            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new DeltaObjectException();
            return propInfo.Name;
        }

        public DeltaProperty<TValue> Property<TValue>(string propertyName)
        {
            if (!DeltaPropertyTypesForT.ContainsKey(propertyName))
                throw new DeltaObjectException();
            if (!_properties.ContainsKey(propertyName)) return new DeltaProperty<TValue>();
            return (DeltaProperty<TValue>)_properties[propertyName];
        }

        public DeltaProperty<TValue> Property<TValue>(Expression<Func<T, TValue>> property)
        {
            return Property<TValue>(GetPropertyName(property));
        }

        public void Patch<TTarget>(TTarget target)
        {
            var targetProperties = typeof(TTarget).GetCachedProperties();
            var mapping = DeltaObjectMappings.GetMapping<T, TTarget>();

            foreach (var key in _properties.Keys)
            {
                if (!_properties[key].IsSet)
                    continue;
                if (!mapping.TryGetMappedPropertyName(key, out var targetPropertyMap))
                    continue;

                var targetProperty = targetProperties.FirstOrDefault(p => p.Name.Equals(targetPropertyMap.propertyName, StringComparison.CurrentCultureIgnoreCase));
                if (targetProperty == null)
                    continue;

                var value = targetPropertyMap.mapFunction == null
                    ? _properties[key].Value
                    : targetPropertyMap.mapFunction(_properties[key].Value);

                targetProperty.SetValue(target, value);
            }
        }

        internal void SetProperty(string propertyName, object value)
        {
            if (!DeltaPropertyTypesForT.ContainsKey(propertyName) || _properties.ContainsKey(propertyName)) return;

            var deltaProp = Activator.CreateInstance(DeltaPropertyTypesForT[propertyName]) as dynamic;
            deltaProp.SetValue(value);
            _properties.Add(propertyName, deltaProp);
        }
    }
}
