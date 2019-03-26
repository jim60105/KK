using System;
using System.Collections.Generic;
using System.Reflection;

namespace KK_CostumeInfo_Patches
{
    public static class Extensions
    {
        private struct FieldKey
        {
            public readonly Type type;
            public readonly string name;
            private readonly int _hashCode;

            public FieldKey(Type inType, string inName)
            {
                this.type = inType;
                this.name = inName;
                this._hashCode = this.type.GetHashCode() ^ this.name.GetHashCode();
            }

            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }
        private static readonly Dictionary<FieldKey, FieldInfo> _fieldCache = new Dictionary<FieldKey, FieldInfo>();
        public static object GetPrivate(this object self, string name)
        {
            FieldKey key = new FieldKey(self.GetType(), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            return info.GetValue(self);
        }
    }

}