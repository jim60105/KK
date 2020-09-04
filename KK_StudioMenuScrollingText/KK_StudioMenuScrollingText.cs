/*
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMM               MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM    M7    MZ    MMO    MMMMM
MMM               MMMMMMMMMMMMM   MMM     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMM               MMMMMMMMMM       ?M     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMM8     MMMMMMMM       ~MMM.    MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MMMMM        MMM                 M    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MM.         ZMMMMMM     MMMM     MMMMMMMMMMMMZ    MMO    MMMMM
MMMMMMMMMMMMM     MM      .   ZMMMMMM     MMMM     MMMMMMMMMMMM?    MMO    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    $MMMMMM     MMMM     MMMMMMMMMMMM?    MM8    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    7MMMMMM     MMMM     MMMMMMMMMMMMI    MM8    MMMMM
MMM               MMMMMMMM    7MMMMMM     MMMM    .MMMMMMMMMMMM.    MMMM?ZMMMMMM
MMM               MMMMMMMM.   ?MMMMMM     MMMM     MMMMMMMMMM ,:MMMMMM?    MMMMM
MMM           ..MMMMMMMMMM    =MMMMMM     MMMM     M$ MM$M7M $MOM MMMM     ?MMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM .+Z: M   :M M  MM   ?MMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
*/

using BepInEx;
//using BepInEx.Logging;
using Extension;
using HarmonyLib;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioMenuScrollingText {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioMenuScrollingText : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Menu Scrolling Text";
        internal const string GUID = "com.jim60105.kk.studiomenuscrollingtext";
        internal const string PLUGIN_VERSION = "20.09.04.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.0";

        //internal static new ManualLogSource Logger;
        public void Awake() {
            //Logger = base.Logger;
            Extension.Extension.LogPrefix = $"[{PLUGIN_NAME}]";
            Harmony.CreateAndPatchAll(typeof(Patches));
        }
    }

    internal class ScrollingListNode : ListNode {
        const int DELAY = 10;
        private string FullText;
        private int anchor = 0;
        private int delay = DELAY;
        private int segmentLength = 14;

        public void Set(ListNode node, int s) {
            this.SetField<ListNode>("button", node.GetField("button"));
            this.SetField<ListNode>("content", node.GetField("content"));
            this.SetField<ListNode>("imageSelect", node.GetField("imageSelect"));
            this.SetField<ListNode>("textMesh", node.GetField("textMesh"));
            segmentLength = s;
            if (System.Text.Encoding.Default.GetBytes(node.text).Length > segmentLength) {
                FullText = node.text + "  " + node.text;
                Text t = this.GetField<ListNode>("content") as Text ?? this.GetField<ListNode>("textMesh") as Text ?? this.gameObject.GetComponentInChildren<Text>();
                t.resizeTextForBestFit = false;
                t.fontSize = 13;
                t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        public void Update() {
            if (null != FullText && delay-- < 0) {
                if (anchor > ((FullText.Length - 2) / 2)) anchor = 0;
                base.text = FullText.Substring(anchor, segmentLength);
                anchor++;
                delay = DELAY;
                //KK_StudioMenuScrollingText.Logger.LogDebug(base.text);
            }
        }

        public void OnDisable() {
            System.GC.Collect();
        }
    }

    class Patches {
        [HarmonyPostfix, HarmonyPatch(typeof(ItemGroupList), "InitList")]
        public static void InitListPostfix(ItemGroupList __instance) => InitItemListPostfix(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(ItemCategoryList), "InitList")]
        public static void InitListPostfix2(ItemCategoryList __instance) => InitItemListPostfix(__instance);

        public static void InitItemListPostfix(MonoBehaviour __instance) {
            Transform parent = __instance.GetField("transformRoot") as Transform;
            ListNode[] listNodes = parent.gameObject.GetComponentsInChildren<ListNode>();
            foreach (ListNode node in listNodes) {
                ScrollingListNode SLN = parent.gameObject.AddComponent(typeof(ScrollingListNode)) as ScrollingListNode;
                SLN.Set(node, 13);
                GameObject.Destroy(node);
            }
        }
    }
}
