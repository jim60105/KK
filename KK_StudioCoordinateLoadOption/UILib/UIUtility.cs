using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
	// Token: 0x02000027 RID: 39
	internal static class UIUtility
	{
		// Token: 0x06000120 RID: 288 RVA: 0x0000DF04 File Offset: 0x0000C104
		public static void Init()
		{
			if (UIUtility._initCalled)
			{
				return;
			}
			UIUtility._initCalled = true;
			AssetBundle assetBundle = AssetBundle.LoadFromMemory(UILib.Properties.Resources.DefaultResources);
			foreach (Sprite sprite in assetBundle.LoadAllAssets<Sprite>())
			{
				string name = sprite.name;
				uint num = UIUtility.PrivateImplementationDetails.ComputeStringHash(name);
				if (num <= 3137079997u)
				{
					if (num != 2856253361u)
					{
						if (num != 3123409217u)
						{
							if (num == 3137079997u)
							{
								if (name == "Background")
								{
									UIUtility.backgroundSprite = sprite;
								}
							}
						}
						else if (name == "Knob")
						{
							UIUtility.knob = sprite;
						}
					}
					else if (name == "DropdownArrow")
					{
						UIUtility.dropdownArrow = sprite;
					}
				}
				else if (num <= 4166605170u)
				{
					if (num != 4163407581u)
					{
						if (num == 4166605170u)
						{
							if (name == "Checkmark")
							{
								UIUtility.checkMark = sprite;
							}
						}
					}
					else if (name == "InputFieldBackground")
					{
						UIUtility.inputFieldBackground = sprite;
					}
				}
				else if (num != 4251428655u)
				{
					if (num == 4274317666u)
					{
						if (name == "UISprite")
						{
							UIUtility.standardSprite = sprite;
						}
					}
				}
				else if (name == "UIMask")
				{
					UIUtility.mask = sprite;
				}
			}
			UIUtility.defaultFont = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
			UIUtility.resources = new DefaultControls.Resources
			{
				background = UIUtility.backgroundSprite,
				checkmark = UIUtility.checkMark,
				dropdown = UIUtility.dropdownArrow,
				inputField = UIUtility.inputFieldBackground,
				knob = UIUtility.knob,
				mask = UIUtility.mask,
				standard = UIUtility.standardSprite
			};
			UIUtility.defaultFontSize = 16;
			assetBundle.Unload(false);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000E144 File Offset: 0x0000C344
		public static Canvas CreateNewUISystem(string name = "NewUISystem")
		{
			GameObject gameObject = new GameObject(name, new Type[]
			{
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster)
			});
			Canvas component = gameObject.GetComponent<Canvas>();
			component.renderMode = RenderMode.ScreenSpaceOverlay;
			CanvasScaler component2 = gameObject.GetComponent<CanvasScaler>();
			component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			component2.referencePixelsPerUnit = 100f;
			component2.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
			GraphicRaycaster component3 = gameObject.GetComponent<GraphicRaycaster>();
			component3.ignoreReversedGraphics = true;
			component3.blockingObjects = GraphicRaycaster.BlockingObjects.None;
			return component;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000E1C8 File Offset: 0x0000C3C8
		public static void SetCustomFont(string customFontName)
		{
			foreach (Font font in UnityEngine.Resources.FindObjectsOfTypeAll<Font>())
			{
				if (font.name.Equals(customFontName))
				{
					UIUtility.defaultFont = font;
				}
			}
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000E210 File Offset: 0x0000C410
		public static RectTransform CreateNewUIObject()
		{
			return UIUtility.CreateNewUIObject(null, "UIObject");
		}

		// Token: 0x06000124 RID: 292 RVA: 0x0000E220 File Offset: 0x0000C420
		public static RectTransform CreateNewUIObject(string name)
		{
			return UIUtility.CreateNewUIObject(null, name);
		}

		// Token: 0x06000125 RID: 293 RVA: 0x0000E22C File Offset: 0x0000C42C
		public static RectTransform CreateNewUIObject(Transform parent)
		{
			return UIUtility.CreateNewUIObject(parent, "UIObject");
		}

		// Token: 0x06000126 RID: 294 RVA: 0x0000E23C File Offset: 0x0000C43C
		public static RectTransform CreateNewUIObject(Transform parent, string name)
		{
			RectTransform component = new GameObject(name, new Type[]
			{
				typeof(RectTransform)
			}).GetComponent<RectTransform>();
			if (parent != null)
			{
				component.SetParent(parent, false);
				component.localPosition = Vector3.zero;
				component.localScale = Vector3.one;
			}
			return component;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x0000E298 File Offset: 0x0000C498
		public static InputField CreateInputField(string objectName = "New Input Field", Transform parent = null, string placeholder = "Placeholder...")
		{
			GameObject gameObject = DefaultControls.CreateInputField(UIUtility.resources);
			gameObject.name = objectName;
			foreach (Text text in gameObject.GetComponentsInChildren<Text>(true))
			{
				text.font = UIUtility.defaultFont;
				text.resizeTextForBestFit = true;
				text.resizeTextMinSize = 2;
				text.resizeTextMaxSize = 100;
				text.alignment = TextAnchor.MiddleLeft;
				text.rectTransform.offsetMin = new Vector2(5f, 2f);
				text.rectTransform.offsetMax = new Vector2(-5f, -2f);
			}
			gameObject.transform.SetParent(parent, false);
			InputField component = gameObject.GetComponent<InputField>();
			component.placeholder.GetComponent<Text>().text = placeholder;
			return component;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000E35C File Offset: 0x0000C55C
		public static Button CreateButton(string objectName = "New Button", Transform parent = null, string buttonText = "Button")
		{
			GameObject gameObject = DefaultControls.CreateButton(UIUtility.resources);
			gameObject.name = objectName;
			Text componentInChildren = gameObject.GetComponentInChildren<Text>(true);
			componentInChildren.font = UIUtility.defaultFont;
			componentInChildren.resizeTextForBestFit = true;
			componentInChildren.resizeTextMinSize = 2;
			componentInChildren.resizeTextMaxSize = 100;
			componentInChildren.alignment = TextAnchor.MiddleCenter;
			componentInChildren.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
			componentInChildren.text = buttonText;
			gameObject.transform.SetParent(parent, false);
			return gameObject.GetComponent<Button>();
		}

		// Token: 0x06000129 RID: 297 RVA: 0x0000E3F8 File Offset: 0x0000C5F8
		public static Image CreateImage(string objectName = "New Image", Transform parent = null, Sprite sprite = null)
		{
			GameObject gameObject = DefaultControls.CreateImage(UIUtility.resources);
			gameObject.name = objectName;
			gameObject.transform.SetParent(parent, false);
			Image component = gameObject.GetComponent<Image>();
			component.sprite = sprite;
			return component;
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000E434 File Offset: 0x0000C634
		public static Text CreateText(string objectName = "New Text", Transform parent = null, string textText = "Text")
		{
			GameObject gameObject = DefaultControls.CreateText(UIUtility.resources);
			gameObject.name = objectName;
			Text componentInChildren = gameObject.GetComponentInChildren<Text>(true);
			componentInChildren.font = UIUtility.defaultFont;
			componentInChildren.resizeTextForBestFit = true;
			componentInChildren.resizeTextMinSize = 2;
			componentInChildren.resizeTextMaxSize = 100;
			componentInChildren.alignment = TextAnchor.UpperLeft;
			componentInChildren.text = textText;
			componentInChildren.color = UIUtility.whiteColor;
			gameObject.transform.SetParent(parent, false);
			return componentInChildren;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0000E4A8 File Offset: 0x0000C6A8
		public static Toggle CreateToggle(string objectName = "New Toggle", Transform parent = null, string label = "Label")
		{
			GameObject gameObject = DefaultControls.CreateToggle(UIUtility.resources);
			gameObject.name = objectName;
			Text componentInChildren = gameObject.GetComponentInChildren<Text>(true);
			componentInChildren.font = UIUtility.defaultFont;
			componentInChildren.resizeTextForBestFit = true;
			componentInChildren.resizeTextMinSize = 2;
			componentInChildren.resizeTextMaxSize = 100;
			componentInChildren.alignment = TextAnchor.MiddleCenter;
			componentInChildren.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(23f, 1f), new Vector2(-5f, -2f));
			componentInChildren.text = label;
			gameObject.transform.SetParent(parent, false);
			return gameObject.GetComponent<Toggle>();
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0000E544 File Offset: 0x0000C744
		public static Dropdown CreateDropdown(string objectName = "New Dropdown", Transform parent = null, string label = "Label")
		{
			GameObject gameObject = DefaultControls.CreateDropdown(UIUtility.resources);
			gameObject.name = objectName;
			foreach (Text text in gameObject.GetComponentsInChildren<Text>(true))
			{
				text.font = UIUtility.defaultFont;
				text.resizeTextForBestFit = true;
				text.resizeTextMinSize = 2;
				text.resizeTextMaxSize = 100;
				text.alignment = TextAnchor.MiddleLeft;
				if (text.name.Equals("Label"))
				{
					text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(10f, 6f), new Vector2(-25f, -7f));
				}
				else
				{
					text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(20f, 1f), new Vector2(-10f, -2f));
				}
			}
			gameObject.transform.SetParent(parent, false);
			return gameObject.GetComponent<Dropdown>();
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000E648 File Offset: 0x0000C848
		public static RawImage CreateRawImage(string objectName = "New Raw Image", Transform parent = null, Texture texture = null)
		{
			GameObject gameObject = DefaultControls.CreateRawImage(UIUtility.resources);
			gameObject.name = objectName;
			gameObject.transform.SetParent(parent, false);
			RawImage component = gameObject.GetComponent<RawImage>();
			component.texture = texture;
			return component;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x0000E684 File Offset: 0x0000C884
		public static Scrollbar CreateScrollbar(string objectName = "New Scrollbar", Transform parent = null)
		{
			GameObject gameObject = DefaultControls.CreateScrollbar(UIUtility.resources);
			gameObject.name = objectName;
			gameObject.transform.SetParent(parent, false);
			return gameObject.GetComponent<Scrollbar>();
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000E6B8 File Offset: 0x0000C8B8
		public static ScrollRect CreateScrollView(string objectName = "New ScrollView", Transform parent = null)
		{
			GameObject gameObject = DefaultControls.CreateScrollView(UIUtility.resources);
			gameObject.name = objectName;
			gameObject.transform.SetParent(parent, false);
			return gameObject.GetComponent<ScrollRect>();
		}

		// Token: 0x06000130 RID: 304 RVA: 0x0000E6EC File Offset: 0x0000C8EC
		public static Slider CreateSlider(string objectName = "New Slider", Transform parent = null)
		{
			GameObject gameObject = DefaultControls.CreateSlider(UIUtility.resources);
			gameObject.name = objectName;
			gameObject.transform.SetParent(parent, false);
			return gameObject.GetComponent<Slider>();
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000E720 File Offset: 0x0000C920
		public static Image CreatePanel(string objectName = "New Panel", Transform parent = null)
		{
			GameObject gameObject = DefaultControls.CreatePanel(UIUtility.resources);
			gameObject.name = objectName;
			gameObject.transform.SetParent(parent, false);
			return gameObject.GetComponent<Image>();
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000E754 File Offset: 0x0000C954
		public static Outline AddOutlineToObject(Transform t)
		{
			return UIUtility.AddOutlineToObject(t, Color.black, new Vector2(1f, -1f));
		}

		// Token: 0x06000133 RID: 307 RVA: 0x0000E770 File Offset: 0x0000C970
		public static Outline AddOutlineToObject(Transform t, Color c)
		{
			return UIUtility.AddOutlineToObject(t, c, new Vector2(1f, -1f));
		}

		// Token: 0x06000134 RID: 308 RVA: 0x0000E788 File Offset: 0x0000C988
		public static Outline AddOutlineToObject(Transform t, Vector2 effectDistance)
		{
			return UIUtility.AddOutlineToObject(t, Color.black, effectDistance);
		}

		// Token: 0x06000135 RID: 309 RVA: 0x0000E798 File Offset: 0x0000C998
		public static Outline AddOutlineToObject(Transform t, Color color, Vector2 effectDistance)
		{
			Outline outline = t.gameObject.AddComponent<Outline>();
			outline.effectColor = color;
			outline.effectDistance = effectDistance;
			return outline;
		}

		// Token: 0x06000136 RID: 310 RVA: 0x0000E7C4 File Offset: 0x0000C9C4
		public static Toggle AddCheckboxToObject(Transform tr)
		{
			Toggle toggle = tr.gameObject.AddComponent<Toggle>();
			RectTransform rectTransform = UIUtility.CreateNewUIObject(tr.transform, "Background");
			toggle.targetGraphic = UIUtility.AddImageToObject(rectTransform, UIUtility.standardSprite);
			RectTransform rectTransform2 = UIUtility.CreateNewUIObject(rectTransform, "CheckMark");
			Image image = UIUtility.AddImageToObject(rectTransform2, UIUtility.checkMark);
			image.color = Color.black;
			toggle.graphic = image;
			rectTransform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
			rectTransform2.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
			return toggle;
		}

		// Token: 0x06000137 RID: 311 RVA: 0x0000E860 File Offset: 0x0000CA60
		public static Image AddImageToObject(Transform t, Sprite sprite = null)
		{
			Image image = t.gameObject.AddComponent<Image>();
			image.type = Image.Type.Sliced;
			image.fillCenter = true;
			image.color = UIUtility.whiteColor;
			image.sprite = ((sprite == null) ? UIUtility.backgroundSprite : sprite);
			return image;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000E8B4 File Offset: 0x0000CAB4
		public static MovableWindow MakeObjectDraggable(RectTransform clickableDragZone, RectTransform draggableObject, bool preventCameraControl = true)
		{
			MovableWindow movableWindow = clickableDragZone.gameObject.AddComponent<MovableWindow>();
			movableWindow.toDrag = draggableObject;
			movableWindow.preventCameraControl = preventCameraControl;
			return movableWindow;
		}

		// Token: 0x040000D3 RID: 211
		public const RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;

		// Token: 0x040000D4 RID: 212
		public const bool canvasPixelPerfect = false;

		// Token: 0x040000D5 RID: 213
		public const CanvasScaler.ScaleMode canvasScalerUiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

		// Token: 0x040000D6 RID: 214
		public const float canvasScalerReferencePixelsPerUnit = 100f;

		// Token: 0x040000D7 RID: 215
		public const bool graphicRaycasterIgnoreReversedGraphics = true;

		// Token: 0x040000D8 RID: 216
		public const GraphicRaycaster.BlockingObjects graphicRaycasterBlockingObjects = GraphicRaycaster.BlockingObjects.None;

		// Token: 0x040000D9 RID: 217
		public static Sprite checkMark;

		// Token: 0x040000DA RID: 218
		public static Sprite backgroundSprite;

		// Token: 0x040000DB RID: 219
		public static Sprite standardSprite;

		// Token: 0x040000DC RID: 220
		public static Sprite inputFieldBackground;

		// Token: 0x040000DD RID: 221
		public static Sprite knob;

		// Token: 0x040000DE RID: 222
		public static Sprite dropdownArrow;

		// Token: 0x040000DF RID: 223
		public static Sprite mask;

		// Token: 0x040000E0 RID: 224
		public static readonly Color whiteColor = new Color(1f, 1f, 1f);

		// Token: 0x040000E1 RID: 225
		public static readonly Color grayColor = new Color32(100, 99, 95, byte.MaxValue);

		// Token: 0x040000E2 RID: 226
		public static readonly Color lightGrayColor = new Color32(150, 149, 143, byte.MaxValue);

		// Token: 0x040000E3 RID: 227
		public static readonly Color greenColor = new Color32(0, 160, 0, byte.MaxValue);

		// Token: 0x040000E4 RID: 228
		public static readonly Color lightGreenColor = new Color32(0, 200, 0, byte.MaxValue);

		// Token: 0x040000E5 RID: 229
		public static readonly Color purpleColor = new Color(0f, 0.007f, 1f, 0.545f);

		// Token: 0x040000E6 RID: 230
		public static readonly Color transparentGrayColor = new Color32(100, 99, 95, 90);

		// Token: 0x040000E7 RID: 231
		public static Font defaultFont;

		// Token: 0x040000E8 RID: 232
		public static int defaultFontSize;

		// Token: 0x040000E9 RID: 233
		public static DefaultControls.Resources resources;

		// Token: 0x040000EA RID: 234
		private static bool _initCalled = false;

		// Token: 0x02000042 RID: 66
		public enum Binary
		{
			// Token: 0x04000131 RID: 305
			Neo,
			// Token: 0x04000132 RID: 306
			Game
		}
        // Token: 0x0200002A RID: 42
        [CompilerGenerated]
        internal sealed class PrivateImplementationDetails
        {
            // Token: 0x06000153 RID: 339 RVA: 0x0000ED8C File Offset: 0x0000CF8C
            internal static uint ComputeStringHash(string s)
            {
                uint num = 0;
                if (s != null)
                {
                    num = 2166136261u;
                    for (int i = 0; i < s.Length; i++)
                    {
                        num = ((uint)s[i] ^ num) * 16777619u;
                    }
                }
                return num;
            }
        }
    }
}
