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
	// Token: 0x02000017 RID: 23
	internal static class Extensions
	{
		// Token: 0x0600005D RID: 93 RVA: 0x000073E8 File Offset: 0x000055E8
		public static void SetPrivateExplicit<T>(this T self, string name, object value)
		{
			Extensions.FieldKey fieldKey = new Extensions.FieldKey(typeof(T), name);
			FieldInfo field;
			if (!Extensions._fieldCache.TryGetValue(fieldKey, out field))
			{
				field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				Extensions._fieldCache.Add(fieldKey, field);
			}
			field.SetValue(self, value);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x0000744C File Offset: 0x0000564C
		public static void SetPrivate(this object self, string name, object value)
		{
			Extensions.FieldKey fieldKey = new Extensions.FieldKey(self.GetType(), name);
			FieldInfo field;
			if (!Extensions._fieldCache.TryGetValue(fieldKey, out field))
			{
				field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				Extensions._fieldCache.Add(fieldKey, field);
			}
			field.SetValue(self, value);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x000074A8 File Offset: 0x000056A8
		public static object GetPrivateExplicit<T>(this T self, string name)
		{
			Extensions.FieldKey fieldKey = new Extensions.FieldKey(typeof(T), name);
			FieldInfo field;
			if (!Extensions._fieldCache.TryGetValue(fieldKey, out field))
			{
				field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				Extensions._fieldCache.Add(fieldKey, field);
			}
			return field.GetValue(self);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x0000750C File Offset: 0x0000570C
		public static object GetPrivate(this object self, string name)
		{
			Extensions.FieldKey fieldKey = new Extensions.FieldKey(self.GetType(), name);
			FieldInfo field;
			if (!Extensions._fieldCache.TryGetValue(fieldKey, out field))
			{
				field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				Extensions._fieldCache.Add(fieldKey, field);
			}
			return field.GetValue(self);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00007564 File Offset: 0x00005764
		public static object CallPrivate(this object self, string name, params object[] p)
		{
			return self.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Invoke(self, p);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x0000757C File Offset: 0x0000577C
		public static void LoadWith<T>(this T to, T from)
		{
			foreach (FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (fieldInfo.FieldType.IsArray)
				{
					Array array = (Array)fieldInfo.GetValue(from);
					Array array2 = Array.CreateInstance(fieldInfo.FieldType.GetElementType(), array.Length);
					for (int j = 0; j < array.Length; j++)
					{
						array2.SetValue(array.GetValue(j), j);
					}
				}
				else
				{
					fieldInfo.SetValue(to, fieldInfo.GetValue(from));
				}
			}
		}

		// Token: 0x06000063 RID: 99 RVA: 0x0000763C File Offset: 0x0000583C
		public static void ReplaceEventsOf(this object self, object obj)
		{
			foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
			{
				for (int j = 0; j < button.onClick.GetPersistentEventCount(); j++)
				{
					if (button.onClick.GetPersistentTarget(j) == obj)
					{
						(button.onClick.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[j].SetPrivate("m_Target", self);
					}
				}
			}
			foreach (Slider slider in Resources.FindObjectsOfTypeAll<Slider>())
			{
				for (int k = 0; k < slider.onValueChanged.GetPersistentEventCount(); k++)
				{
					if (slider.onValueChanged.GetPersistentTarget(k) == obj)
					{
						(slider.onValueChanged.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[k].SetPrivate("m_Target", self);
					}
				}
			}
			foreach (InputField inputField in Resources.FindObjectsOfTypeAll<InputField>())
			{
				for (int l = 0; l < inputField.onEndEdit.GetPersistentEventCount(); l++)
				{
					if (inputField.onEndEdit.GetPersistentTarget(l) == obj)
					{
						(inputField.onEndEdit.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[l].SetPrivate("m_Target", self);
					}
				}
				for (int m = 0; m < inputField.onValueChanged.GetPersistentEventCount(); m++)
				{
					if (inputField.onValueChanged.GetPersistentTarget(m) == obj)
					{
						(inputField.onValueChanged.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[m].SetPrivate("m_Target", self);
					}
				}
				if (inputField.onValidateInput != null && inputField.onValidateInput.Target == obj)
				{
					inputField.onValidateInput.SetPrivate("_target", obj);
				}
			}
			foreach (Toggle toggle in Resources.FindObjectsOfTypeAll<Toggle>())
			{
				for (int n = 0; n < toggle.onValueChanged.GetPersistentEventCount(); n++)
				{
					if (toggle.onValueChanged.GetPersistentTarget(n) == obj)
					{
						(toggle.onValueChanged.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[n].SetPrivate("m_Target", self);
					}
				}
			}
			foreach (UI_OnEnableEvent ui_OnEnableEvent in Resources.FindObjectsOfTypeAll<UI_OnEnableEvent>())
			{
				for (int num = 0; num < ui_OnEnableEvent._event.GetPersistentEventCount(); num++)
				{
					if (ui_OnEnableEvent._event.GetPersistentTarget(num) == obj)
					{
						(ui_OnEnableEvent._event.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[num].SetPrivate("m_Target", self);
					}
				}
			}
			EventTrigger[] array6 = Resources.FindObjectsOfTypeAll<EventTrigger>();
			for (int i = 0; i < array6.Length; i++)
			{
				foreach (EventTrigger.Entry entry in array6[i].triggers)
				{
					for (int num2 = 0; num2 < entry.callback.GetPersistentEventCount(); num2++)
					{
						if (entry.callback.GetPersistentTarget(num2) == obj)
						{
							(entry.callback.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[num2].SetPrivate("m_Target", self);
						}
					}
				}
			}
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00007A44 File Offset: 0x00005C44
		public static string GetPathFrom(this Transform self, Transform root, bool includeRoot = false)
		{
			if (self == root)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(self.name);
			Transform parent = self.parent;
			while (parent != root)
			{
				stringBuilder.Insert(0, "/");
				stringBuilder.Insert(0, parent.name);
				parent = parent.parent;
			}
			if (parent != null && includeRoot)
			{
				stringBuilder.Insert(0, "/");
				stringBuilder.Insert(0, root.name);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00007ADC File Offset: 0x00005CDC
		public static bool IsChildOf(this Transform self, string parent)
		{
			while (self != null)
			{
				if (self.name.Equals(parent))
				{
					return true;
				}
				self = self.parent;
			}
			return false;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00007B08 File Offset: 0x00005D08
		public static string GetPathFrom(this Transform self, string root, bool includeRoot = false)
		{
			if (self.name.Equals(root))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(self.name);
			Transform parent = self.parent;
			while (parent != null && !parent.name.Equals(root))
			{
				stringBuilder.Insert(0, "/");
				stringBuilder.Insert(0, parent.name);
				parent = parent.parent;
			}
			if (parent != null && includeRoot)
			{
				stringBuilder.Insert(0, "/");
				stringBuilder.Insert(0, root);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00007BB0 File Offset: 0x00005DB0
		public static List<int> GetListPathFrom(this Transform self, Transform root)
		{
			List<int> list = new List<int>();
			Transform transform = self;
			while (transform != root)
			{
				list.Add(transform.GetSiblingIndex());
				transform = transform.parent;
			}
			list.Reverse();
			return list;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00007BF4 File Offset: 0x00005DF4
		public static Transform Find(this Transform self, List<int> path)
		{
			Transform transform = self;
			for (int i = 0; i < path.Count; i++)
			{
				transform = transform.GetChild(path[i]);
			}
			return transform;
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00007C2C File Offset: 0x00005E2C
		public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, int frameCount = 1)
		{
			return self.StartCoroutine(Extensions.ExecuteDelayed_Routine(action, 1));
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00007C3C File Offset: 0x00005E3C
		private static IEnumerator ExecuteDelayed_Routine(Action action, int frameCount = 1)
		{
			int num;
			for (int i = 0; i < frameCount; i = num + 1)
			{
				yield return null;
				num = i;
			}
			action();
			yield break;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00007C54 File Offset: 0x00005E54
		public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, float delay, bool timeScaled = true)
		{
			return self.StartCoroutine(Extensions.ExecuteDelayed_Routine(action, delay, timeScaled));
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00007C64 File Offset: 0x00005E64
		private static IEnumerator ExecuteDelayed_Routine(Action action, float delay, bool timeScaled)
		{
			if (timeScaled)
			{
				yield return new WaitForSeconds(delay);
			}
			else
			{
				yield return new WaitForSecondsRealtime(delay);
			}
			action();
			yield break;
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00007C84 File Offset: 0x00005E84
		public static Coroutine ExecuteDelayedFixed(this MonoBehaviour self, Action action)
		{
			return self.StartCoroutine(Extensions.ExecuteDelayedFixed_Routine(action));
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00007C94 File Offset: 0x00005E94
		private static IEnumerator ExecuteDelayedFixed_Routine(Action action)
		{
			yield return new WaitForFixedUpdate();
			action();
			yield break;
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00007CA4 File Offset: 0x00005EA4
		public static Coroutine ExecuteDelayed(this MonoBehaviour self, Func<bool> waitUntil, Action action)
		{
			return self.StartCoroutine(Extensions.ExecuteDelayed_Routine(waitUntil, action));
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00007CB4 File Offset: 0x00005EB4
		private static IEnumerator ExecuteDelayed_Routine(Func<bool> waitUntil, Action action)
		{
			yield return new WaitUntil(waitUntil);
			action();
			yield break;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00007CCC File Offset: 0x00005ECC
		public static Transform FindDescendant(this Transform self, string name)
		{
			if (self.name.Equals(name))
			{
				return self;
			}
			foreach (object obj in self)
			{
				Transform transform = ((Transform)obj).FindDescendant(name);
				if (transform != null)
				{
					return transform;
				}
			}
			return null;
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00007D54 File Offset: 0x00005F54
		public static XmlNode FindChildNode(this XmlNode self, string name)
		{
			if (!self.HasChildNodes)
			{
				return null;
			}
			foreach (object obj in self.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name.Equals(name))
				{
					return xmlNode;
				}
			}
			return null;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00007DD8 File Offset: 0x00005FD8
		public static void Resize<T>(this List<T> self, int newSize)
		{
			int num = self.Count - newSize;
			if (num < 0)
			{
				while (self.Count != newSize)
				{
					self.Add(default(T));
				}
				return;
			}
			if (num > 0)
			{
				while (self.Count != newSize)
				{
					self.RemoveRange(newSize, num);
				}
			}
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00007E34 File Offset: 0x00006034
		public static Transform GetFirstLeaf(this Transform self)
		{
			while (self.childCount != 0)
			{
				self = self.GetChild(0);
			}
			return self;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00007E50 File Offset: 0x00006050
		public static int IndexOf<T>(this T[] self, T obj)
		{
			for (int i = 0; i < self.Length; i++)
			{
				if (self[i].Equals(obj))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0400004F RID: 79
		private static readonly Dictionary<Extensions.FieldKey, FieldInfo> _fieldCache = new Dictionary<Extensions.FieldKey, FieldInfo>();

		// Token: 0x0200003A RID: 58
		private struct FieldKey
		{
			// Token: 0x06000178 RID: 376 RVA: 0x0000F568 File Offset: 0x0000D768
			public FieldKey(Type inType, string inName)
			{
				this.type = inType;
				this.name = inName;
				this._hashCode = (this.type.GetHashCode() ^ this.name.GetHashCode());
			}

			// Token: 0x06000179 RID: 377 RVA: 0x0000F598 File Offset: 0x0000D798
			public override int GetHashCode()
			{
				return this._hashCode;
			}

			// Token: 0x04000110 RID: 272
			public readonly Type type;

			// Token: 0x04000111 RID: 273
			public readonly string name;

			// Token: 0x04000112 RID: 274
			private readonly int _hashCode;
		}
	}
}
