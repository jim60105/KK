//From: https://bitbucket.org/Joan6694/hsplugins/src/0754ed3c991e/HSUS/?at=master
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToolBox
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

        public static void SetPrivateExplicit<T>(this T self, string name, object value)
        {
            FieldKey key = new FieldKey(typeof(T), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            info.SetValue(self, value);
        }
        public static void SetPrivate(this object self, string name, object value)
        {
            FieldKey key = new FieldKey(self.GetType(), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            info.SetValue(self, value);
        }
        public static object GetPrivateExplicit<T>(this T self, string name)
        {
            FieldKey key = new FieldKey(typeof(T), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            return info.GetValue(self);
        }
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

        public static object CallPrivate(this object self, string name, params object[] p)
        {
            return self.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Invoke(self, p);
        }

        public static void LoadWith<T>(this T to, T from)
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo fi in fields)
            {
                if (fi.FieldType.IsArray)
                {
                    Array arr = (Array)fi.GetValue(from);
                    Array arr2 = Array.CreateInstance(fi.FieldType.GetElementType(), arr.Length);
                    for (int i = 0; i < arr.Length; i++)
                        arr2.SetValue(arr.GetValue(i), i);
                }
                else
                    fi.SetValue(to, fi.GetValue(from));
            }
        }

        public static void ReplaceEventsOf(this object self, object obj)
        {
            foreach (Button b in Resources.FindObjectsOfTypeAll<Button>())
            {
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onClick.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onClick.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
            foreach (Slider b in Resources.FindObjectsOfTypeAll<Slider>())
            {
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
            foreach (InputField b in Resources.FindObjectsOfTypeAll<InputField>())
            {
                for (int i = 0; i < b.onEndEdit.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onEndEdit.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onEndEdit.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
                if (b.onValidateInput != null && ReferenceEquals(b.onValidateInput.Target, obj))
                {
                    b.onValidateInput.SetPrivate("_target", obj);
                }
            }
            foreach (Toggle b in Resources.FindObjectsOfTypeAll<Toggle>())
            {
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }

            foreach (UI_OnEnableEvent b in Resources.FindObjectsOfTypeAll<UI_OnEnableEvent>())
            {
                for (int i = 0; i < b._event.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b._event.GetPersistentTarget(i), obj))
                    {
                        IList objects = b._event.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }

            foreach (EventTrigger b in Resources.FindObjectsOfTypeAll<EventTrigger>())
            {
                foreach (EventTrigger.Entry et in b.triggers)
                {
                    for (int i = 0; i < et.callback.GetPersistentEventCount(); ++i)
                    {
                        if (ReferenceEquals(et.callback.GetPersistentTarget(i), obj))
                        {
                            IList objects = et.callback.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                            objects[i].SetPrivate("m_Target", self);
                        }
                    }
                }
            }
        }
        public static string GetPathFrom(this Transform self, Transform root, bool includeRoot = false)
        {
            if (self == root)
                return "";
            Transform self2 = self;
            StringBuilder path = new StringBuilder(self2.name);
            self2 = self2.parent;
            while (self2 != root)
            {
                path.Insert(0, "/");
                path.Insert(0, self2.name);
                self2 = self2.parent;
            }
            if (self2 != null && includeRoot)
            {
                path.Insert(0, "/");
                path.Insert(0, root.name);
            }
            return path.ToString();
        }

        public static bool IsChildOf(this Transform self, string parent)
        {
            while (self != null)
            {
                if (self.name.Equals(parent))
                    return true;
                self = self.parent;
            }
            return false;
        }

        public static string GetPathFrom(this Transform self, string root, bool includeRoot = false)
        {
            if (self.name.Equals(root))
                return "";
            Transform self2 = self;
            StringBuilder path = new StringBuilder(self2.name);
            self2 = self2.parent;
            while (self2 != null && self2.name.Equals(root) == false)
            {
                path.Insert(0, "/");
                path.Insert(0, self2.name);
                self2 = self2.parent;
            }
            if (self2 != null && includeRoot)
            {
                path.Insert(0, "/");
                path.Insert(0, root);
            }
            return path.ToString();
        }

        public static List<int> GetListPathFrom(this Transform self, Transform root)
        {
            List<int> path = new List<int>();
            Transform self2 = self;
            while (self2 != root)
            {
                path.Add(self2.GetSiblingIndex());
                self2 = self2.parent;
            }
            path.Reverse();
            return path;
        }

        public static Transform Find(this Transform self, List<int> path)
        {
            Transform self2 = self;
            for (int i = 0; i < path.Count; i++)
                self2 = self2.GetChild(path[i]);
            return self2;
        }

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, int frameCount = 1)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(action));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, int frameCount = 1)
        {
            for (int i = 0; i < frameCount; i++)
                yield return null;
            action();
        }

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, float delay, bool timeScaled = true)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(action, delay, timeScaled));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, float delay, bool timeScaled)
        {
            if (timeScaled)
                yield return new WaitForSeconds(delay);
            else
                yield return new WaitForSecondsRealtime(delay);
            action();
        }

        public static Coroutine ExecuteDelayedFixed(this MonoBehaviour self, Action action)
        {
            return self.StartCoroutine(ExecuteDelayedFixed_Routine(action));
        }

        private static IEnumerator ExecuteDelayedFixed_Routine(Action action)
        {
            yield return new WaitForFixedUpdate();
            action();
        }

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Func<bool> waitUntil, Action action)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(waitUntil, action));
        }

        private static IEnumerator ExecuteDelayed_Routine(Func<bool> waitUntil, Action action)
        {
            yield return new WaitUntil(waitUntil);
            action();
        }

        public static Transform FindDescendant(this Transform self, string name)
        {
            if (self.name.Equals(name))
                return self;
            foreach (Transform t in self)
            {
                Transform res = t.FindDescendant(name);
                if (res != null)
                    return res;
            }
            return null;
        }

        public static XmlNode FindChildNode(this XmlNode self, string name)
        {
            if (self.HasChildNodes == false)
                return null;
            foreach (XmlNode chilNode in self.ChildNodes)
                if (chilNode.Name.Equals(name))
                    return chilNode;
            return null;
        }

        public static void Resize<T>(this List<T> self, int newSize)
        {
            int diff = self.Count - newSize;
            if (diff < 0)
                while (self.Count != newSize)
                    self.Add(default(T));
            else if (diff > 0)
                while (self.Count != newSize)
                    self.RemoveRange(newSize, diff);
        }

        public static Transform GetFirstLeaf(this Transform self)
        {
            while (self.childCount != 0)
                self = self.GetChild(0);
            return self;
        }


        public static int IndexOf<T>(this T[] self, T obj)
        {
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i].Equals(obj))
                    return i;
            }
            return -1;
        }
    }
}
