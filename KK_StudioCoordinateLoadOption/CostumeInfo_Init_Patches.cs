using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Studio;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

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
            //(parent as RectTransform).offsetMin += new Vector2(0f, 18f);
            int amount = 7;
            Image panel = UILib.UIUtility.CreatePanel("TooglePanel", parent.parent.parent);
			panel.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(407f, 330f-amount*20), new Vector2(100f, 340f));
            panel.GetComponent<Image>().color = new Color32(80,80,80,220);

            for (int i = 0; i < amount; i++) { 
                Toggle toggleTmp = UILib.UIUtility.CreateToggle("Toggle"+i, panel.transform, "toggle第"+i);
                toggleTmp.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                toggleTmp.GetComponentInChildren<Text>(true).color = Color.white;
                toggleTmp.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, -20f+20f*(amount-i)), new Vector2(0f, 5f+20f*(amount-i)));
                toggleTmp.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));
            }
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
