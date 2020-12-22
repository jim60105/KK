using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Extension {

    public static partial class Reflection {
        public static readonly BindingFlags BindFlagAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;

        private struct Key {
            public readonly Type[] Types;
            public readonly string Name;
            private readonly int _hashCode;
            public Type Type => Types[0];

            public Key(string inName, params Type[] inTypes) {
                Types = inTypes;
                Name = inName;
                _hashCode = (Types.GetHashCode() ^ Name.GetHashCode());
            }

            public override int GetHashCode() => _hashCode;
        }

        private static readonly Dictionary<Key, FieldInfo> _fieldCache = new Dictionary<Key, FieldInfo>();
        private static readonly Dictionary<Key, PropertyInfo> _propertyCache = new Dictionary<Key, PropertyInfo>();
        private static readonly Dictionary<Key, MethodInfo> _methodCache = new Dictionary<Key, MethodInfo>();

        public static object GetField<T>(this object self, string name) where T : class => _GetField<T>(self, name);
        public static object GetField(this object self, string name) {
            Type type = self.GetType();
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Reflection._GetField) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(self, new object[] { self, name });
        }
        public static object GetFieldStatic(this Type type, string name) {
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Reflection._GetField) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(null, new object[] { null, name });
        }
        private static object _GetField<T>(this object self, string name) where T : class {
            Key fieldKey = new Key(name, typeof(T));
            if (!SearchForFields(fieldKey)) {
                return null;
            }
            _fieldCache.TryGetValue(fieldKey, out FieldInfo field);
            return field.GetValue(self as T) ?? null;
        }

        public static bool SetField(this object self, string name, object value) {
            //LogDebug($"SetField with no type");
            Type type = self.GetType();
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Reflection.SetField) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return (bool)method.Invoke(self, new object[] { self, name, value });
        }
        public static bool SetFieldStatic(this Type type, string name, object value) {
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Reflection.SetField) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return (bool)method.Invoke(null, new object[] { null, name, value });
        }
        public static bool SetField<T>(this object self, string name, object value) where T : class {
            Key fieldKey = new Key(name, typeof(T));
            if (!SearchForFields(fieldKey)) {
                return false;
            }
            _fieldCache.TryGetValue(fieldKey, out FieldInfo field);
            try {
                field.SetValue(self, value);
                return true;
            } catch (ArgumentException ae) {
                Logger.LogError($"Set Field is not the same type as input: {name}");
                Logger.LogError($"{ae.Message}");
                return false;
            }
        }

        public static object GetProperty<T>(this object self, string name) where T : class => _GetProperty<T>(self, name);
        public static object GetProperty(this object self, string name) {
            Type type = self.GetType();
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Reflection._GetProperty) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(self, new object[] { self, name });
        }
        public static object GetPropertyStatic(this Type type, string name) {
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Reflection._GetProperty) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(null, new object[] { null, name });
        }
        private static object _GetProperty<T>(this object self, string name) where T : class {
            Key key = new Key(name, typeof(T));
            if (!SearchForProperties(key)) {
                return null;
            }

            _propertyCache.TryGetValue(key, out PropertyInfo info);
            return info.GetValue(self as T, null) ?? null;
        }

        public static bool SetProperty(this object self, string name, object value) {
            Type type = self.GetType();
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Reflection.SetProperty) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return (bool)method.Invoke(self, new object[] { self, name, value });
        }
        public static bool SetProperty<T>(this object self, string name, object value) where T : class {
            Key key = new Key(name, typeof(T));
            if (!SearchForProperties(key)) {
                return false;
            }

            _propertyCache.TryGetValue(key, out PropertyInfo property);
            try {
                property.SetValue(self as T, value, null);
                return true;
            } catch (ArgumentException ae) {
                Logger.LogError($"Set Property is not the same type as input: {name}");
                Logger.LogError($"{ae.Message}");
                return false;
            }
        }

        public static object Invoke<T>(this object self, string name, object[] p = null) where T : class => _Invoke<T>(self, name, p);
        public static object Invoke(this object self, string name, object[] p = null) {
            Type type = self.GetType();
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Reflection._Invoke) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(self, new object[] { self, name, p });
        }
        public static object InvokeStatic(this Type type, string name, object[] p = null) {
            MethodInfo method = typeof(Reflection).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Reflection._Invoke) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(null, new object[] { null, name, p });
        }
        private static object _Invoke<T>(object self, string name, object[] p = null) where T : class {
            List<Type> list = new List<Type> { typeof(T) };
            if (p != null) {
                list = list.Concat(p.Select<object, Type>(o => o?.GetType() ?? typeof(object))).ToList();
            }
            Key key = new Key(name, list.ToArray());
            if (!SearchForMethod(key)) {
                return null;
            }

            _methodCache.TryGetValue(key, out MethodInfo methodInfo);
            try {
                if (null != self) {
                    return methodInfo.Invoke(self as T, p);
                } else if (methodInfo.IsStatic) {
                    return methodInfo.Invoke(null, p);
                } else { throw new Exception($"Invoke as static on a not static method"); }
            } catch (ArgumentException ae) {
                Logger.LogError($"Invoke Method is not the same type as input: {name}");
                Logger.LogError($"{ae.Message}");
            } catch (Exception e) {
                Logger.LogError($"{e.GetType().Name} {e.Message}");
            }
            return null;
        }

        private static bool SearchForFields(Key fieldKey) {
            if (_fieldCache.ContainsKey(fieldKey)) {
                return true;
            } else {
                FieldInfo[] fieldInfos = fieldKey.Type.GetFields(BindFlagAll);
                System.Text.StringBuilder printArray = new System.Text.StringBuilder();
                foreach (FieldInfo fi in fieldInfos) {
                    if (fi.Name == fieldKey.Name) {
                        _fieldCache.Add(fieldKey, fi);
                        return true;
                    }
                    printArray.Add($"Field Name/ Type: {fi.Name}/ {fi.FieldType}");
                }
                Logger.LogError($"Field Not Found : {fieldKey.Name} on {fieldKey.Type.FullName}");
                Logger.LogDebug($"Get {fieldInfos.Length} Fields.");
                Logger.LogDebug(printArray.ToString());
                return false;
            }
        }

        private static bool SearchForProperties(Key propertyKey) {
            if (_propertyCache.ContainsKey(propertyKey)) {
                return true;
            } else {
                PropertyInfo[] propertyInfos = propertyKey.Type.GetProperties(BindFlagAll);
                System.Text.StringBuilder printArray = new System.Text.StringBuilder();
                foreach (PropertyInfo pi in propertyInfos) {
                    if (pi.Name == propertyKey.Name) {
                        _propertyCache.Add(propertyKey, pi);
                        return true;
                    }
                    printArray.AppendLine($"Property Name/ Type: {pi.Name}/ {pi.PropertyType}");
                }
                Logger.LogError($"Property Not Found : {propertyKey.Name} on {propertyKey.Type.FullName}");
                Logger.LogDebug($"Get {propertyInfos.Length} Properties.");
                Logger.LogDebug(printArray.ToString());
                return false;
            }
        }

        private static bool SearchForMethod(Key methodKey) {
            if (_methodCache.ContainsKey(methodKey)) {
                return true;
            } else {
                MethodInfo[] methodInfos = methodKey.Type.GetMethods(BindFlagAll);
                System.Text.StringBuilder printArray = new System.Text.StringBuilder();
                foreach (MethodInfo me in methodInfos) {
                    if (me.Name == methodKey.Name &&
                        me.GetParameters().Length + 1 == methodKey.Types.Length &&
                        new List<Type> { methodKey.Type }
                        .Concat(me.GetParameters().Select(pi => pi.ParameterType))
                        .SequenceEqual(methodKey.Types, new TypeEqualityOrAssignableComparer())
                        ) {
                        _methodCache.Add(methodKey, me);
                        return true;
                    }
                    printArray.AppendLine($"Method Name: ReturnType/ ParamsType: {me.Name}: {me.ReturnType.Name}/ {string.Join($", ", me.GetParameters().Select<ParameterInfo, string>(pi => pi.ParameterType.Name).ToArray())}");
                }
                Logger.LogError($"Method Not Found: {methodKey.Name} on {methodKey.Type.FullName}");
                Logger.LogError($"Search for Types and Params: " + string.Join($", ", methodKey.Types.Select<Type, string>(x => x.Name).ToArray()));
                Logger.LogDebug($"Get {methodInfos.Length} Methods.");
                Logger.LogDebug(printArray.ToString());
                return false;
            }
        }
        private class TypeEqualityOrAssignableComparer : EqualityComparer<Type> {
            public override bool Equals(Type t1, Type t2) =>
                t1.Equals(t2) ||
                t1.IsAssignableFrom(t2) ||
                t2.IsAssignableFrom(t1) ||
                (t1.IsEnum && t2 == typeof(int)) ||
                (t2.IsEnum && t1 == typeof(int));
            public override int GetHashCode(Type t) => t.GetHashCode();
        }
    }
}