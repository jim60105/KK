using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UILib
{
	// Token: 0x02000028 RID: 40
	internal static class UIExtensions
	{
		// Token: 0x0600013A RID: 314 RVA: 0x0000E9A8 File Offset: 0x0000CBA8
		public static void SetRect(this RectTransform self, Vector2 anchorMin)
		{
			self.SetRect(anchorMin, Vector2.one, Vector2.zero, Vector2.zero);
		}

		// Token: 0x0600013B RID: 315 RVA: 0x0000E9C0 File Offset: 0x0000CBC0
		public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax)
		{
			self.SetRect(anchorMin, anchorMax, Vector2.zero, Vector2.zero);
		}

		// Token: 0x0600013C RID: 316 RVA: 0x0000E9D4 File Offset: 0x0000CBD4
		public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin)
		{
			self.SetRect(anchorMin, anchorMax, offsetMin, Vector2.zero);
		}

		// Token: 0x0600013D RID: 317 RVA: 0x0000E9E4 File Offset: 0x0000CBE4
		public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
		{
			self.anchorMin = anchorMin;
			self.anchorMax = anchorMax;
			self.offsetMin = offsetMin;
			self.offsetMax = offsetMax;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000EA14 File Offset: 0x0000CC14
		public static void SetRect(this RectTransform self, RectTransform other)
		{
			self.anchorMin = other.anchorMin;
			self.anchorMax = other.anchorMax;
			self.offsetMin = other.offsetMin;
			self.offsetMax = other.offsetMax;
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000EA58 File Offset: 0x0000CC58
		public static void SetRect(this RectTransform self, float anchorLeft = 0f, float anchorBottom = 0f, float anchorRight = 1f, float anchorTop = 1f, float offsetLeft = 0f, float offsetBottom = 0f, float offsetRight = 0f, float offsetTop = 0f)
		{
			self.anchorMin = new Vector2(anchorLeft, anchorBottom);
			self.anchorMax = new Vector2(anchorRight, anchorTop);
			self.offsetMin = new Vector2(offsetLeft, offsetBottom);
			self.offsetMax = new Vector2(offsetRight, offsetTop);
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000EAA4 File Offset: 0x0000CCA4
		public static void SetRect(this Transform self, Transform other)
		{
			(self as RectTransform).SetRect(other as RectTransform);
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000EAB8 File Offset: 0x0000CCB8
		public static void SetRect(this Transform self, Vector2 anchorMin)
		{
			(self as RectTransform).SetRect(anchorMin, Vector2.one, Vector2.zero, Vector2.zero);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000EAD8 File Offset: 0x0000CCD8
		public static void SetRect(this Transform self, Vector2 anchorMin, Vector2 anchorMax)
		{
			(self as RectTransform).SetRect(anchorMin, anchorMax, Vector2.zero, Vector2.zero);
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000EAF4 File Offset: 0x0000CCF4
		public static void SetRect(this Transform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin)
		{
			(self as RectTransform).SetRect(anchorMin, anchorMax, offsetMin, Vector2.zero);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000EB0C File Offset: 0x0000CD0C
		public static void SetRect(this Transform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
		{
			RectTransform rectTransform = self as RectTransform;
			rectTransform.anchorMin = anchorMin;
			rectTransform.anchorMax = anchorMax;
			rectTransform.offsetMin = offsetMin;
			rectTransform.offsetMax = offsetMax;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000EB40 File Offset: 0x0000CD40
		public static void SetRect(this Transform self, float anchorLeft = 0f, float anchorBottom = 0f, float anchorRight = 1f, float anchorTop = 1f, float offsetLeft = 0f, float offsetBottom = 0f, float offsetRight = 0f, float offsetTop = 0f)
		{
			RectTransform rectTransform = self as RectTransform;
			rectTransform.anchorMin = new Vector2(anchorLeft, anchorBottom);
			rectTransform.anchorMax = new Vector2(anchorRight, anchorTop);
			rectTransform.offsetMin = new Vector2(offsetLeft, offsetBottom);
			rectTransform.offsetMax = new Vector2(offsetRight, offsetTop);
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000EB90 File Offset: 0x0000CD90
		public static Button LinkButtonTo(this Transform root, string path, UnityAction onClick)
		{
			Button component = root.Find(path).GetComponent<Button>();
			if (onClick != null)
			{
				component.onClick.AddListener(onClick);
			}
			return component;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000EBC4 File Offset: 0x0000CDC4
		public static Dropdown LinkDropdownTo(this Transform root, string path, UnityAction<int> onValueChanged)
		{
			Dropdown component = root.Find(path).GetComponent<Dropdown>();
			if (onValueChanged != null)
			{
				component.onValueChanged.AddListener(onValueChanged);
			}
			return component;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000EBF8 File Offset: 0x0000CDF8
		public static InputField LinkInputFieldTo(this Transform root, string path, UnityAction<string> onValueChanged, UnityAction<string> onEndEdit)
		{
			InputField component = root.Find(path).GetComponent<InputField>();
			if (onValueChanged != null)
			{
				component.onValueChanged.AddListener(onValueChanged);
			}
			if (onEndEdit != null)
			{
				component.onEndEdit.AddListener(onEndEdit);
			}
			return component;
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000EC3C File Offset: 0x0000CE3C
		public static ScrollRect LinkScrollViewTo(this Transform root, string path, UnityAction<Vector2> onValueChanged)
		{
			ScrollRect component = root.Find(path).GetComponent<ScrollRect>();
			if (onValueChanged != null)
			{
				component.onValueChanged.AddListener(onValueChanged);
			}
			return component;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000EC70 File Offset: 0x0000CE70
		public static Scrollbar LinkScrollbarTo(this Transform root, string path, UnityAction<float> onValueChanged)
		{
			Scrollbar component = root.Find(path).GetComponent<Scrollbar>();
			if (onValueChanged != null)
			{
				component.onValueChanged.AddListener(onValueChanged);
			}
			return component;
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000ECA4 File Offset: 0x0000CEA4
		public static Slider LinkSliderTo(this Transform root, string path, UnityAction<float> onValueChanged)
		{
			Slider component = root.Find(path).GetComponent<Slider>();
			if (onValueChanged != null)
			{
				component.onValueChanged.AddListener(onValueChanged);
			}
			return component;
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000ECD8 File Offset: 0x0000CED8
		public static Toggle LinkToggleTo(this Transform root, string path, UnityAction<bool> onValueChanged)
		{
			Toggle component = root.Find(path).GetComponent<Toggle>();
			if (onValueChanged != null)
			{
				component.onValueChanged.AddListener(onValueChanged);
			}
			return component;
		}
	}
}
