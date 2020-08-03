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
        private struct Key {
            public readonly Type type;
            public readonly string name;
            private readonly int _hashCode;

            public Key(Type inType, string inName) {
                type = inType;
                name = inName;
                _hashCode = type.GetHashCode() ^ name.GetHashCode();
            }

            public override int GetHashCode() {
                return _hashCode;
            }
        }

        private static readonly Dictionary<Key, FieldInfo> _fieldCache = new Dictionary<Key, FieldInfo>();
        private static readonly Dictionary<Key, PropertyInfo> _propertyCache = new Dictionary<Key, PropertyInfo>();
        private static readonly Dictionary<Key, MethodInfo> _methodCache = new Dictionary<Key, MethodInfo>();

        public static object GetField(this object self, string name) {
            //Console.WriteLine("GetField with no type");
            Type type = self.GetType();
            MethodInfo method = typeof(Extension).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Extension.GetField) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(self, new object[] { self, name });
        }
        public static object GetField<T>(this object self, string name) where T : class {
            Key fieldKey = new Key(typeof(T), name);
            //Console.WriteLine("GetField on " + typeof(T).FullName);
            if (!SearchForFields(fieldKey)) {
                //Console.WriteLine("GetField failed " + typeof(T).FullName);
                return null;
            }
            _fieldCache.TryGetValue(fieldKey, out FieldInfo field);
            //Console.WriteLine("GetField GetValue " + field.Name);
            return field.GetValue(self as T) ?? null;
        }

        public static bool SetField(this object self, string name, object value) {
            //Console.WriteLine("SetField with no type");
            Type type = self.GetType();
            MethodInfo method = typeof(Extension).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Extension.SetField) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return (bool)method.Invoke(self, new object[] { self, name, value });
        }
        public static bool SetField<T>(this object self, string name, object value) where T : class {
            Key fieldKey = new Key(typeof(T), name);
            //Console.WriteLine("SetField on " + typeof(T).FullName);
            if (!SearchForFields(fieldKey)) {
                //Console.WriteLine("SetField failed " + typeof(T).FullName);
                return false;
            }
            _fieldCache.TryGetValue(fieldKey, out FieldInfo field);
            //Console.WriteLine("SetField SetValue " + field.Name);
            try {
                field.SetValue(self, value);
                return true;
            } catch (ArgumentException ae) {
                Console.WriteLine("[KK_Extension] Set Field is not the same type as input: " + name);
                Console.WriteLine("[KK_Extension] " + ae.Message);
                return false;
            }
        }

        public static object GetProperty(this object self, string name) {
            Type type = self.GetType();
            MethodInfo method = typeof(Extension).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Extension.GetProperty) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(self, new object[] { self, name });
        }
        public static object GetProperty<T>(this object self, string name) where T : class {
            Key key = new Key(typeof(T), name);
            if (!SearchForProperties(key)) {
                return null;
            }

            _propertyCache.TryGetValue(key, out PropertyInfo info);
            return info.GetValue(self as T, null) ?? null;
        }

        public static bool SetProperty(this object self, string name, object value) {
            Type type = self.GetType();
            MethodInfo method = typeof(Extension).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Extension.SetProperty) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return (bool) method.Invoke(self, new object[] { self, name, value });
        }
        public static bool SetProperty<T>(this object self, string name, object value) where T : class {
            Key key = new Key(typeof(T), name);
            if (!SearchForProperties(key)) {
                return false;
            }

            _propertyCache.TryGetValue(key, out PropertyInfo property);
            try {
                property.SetValue(self as T, value, null);
                return true;
            } catch (ArgumentException ae) {
                Console.WriteLine("[KK_Extension] Set Property is not the same type as input: " + name);
                Console.WriteLine("[KK_Extension] " + ae.Message);
                return false;
            }
        }

        public static object Invoke(this object self, string name, object[] p = null) {
            Type type = self.GetType();
            MethodInfo method = typeof(Extension).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Extension._Invoke) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(self, new object[] { self, name, p });
        }
        public static object Invoke<T>(this object self, string name, object[] p = null) where T : class => _Invoke<T>(self, name, p);

        public static object InvokeStatic(Type type, string name, object[] p = null) {
            MethodInfo method = typeof(Extension).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == nameof(Extension._Invoke) && m.IsGenericMethod).First();
            method = method.MakeGenericMethod(type);

            return method.Invoke(null, new object[] { null, name, p });
        }

        private static object _Invoke<T>(object self, string name, object[] p = null) where T : class {
            Key key = new Key(typeof(T), name);
            if (!SearchForMethod(key)) {
                return null;
            }

            _methodCache.TryGetValue(key, out MethodInfo methodInfo);
            try {
                if (null != self) {
                    return methodInfo.Invoke(self as T, p);
                } else if (methodInfo.IsStatic) {
                    return methodInfo.Invoke(null, p);
                } else { throw new Exception("Invoke as static on a not static method"); }
            } catch (ArgumentException ae) {
                Console.WriteLine("[KK_Extension] Invoke Method is not the same type as input: " + name);
                Console.WriteLine("[KK_Extension] " + ae.Message);
            } catch (Exception e) {
                Console.WriteLine("[KK_Extension] " + e.Message);
            }
            return null;
        }

        private static bool SearchForFields(Key fieldKey) {
            if (_fieldCache.ContainsKey(fieldKey)) {
                return true;
            } else {
                FieldInfo[] fieldInfos = fieldKey.type.GetFields(AccessTools.all);
                List<string> printArray = new List<string>();
                foreach (FieldInfo fi in fieldInfos) {
                    if (fi.Name == fieldKey.name) {
                        _fieldCache.Add(fieldKey, fi);
                        return true;
                    }
                    printArray.Add("[KK_Extension] Field Name/Type: " + fi.Name + " / " + fi.FieldType);
                }
                Console.WriteLine($"[KK_Extension] Field Not Found on {fieldKey.type.FullName}: {fieldKey.name}");
                Console.WriteLine("[KK_Extension] Get " + fieldInfos.Length + " Fields.");

                foreach (string st in printArray) {
                    Console.WriteLine(st);
                }
                return false;
            }
        }

        private static bool SearchForProperties(Key propertyKey) {
            if (_propertyCache.ContainsKey(propertyKey)) {
                return true;
            } else {
                PropertyInfo[] propertyInfos = propertyKey.type.GetProperties(AccessTools.all);
                List<string> printArray = new List<string>();
                foreach (PropertyInfo pi in propertyInfos) {
                    if (pi.Name == propertyKey.name) {
                        _propertyCache.Add(propertyKey, pi);
                        return true;
                    }
                    printArray.Add("[KK_Extension] Property Name/Type: " + pi.Name + " / " + pi.PropertyType);
                }
                Console.WriteLine($"[KK_Extension] Property Not Found on {nameof(propertyKey.type)}: " + propertyKey.name);
                Console.WriteLine("[KK_Extension] Get " + propertyInfos.Length + " Properties.");

                foreach (string st in printArray) {
                    Console.WriteLine(st);
                }
                return false;
            }
        }

        private static bool SearchForMethod(Key methodKey) {
            if (_methodCache.ContainsKey(methodKey)) {
                return true;
            } else {
                MethodInfo[] methodInfos = methodKey.type.GetMethods(AccessTools.all);
                List<string> printArray = new List<string>();
                foreach (MethodInfo me in methodInfos) {
                    if (me.Name == methodKey.name) {
                        _methodCache.Add(methodKey, me);
                        return true;
                    }
                    printArray.Add("[KK_Extension] Method Name/Type: " + me.Name + " / " + me.MemberType);
                }
                Console.WriteLine($"[KK_Extension] Invoke Method Not Found on {nameof(methodKey.type)}: " + methodKey.name);
                Console.WriteLine("[KK_Extension] Get " + methodInfos.Length + " Members.");
                foreach (string st in printArray) {
                    Console.WriteLine(st);
                }
                return false;
            }
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
            if (self is IList list) {
                foreach (object l in list) {
                    action(l);
                }
            } else if (self is IDictionary dic) {
                foreach (DictionaryEntry d in dic) {
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

        /// <summary>
        /// Load a PNG or JPG file to a Sprite 
        /// </summary>
        /// <param name="FilePath">Can be a filepath or a embedded resource path</param>
        /// <returns>Texture, or Null if load fails</returns>
        public static Sprite LoadNewSprite(string FilePath, int width, int height, float PixelsPerUnit = 100.0f) {
            Sprite NewSprite;
            Texture2D SpriteTexture = LoadTexture(FilePath);
            if (null == SpriteTexture) {
                SpriteTexture = LoadDllResource(FilePath);
            }
            NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, width, height), new Vector2(0, 0), PixelsPerUnit);

            return NewSprite;
        }

        /// <summary>
        /// Load a PNG or JPG file from disk to a Texture2D
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns>Texture, or Null if load fails</returns>
        public static Texture2D LoadTexture(string FilePath, int width = -1, int height = -1) {
            Texture2D texture;
            byte[] FileData;

            if (File.Exists(FilePath)) {
                FileData = File.ReadAllBytes(FilePath);
                texture = new Texture2D(2, 2);
                if (texture.LoadImage(FileData)) {
                    if ((width > 0 && texture.width != width) || (height > 0 && texture.height != height)) {
                        texture = Scale(texture, width > 0 ? width : texture.width, height > 0 ? height : texture.height);
                    }
                    return texture;
                }
            }
            return null;
        }

        /// <summary>
        /// Load a embedded PNG or JPG resource to a Texture2D
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns>Texture, or Null if load fails</returns>
        public static Texture2D LoadDllResource(string FilePath, int width = -1, int height = -1) {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Texture2D texture = new Texture2D(2, 2);
            using (Stream myStream = myAssembly.GetManifestResourceStream(FilePath)) {
                if (texture.LoadImage(ReadToEnd(myStream))) {
                    if ((width > 0 && texture.width != width) || (height > 0 && texture.height != height)) {
                        texture = Scale(texture, width > 0 ? width : texture.width, height > 0 ? height : texture.height);
                    }
                    return texture;
                } else {
                    Console.WriteLine("Missing Dll resource: " + FilePath);
                }
            }
            return null;
        }

        public static byte[] ReadToEnd(Stream stream) {
            long originalPosition = stream.Position;
            //stream.Position = 0;

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

        /// <summary>
        ///	Returns a scaled copy of given texture. 
        /// </summary>
        /// <param name="tex">Source texure to scale</param>
        /// <param name="width">Destination texture width</param>
        /// <param name="height">Destination texture height</param>
        /// <param name="mode">Filtering mode</param>
        public static Texture2D Scale(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear) {
            Rect texR = new Rect(0, 0, width, height);
            _gpu_scale(src, width, height, mode);

            //Get rendered data back to a new texture
            Texture2D result = new Texture2D(width, height, src.format, true);
            result.Resize(width, height);
            result.ReadPixels(texR, 0, 0, true);
            return result;
        }

        ///// <summary>
        ///// Scales the texture data of the given texture.
        ///// </summary>
        ///// <param name="tex">Texure to scale</param>
        ///// <param name="width">New width</param>
        ///// <param name="height">New height</param>
        ///// <param name="mode">Filtering mode</param>
        //public static void Scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear) {
        //    Rect texR = new Rect(0, 0, width, height);
        //    _gpu_scale(tex, width, height, mode);

        //    // Update new texture
        //    tex.Resize(width, height);
        //    tex.ReadPixels(texR, 0, 0, true);
        //    tex.Apply(true);    //Remove this if you hate us applying textures for you :)
        //}

        private static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode = FilterMode.Trilinear) {
            //We need the source texture in VRAM because we render with it
            src.filterMode = fmode;
            src.Apply(true);

            //Using RTT for best quality and performance. Thanks, Unity 5
            RenderTexture rtt = new RenderTexture(width, height, 32);

            //Set the RTT in order to render to it
            Graphics.SetRenderTarget(rtt);

            //Setup 2D matrix in range 0..1, so nobody needs to care about sized
            GL.LoadPixelMatrix(0, 1, 1, 0);

            //Then clear & draw the texture to fill the entire RTT.
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
        }

        public static Texture2D OverwriteTexture(Texture2D background, Texture2D watermark, int startX, int startY) {
            Texture2D newTex = new Texture2D(background.width, background.height, background.format, false);
            for (int x = 0; x < background.width; x++) {
                for (int y = 0; y < background.height; y++) {
                    if (x >= startX && y >= startY && x - startX < watermark.width && y - startY < watermark.height) {
                        Color bgColor = background.GetPixel(x, y);
                        Color wmColor = watermark.GetPixel(x - startX, y - startY);

                        Color final_color = Color.Lerp(bgColor, wmColor, wmColor.a);
                        final_color.a = bgColor.a + wmColor.a;

                        newTex.SetPixel(x, y, final_color);
                    } else {
                        newTex.SetPixel(x, y, background.GetPixel(x, y));
                    }
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

        //public static bool IsSteam() {
        //    if (typeof(DownloadScene).GetProperty("isSteam", AccessTools.all) != null) {
        //        Console.WriteLine("[KK_Extension] This Plugin is not working in Koikatu Party (Steam version)");
        //        return true;
        //    }
        //    return false;
        //}

        //public static bool IsDarkness() {
        //    if (null == typeof(ChaFileParameter).GetProperty("exType")) {
        //        Console.WriteLine("[KK_Extension] This Plugin is not working without Darkness.");
        //        return false;
        //    }
        //    return true;
        //}
    }
}