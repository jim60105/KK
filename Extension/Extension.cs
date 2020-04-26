using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Extension {
    public static class Extension {
        #region Reflection Stuff
        private struct FieldKey {
            public readonly Type type;
            public readonly string name;
            private readonly int _hashCode;

            public FieldKey(Type inType, string inName) {
                this.type = inType;
                this.name = inName;
                this._hashCode = this.type.GetHashCode() ^ this.name.GetHashCode();
            }

            public override int GetHashCode() {
                return this._hashCode;
            }
        }

        private static readonly Dictionary<FieldKey, FieldInfo> _fieldCache = new Dictionary<FieldKey, FieldInfo>();

        public static object GetField(this object self, string name, Type type = null) {
            if (null == type) {
                type = self.GetType();
            }
            if (!self.SearchForFields(name)) {
                Console.WriteLine("[KK_Extension] Field Not Found: " + name);
                return false;
            }
            FieldKey key = new FieldKey(type, name);
            if (_fieldCache.TryGetValue(key, out FieldInfo info) == false) {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            return info.GetValue(self);
        }

        public static bool SetField(this object self, string name, object value, Type type = null) {
            if (null == type) {
                type = self.GetType();
            }
            if (!self.SearchForFields(name)) {
                Console.WriteLine("[KK_Extension] Field Not Found: " + name);
                return false;
            }
            FieldKey fieldKey = new FieldKey(type, name);
            if (_fieldCache.TryGetValue(fieldKey, out FieldInfo field) == false) {
                field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (null != field) {
                    _fieldCache.Add(fieldKey, field);
                } else {
                    Console.WriteLine("[KK_Extension] Set Field Not Found: " + name);
                    return false;
                }
            }
            try {
                field.SetValue(self, value);
                return true;
            } catch (ArgumentException ae) {
                Console.WriteLine("[KK_Extension] Set Field is not the same type as input: " + name);
                Console.WriteLine("[KK_Extension] " + ae.Message);
                return false;
            }
        }

        public static bool SetProperty(this object self, string name, object value) {
            if (!self.SearchForProperties(name)) {
                Console.WriteLine("[KK_Extension] Field Not Found: " + name);
                return false;
            }
            PropertyInfo propertyInfo;
            propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            if (null != propertyInfo) {
                propertyInfo.SetValue(self, value, null);
                return true;
            } else {
                Console.WriteLine("[KK_Extension] Set Property Not Found: " + name);
                return false;
            }
        }
        public static object GetProperty(this object self, string name) {
            if (!self.SearchForProperties(name)) {
                Console.WriteLine("[KK_Extension] Property Not Found: " + name);
                return false;
            }
            PropertyInfo propertyInfo;
            propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            return propertyInfo.GetValue(self, null);
        }
        public static object Invoke(this object self, string name, object[] p = null) {
            try {
                return self?.GetType().InvokeMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, self, p);
            } catch (MissingMethodException e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
                MemberInfo[] members = self?.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod);
                List<string> printArray = new List<string>();
                foreach (MemberInfo me in members) {
                    if (me.Name == name) {
                        return true;
                    }
                    printArray.Add("[KK_Extension] Member Name/Type: " + me.Name + " / " + me.MemberType);
                }
                foreach (string st in printArray) {
                    Console.WriteLine(st);
                }
                Console.WriteLine("[KK_Extension] Get " + members.Length + " Members.");
                return false;
            }
        }

        //List all the fields inside the object if name not found.
        public static bool SearchForFields(this object self, string name) {
            FieldInfo[] fieldInfos = self.GetType().GetFields(AccessTools.all);
            List<string> printArray = new List<string>();
            foreach (FieldInfo fi in fieldInfos) {
                if (fi.Name == name) {
                    return true;
                }
                printArray.Add("[KK_Extension] Field Name/Type: " + fi.Name + " / " + fi.FieldType);
            }
            Console.WriteLine("[KK_Extension] Get " + fieldInfos.Length + " Fields.");

            foreach (string st in printArray) {
                Console.WriteLine(st);
            }
            return false;
        }

        //List all the fields inside the object if name not found.
        public static bool SearchForProperties(this object self, string name) {
            PropertyInfo[] propertyInfos = self.GetType().GetProperties(AccessTools.all);
            List<string> printArray = new List<string>();
            foreach (PropertyInfo pi in propertyInfos) {
                if (pi.Name == name) {
                    return true;
                }
                printArray.Add("[KK_Extension] Property Name/Type: " + pi.Name + " / " + pi.PropertyType);
            }
            Console.WriteLine("[KK_Extension] Get " + propertyInfos.Length + " Properties.");

            foreach (string st in printArray) {
                Console.WriteLine(st);
            }
            return false;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this object self) {
            if (!(self is IDictionary dictionary)) {
                Console.WriteLine("[KK_Extension] Faild to cast to Dictionary!");
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
                Console.WriteLine("[KK_Extension] Faild to cast to List!");
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
                //Console.WriteLine(interfaceType.ToString());
                if (interfaceType.IsGenericType &&
                   interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
                    Type KeyType = type.GetGenericArguments()[0];
                    Type ValueType = type.GetGenericArguments()[1];
                    MethodInfo method = typeof(Extension).GetMethod(nameof(Extension.ToDictionary), BindingFlags.Public | BindingFlags.Static);
                    if (null != KeyType && null != ValueType) {
                        method = method.MakeGenericMethod(new Type[] { KeyType, ValueType });
                    }

                    return method.Invoke(null, new object[] { self });
                }
            }
            Console.WriteLine("[KK_Extension] Faild to cast to Dictionary<unknown>!");
            return null;
        }

        public static bool TryGetValue(this object self, object key, out object value) {
            value = null;
            if (self is IDictionary dic) {
                Type keyType = null;
                foreach (Type interfaceType in self.GetType().GetInterfaces()) {
                    if (interfaceType.IsGenericType &&
                       interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
                        keyType = self.GetType().GetGenericArguments()[0];
                    }
                }

                if (null != keyType && key.GetType() == keyType && dic.Contains(key)) {
                    value = dic[key];
                    return true;
                    //Console.WriteLine($"[KK_Extension] AddRange: Add {listToAdd.Count} item.");
                } else {
                    Console.WriteLine($"[KK_Extension] Key not found! Cannot Get {key.GetType().FullName} from object");
                }
            } else {
                Console.WriteLine($"[KK_Extension] Type is not Dictionary! Cannot Get {key.GetType().FullName} from object");
            }
            return false;
        }
        #endregion

        #region Fake Linq for "List<unknown>" which cast into "object"
        public static object ToListWithoutType(this object self) {
            Type type = self.GetType();
            foreach (Type interfaceType in type.GetInterfaces()) {
                if (interfaceType.IsGenericType &&
                   interfaceType.GetGenericTypeDefinition() == typeof(IList<>)) {
                    Type itemType = type.GetGenericArguments()[0];
                    MethodInfo method = typeof(Extension).GetMethod("ToList", BindingFlags.Public | BindingFlags.Static);
                    if (null != itemType) {
                        method = method.MakeGenericMethod(itemType);
                    }

                    return method.Invoke(null, new object[] { self });
                }
            }
            Console.WriteLine("[KK_Extension] Faild to cast to List<unknown>!");
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
                    //Console.WriteLine($"[KK_Extension] AddRange: Add {listToAdd.Count} item.");
                } else {
                    Console.WriteLine($"[KK_Extension] Type not Match! Cannot Add {addItemType.FullName} into {selfItemType.FullName}");
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
                        //Console.WriteLine($"[KK_Extension] Remove at {i}/{list.Count}");
                    }
                }
            } else if (self is IDictionary dic) {
                Queue<object> keysToRemove = new Queue<object>();
                foreach (object key in dic.Keys) {
                    if (match(new KeyValuePair<object, object>(key, dic[key]))) {
                        keysToRemove.Enqueue(key);
                        //Console.WriteLine($"[KK_Extension] Remove at {i}/{list.Count}");
                    }
                }
                amount = keysToRemove.Count;
                while (keysToRemove.Count > 0) {
                    object key = keysToRemove.Dequeue();
                    dic.Remove(key);
                }
            } else {
                Console.WriteLine($"[KK_Extension] RemoveAll: Input Object is not type of List<unknown> or Dictionary<unknown>!");
            }
            //Console.WriteLine($"[KK_Extension] RemoveAll: Output Obj Count {list.Count}");
            return amount;
        }

        public static int Count(this object self) {
            if (self is IList list) {
                return list.Count;
            }
            if (self is IDictionary dic) {
                return dic.Count;
            } else {
                Console.WriteLine($"[KK_Extension] Count: Input Object is not type of List<unknown> or Dictionary<unknown>!");
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
                Console.WriteLine($"[KK_Extension] Select: Input Object is not type of List<unknown>!");
                return null;
            }
        }

        public static object ForEach(this object self, Action<object> action) {
            if(self is IList list) {
                foreach (object l in list) {
                    action(l);
                }
            }else if (self is IDictionary dic) {
                foreach (DictionaryEntry  d in dic) {
                    action(d);
                }
            } else {
                Console.WriteLine($"[KK_Extension] Select: Input Object is not type of Dictionary<unknown> or List<unknown>!");
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
                Console.WriteLine($"[KK_Extension] Where: Input Object is not type of List<unknown>!");
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
                    //Console.WriteLine($"[KK_Extension] AddRange: Add {listToAdd.Count} item.");
                } else {
                    Console.WriteLine($"[KK_Extension] Type not Match! Cannot Add {obj2Add.GetType().FullName} into {selfItemType.FullName}");
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
                    //Console.WriteLine($"[KK_Extension] AddRange: Add {listToAdd.Count} item.");
                } else {
                    Console.WriteLine($"[KK_Extension] Type not Match! Cannot Add <{key2Add.GetType().FullName}, <{value2Add.GetType().FullName}> into Dictionary<{keyType.FullName}, {valueType.FullName}>");
                }
            }
        }

        #endregion

        #region Picture Stuff
        public static Sprite LoadNewSprite(string FilePath, int width, int height, float PixelsPerUnit = 100.0f) {
            Sprite NewSprite;
            Texture2D SpriteTexture = LoadTexture(FilePath);
            if (null == SpriteTexture) {
                SpriteTexture = LoadDllResource(FilePath, width, height);
            }
            NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit);

            return NewSprite;
        }

        // Load a PNG or JPG file from disk to a Texture2D
        // Returns null if load fails
        public static Texture2D LoadTexture(string FilePath) {
            Texture2D Tex2D;
            byte[] FileData;

            if (File.Exists(FilePath)) {
                FileData = File.ReadAllBytes(FilePath);
                Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
                if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
                    return Tex2D;                 // If data = readable -> return texture
            }
            return null;                     // Return null if load failed
        }

        public static Texture2D LoadDllResource(string FilePath, int width, int height) {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream myStream = myAssembly.GetManifestResourceStream(FilePath);
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.LoadImage(ReadToEnd(myStream));

            if (texture == null) {
                Console.WriteLine("Missing Dll resource: " + FilePath);
            }

            return texture;
        }

        static byte[] ReadToEnd(Stream stream) {
            long originalPosition = stream.Position;
            stream.Position = 0;

            try {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0) {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length) {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1) {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead) {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            } finally {
                stream.Position = originalPosition;
            }
        }

        public static Texture2D AddWatermark(Texture2D background, Texture2D watermark, int startX, int startY) {
            Texture2D newTex = new Texture2D(background.width, background.height, background.format, false);
            for (int x = 0; x < background.width; x++) {
                for (int y = 0; y < background.height; y++) {
                    if (x >= startX && y >= startY && x - startX < watermark.width && y - startY < watermark.height) {
                        Color bgColor = background.GetPixel(x, y);
                        Color wmColor = watermark.GetPixel(x - startX, y - startY);

                        Color final_color = Color.Lerp(bgColor, wmColor, wmColor.a / 1.0f);

                        newTex.SetPixel(x, y, final_color);
                    } else
                        newTex.SetPixel(x, y, background.GetPixel(x, y));
                }
            }

            newTex.Apply();
            return newTex;
        }
        #endregion

        public static BaseUnityPlugin TryGetPluginInstance(string pluginName, Version minimumVersion = null) {
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(pluginName, out PluginInfo target);
            if (null != target) {
                if (target.Metadata.Version >= minimumVersion) {
                    return target.Instance;
                }
                Console.WriteLine($"[KK_Extension] {pluginName} v{target.Metadata.Version.ToString()} is detacted OUTDATED.");
                Console.WriteLine($"[KK_Extension] Please update {pluginName} to at least v{minimumVersion.ToString()} to enable related feature.");
            }
            return null;
        }
    }
}