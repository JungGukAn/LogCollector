using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Diagnostics.CodeAnalysis;

namespace LogCollectorCore
{
    public class LogDynamicObject : DynamicObject, IDictionary<string, object>
    {
        Dictionary<string, object> properties = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (properties.TryGetValue(binder.Name, out result) == false)
            {
                result = GetDefault(binder.ReturnType);
            }

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (value is Delegate)
            {
                return false;
            }

            properties[binder.Name] = value;
            return true;
        }

        private object GetDefault(Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }

            return null;
        }

        public ICollection<string> Keys => properties.Keys;

        public ICollection<object> Values => properties.Values;

        public int Count => properties.Count;

        public bool IsReadOnly => false;

        public object this[string key] { get => properties[key]; set => properties[key] = value; }

        public void Add(string key, object value)
        {
            properties.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return properties.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return properties.Remove(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            return properties.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            properties.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            properties.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return properties.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            foreach (var item in properties)
            {
                array[arrayIndex++] = item;
            }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            if (properties.TryGetValue(item.Key, out var found) == false)
            {
                return false;
            }

            if (found == item.Value)
            {
                properties.Remove(item.Key);
                return true;
            }

            return false;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
