using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Harmony;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace Extension {
    public static class Extension {
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
        public static object GetField(this object self, string name) {
            if (!self.SearchForFields(name)) {
                Logger.Log(LogLevel.Error, "[KK] Field Not Found: " + name);
                return false;
            }
            FieldKey key = new FieldKey(self.GetType(), name);
            if (_fieldCache.TryGetValue(key, out FieldInfo info) == false) {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            return info.GetValue(self);
        }
        public static bool SetField(this object self, string name, object value) {
            if (!self.SearchForFields(name)) {
                Logger.Log(LogLevel.Error, "[KK] Field Not Found: " + name);
                return false;
            }
            FieldKey fieldKey = new FieldKey(self.GetType(), name);
            if (!_fieldCache.TryGetValue(fieldKey, out FieldInfo field)) {
                field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (null != field) {
                    _fieldCache.Add(fieldKey, field);
                    field.SetValue(self, value);
                    return true;
                } else {
                    Logger.Log(LogLevel.Error, "[KK] Set Field Not Found: " + name);
                }
            }
            return false;
        }
        public static bool SetProperty(this object self, string name, object value) {
            if (!self.SearchForProperties(name)) {
                Logger.Log(LogLevel.Error, "[KK] Field Not Found: " + name);
                return false;
            }
            PropertyInfo propertyInfo;
            propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            if (null != propertyInfo) {
                propertyInfo.SetValue(self, value, null);
                return true;
            } else {
                Logger.Log(LogLevel.Error, "[KK] Set Property Not Found: " + name);
                return false;
            }
        }
        public static object GetProperty(this object self, string name) {
            if (!self.SearchForProperties(name)) {
                Logger.Log(LogLevel.Error, "[KK] Field Not Found: " + name);
                return false;
            }
            PropertyInfo propertyInfo;
            propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            return propertyInfo.GetValue(self, null);
        }
        public static object Invoke(this object self, string name, object[] p = null) {
            return self.GetType().InvokeMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, self, p);
        }

        //List all the fields inside the object if name not found.
        public static bool SearchForFields(this object self, string name) {
            FieldInfo[] fieldInfos = self.GetType().GetFields(AccessTools.all);
            List<string> printArray = new List<string>();
            foreach (var fi in fieldInfos) {
                if (fi.Name == name) {
                    return true;
                }
                printArray.Add("[KK] Field Name/Type: " + fi.Name + " / " + fi.FieldType);
            }
            Logger.Log(LogLevel.Debug, "[KK] Get " + fieldInfos.Length + " Fields.");

            foreach (string st in printArray) {
                Logger.Log(LogLevel.Debug, st);
            }
            return false;
        }

        //List all the fields inside the object if name not found.
        public static bool SearchForProperties(this object self, string name) {
            PropertyInfo[] propertyInfos = self.GetType().GetProperties(AccessTools.all);
            List<string> printArray = new List<string>();
            foreach (var pi in propertyInfos) {
                if (pi.Name == name) {
                    return true;
                }
                printArray.Add("[KK] Property Name/Type: " + pi.Name + " / " + pi.PropertyType);
            }
            Logger.Log(LogLevel.Debug, "[KK] Get " + propertyInfos.Length + " Fields.");

            foreach (string st in printArray) {
                Logger.Log(LogLevel.Debug, st);
            }
            return false;
        }

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
                Logger.Log(LogLevel.Error, "Missing Dll resource: " + FilePath);
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

    }
}