using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Extension
{
    public static class Extension
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
        public static bool SetPrivate(this object self, string name, object value)
        {
            FieldKey fieldKey = new FieldKey(self.GetType(), name);
            FieldInfo field;
            if (!_fieldCache.TryGetValue(fieldKey, out field))
            {
                field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (null != field)
                {
                    _fieldCache.Add(fieldKey, field);
                    field.SetValue(self, value);
                    return true;
                }
                else
                {
                    Logger.Log(LogLevel.Error, "[KK_SAGP] Set Field Not Found: " + name);
                }
            }
            return false;
        }
        public static void SetPrivateProperty(this object self, string name, object value)
        {
            PropertyInfo propertyInfo;
            propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            if (null != propertyInfo)
            {
                propertyInfo.SetValue(self, value, null);
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SAGP] Set Property Not Found: " + name);
            }
        }
        public static object GetPrivateProperty(this object self, string name)
        {
            PropertyInfo propertyInfo;
            propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            return propertyInfo.GetValue(self, null);
        }
        public static object CallPrivate(this object self, string name, params object[] p)
        {
            return self.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Invoke(self, p);
        }
        public static bool CheckRequiredPlugin(BaseUnityPlugin origin, string guid)
        {
            var target = BepInEx.Bootstrap.Chainloader.Plugins
                .Select(MetadataHelper.GetMetadata)
                .FirstOrDefault(x => x.GUID == guid);
            if (target != null)
            {
                //Logger.Log(BepInEx.Logging.LogLevel.Debug,"Plugin "+" guid "+" was not found.");
                //Logger.Log(BepInEx.Logging.LogLevel.Debug, "[KK_SCLO] "+guid+" Found.");
                return true;
            }
            return false;
        }
    }

}