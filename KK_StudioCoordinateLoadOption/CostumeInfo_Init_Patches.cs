using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Studio;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Illusion.Game;

namespace KK_StudioCoordinateLoadOption
{
	internal class CostumeInfo_Init_Patches
	{
		internal static void ManualPatch(HarmonyInstance harmony)
		{
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(CostumeInfo_Init_Patches), "Postfix", null), null);
		}

		private static void Postfix(object __instance)
		{
            Console.WriteLine("KK_SCLO:Patcher Start");
			CharaFileSort charaFileSort = (CharaFileSort)__instance.GetPrivate("fileSort");
			Transform parent = charaFileSort.root.parent;

            Type ClothesKind = typeof(ChaFileDefine.ClothesKind);
            Array ClothesKindArray = Enum.GetValues(ClothesKind);

            Image panel = UILib.UIUtility.CreatePanel("TooglePanel", parent.parent.parent);
            Toggle toggleAll = UILib.UIUtility.CreateToggle("ToggleAll", panel.transform, "all");
            toggleAll.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
            toggleAll.GetComponentInChildren<Text>(true).color = Color.white;
            toggleAll.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 20f+20f*ClothesKindArray.Length), new Vector2(0f, 45f+20f*(ClothesKindArray.Length)));
            toggleAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));

            for (int i = 0; i < ClothesKindArray.Length; i++) { 
                Toggle toggleTmp = UILib.UIUtility.CreateToggle("Toggle"+i, panel.transform, ClothesKindArray.GetValue(i).ToString());
                toggleTmp.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                toggleTmp.GetComponentInChildren<Text>(true).color = Color.white;
                toggleTmp.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 20f*(ClothesKindArray.Length-i)), new Vector2(0f, 25f+20f*(ClothesKindArray.Length-i)));
                toggleTmp.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));
            }

            Toggle toggleAcc = UILib.UIUtility.CreateToggle("ToggleAcc", panel.transform, "accessories");
            toggleAcc.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
            toggleAcc.GetComponentInChildren<Text>(true).color = Color.white;
            toggleAcc.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 0f), new Vector2(0f, 25f));
            toggleAcc.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));

			panel.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(407f, 290f-ClothesKindArray.Length*20), new Vector2(150f, 340f));
            panel.GetComponent<Image>().color = new Color32(80,80,80,220);

		}

		private static void SearchUpdated(string text, List<CharaFileInfo> items)
		{
			foreach (CharaFileInfo charaFileInfo in items)
			{
				charaFileInfo.node.gameObject.SetActive(charaFileInfo.node.text.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1);
			}
		}
	}
}
