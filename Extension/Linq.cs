using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Extension {
    public static class Linq {
        #region Fake Linq
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this object self) {
            if (!(self is IDictionary dictionary)) {
                Logger.LogError($"Faild to cast to Dictionary!");
                return null;
            }
            Dictionary<TKey, TValue> newDictionary =
                CastDict(dictionary)
                .ToDictionary(entry => (TKey)entry.Key,
                              entry => (TValue)entry.Value);
            return newDictionary;

            IEnumerable<DictionaryEntry> CastDict(IDictionary dic) {
                foreach (DictionaryEntry entry in dic) {
                    yield return entry;
                }
            }
        }

        public static List<T> ToList<T>(this object self) {
            if (!(self is IEnumerable<T> iEnumerable)) {
                Logger.LogError($"Faild to cast to List!");
                return null;
            }
            List<T> newList = new List<T>(iEnumerable);
            return newList;
        }
        #endregion

        #region Fake Linq for "Dictionary<unknown>" which cast into "object"
        public static object ToDictionaryWithoutType(this object self) {
            Type type = self.GetType();
            foreach (Type interfaceType in type.GetInterfaces()) {
                //LogDebug(interfaceType.ToString());
                if (interfaceType.IsGenericType &&
                   interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
                    Type KeyType = type.GetGenericArguments()[0];
                    Type ValueType = type.GetGenericArguments()[1];
                    MethodInfo method = typeof(Linq).GetMethod(nameof(ToDictionary), BindingFlags.Public | BindingFlags.Static);
                    if (null != KeyType && null != ValueType) {
                        method = method.MakeGenericMethod(new Type[] { KeyType, ValueType });
                    }

                    return method.Invoke(null, new object[] { self });
                }
            }
            Logger.LogError($"Faild to cast to Dictionary<unknown>!");
            return null;
        }

        public static bool TryGetValue(this object self, object key, out object value) {
            value = null;
            Type paramTypes = self.GetType().GetInterfaces()
                .SingleOrDefault(p =>
                    p.IsGenericType &&
                    p.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                )?.GetGenericArguments()[0];

            if (null == paramTypes || !paramTypes.IsAssignableFrom(key.GetType())) {
                Logger.LogError($"Key type not match! Cannot Get {key.GetType().FullName} from object");
                return false;
            }

            if (!(bool)self.Invoke("ContainsKey", new object[] { key })) {
                Logger.LogError($"Key not found! Cannot Get {key.GetType().FullName} from object");
                return false;
            }

            value = self.Invoke("get_Item", new object[] { key });
            return true;
        }
        #endregion

        #region Fake Linq for "List<unknown>" which cast into "object"
        public static object ToListWithoutType(this object self) {
            Type type = self.GetType();
            foreach (Type interfaceType in type.GetInterfaces()) {
                if (interfaceType.IsGenericType &&
                   interfaceType.GetGenericTypeDefinition() == typeof(IList<>)) {
                    Type itemType = type.GetGenericArguments()[0];
                    MethodInfo method = typeof(Linq).GetMethod($"ToList", BindingFlags.Public | BindingFlags.Static);
                    if (null != itemType) {
                        method = method.MakeGenericMethod(itemType);
                    }

                    return method.Invoke(null, new object[] { self });
                }
            }
            Logger.LogError($"Faild to cast to List<unknown>!");
            return null;
        }

        public static void AddRange(this object self, object obj2Add) {
            if (self is IList oriList && obj2Add is IList listToAdd) {
                Type selfItemType = null;
                foreach (Type interfaceType in self.GetType().GetInterfaces()) {
                    if (interfaceType.IsGenericType &&
                       interfaceType.GetGenericTypeDefinition() == typeof(IList<>)) {
                        selfItemType = self.GetType().GetGenericArguments()[0];
                    }
                }
                Type addItemType = null;
                foreach (Type interfaceType in obj2Add.GetType().GetInterfaces()) {
                    if (interfaceType.IsGenericType &&
                       interfaceType.GetGenericTypeDefinition() == typeof(IList<>)) {
                        addItemType = obj2Add.GetType().GetGenericArguments()[0];
                    }
                }
                if (null != selfItemType && selfItemType == addItemType) {
                    foreach (object o in listToAdd) {
                        oriList.Add(o);
                    }
                    //LogDebug($"AddRange: Add {listToAdd.Count} item."});
                } else {
                    Logger.LogError($"Type not Match! Cannot Add {addItemType.FullName} into {selfItemType.FullName}");
                }
            }
        }
        #endregion

        #region Fake Linq for List<unknown> and Dictionary<unknown, unknown>
        public static int RemoveAll(this object self, Predicate<object> match) {
            int amount = 0;
            if (self is IList list) {
                for (int i = 0; i < list.Count; i++) {
                    if (match(list[i])) {
                        list.RemoveAt(i);
                        amount++;
                        i--;
                        //LogDebug($"Remove at {i}/{list.Count}"});
                    }
                }
            } else if (self is IDictionary dic) {
                Queue<object> keysToRemove = new Queue<object>();
                foreach (object key in dic.Keys) {
                    if (match(new KeyValuePair<object, object>(key, dic[key]))) {
                        keysToRemove.Enqueue(key);
                        //LogDebug($"Remove at {i}/{list.Count}"});
                    }
                }
                amount = keysToRemove.Count;
                while (keysToRemove.Count > 0) {
                    object key = keysToRemove.Dequeue();
                    dic.Remove(key);
                }
            } else {
                Logger.LogError($"RemoveAll: Input Object is not type of List<unknown> or Dictionary<unknown>!");
            }
            //LogDebug($"RemoveAll: Output Obj Count {list.Count}"});
            return amount;
        }

        public static int Count(this object self) {
            if (self is IList list) {
                return list.Count;
            }
            if (self is IDictionary dic) {
                return dic.Count;
            } else {
                Logger.LogError($"Count: Input Object is not type of List<unknown> or Dictionary<unknown>!");
                return -1;
            }
        }

        public static object Select(this object self, Func<object, object> func) {
            if (self is IList oriList) {
                for (int i = 0; i < oriList.Count; i++) {
                    oriList[i] = func(oriList[i]);
                }
                return oriList;
            } else if (self is IDictionary dic) {
                foreach (object k in dic.Keys) {
                    dic[k] = func(dic[k]);
                }
                return dic;
            } else {
                Logger.LogError($"Select: Input Object is not type of List<unknown>!");
                return null;
            }
        }

        public static object ForEach(this object self, Action<object> action) {
            if (self is IList list) {
                foreach (object l in list) {
                    action(l);
                }
            } else if (self is IDictionary dic) {
                foreach (DictionaryEntry d in dic) {
                    action(d);
                }
            } else {
                Logger.LogError($"Select: Input Object is not type of Dictionary<unknown> or List<unknown>!");
                return null;
            }
            return self;
        }

        public static object Where(this object self, Predicate<object> match) {
            if (self is IList list) {
                IList result = (IList)list.ToListWithoutType();

                result.RemoveAll(x => !match(x));
                return result;
            } else if (self is IDictionary dic) {
                IDictionary result = (IDictionary)dic.ToDictionaryWithoutType();

                result.RemoveAll(x => !match(x));
                return result;
            } else {
                Logger.LogError($"Where: Input Object is not type of List<unknown>!");
            }
            return null;
        }

        public static void Add(this object self, object obj2Add) {
            if (self is IList oriList) {
                Type selfItemType = null;
                foreach (Type interfaceType in self.GetType().GetInterfaces()) {
                    if (interfaceType.IsGenericType &&
                       interfaceType.GetGenericTypeDefinition() == typeof(IList<>)) {
                        selfItemType = self.GetType().GetGenericArguments()[0];
                    }
                }
                if (null != selfItemType && obj2Add.GetType() == selfItemType) {
                    oriList.Add(obj2Add);
                    //LogDebug($"AddRange: Add {listToAdd.Count} item."});
                } else {
                    Logger.LogError($"Type not Match! Cannot Add {obj2Add.GetType().FullName} into {selfItemType.FullName}");
                }
            }
        }

        public static void Add(this object self, object key2Add, object value2Add) {
            if (self is IDictionary dic) {
                Type keyType = null;
                Type valueType = null;
                foreach (Type interfaceType in self.GetType().GetInterfaces()) {
                    if (interfaceType.IsGenericType &&
                       interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
                        keyType = self.GetType().GetGenericArguments()[0];
                        valueType = self.GetType().GetGenericArguments()[1];
                    }
                }
                if (null != keyType && null != valueType && key2Add.GetType() == keyType && value2Add.GetType() == valueType) {
                    dic.Add(key2Add, value2Add);
                    //LogDebug($"AddRange: Add {listToAdd.Count} item."});
                } else {
                    Logger.LogError($"Type not Match! Cannot Add <{key2Add.GetType().FullName}, <{value2Add.GetType().FullName}> into Dictionary<{keyType.FullName}, {valueType.FullName}>");
                }
            }
        }
        #endregion

    }
}
