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
using BepInEx.Configuration;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioMenuScrollingText {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioMenuScrollingText : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Menu Scrolling Text";
        internal const string GUID = "com.jim60105.kk.studiomenuscrollingtext";
        internal const string PLUGIN_VERSION = "20.09.05.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.0";

        public static ConfigEntry<string> AddtionalFolder { get; private set; }
        internal readonly static Dictionary<int, string> HeaderDict = new Dictionary<int, string>();
        internal readonly static Dictionary<int, string> FooterDict = new Dictionary<int, string>();

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            Extension.Extension.LogPrefix = $"[{PLUGIN_NAME}]";

            AddtionalFolder = Config.Bind<string>("Config", "Menu Addtional Text Folder", GetRelativePath(BepInEx.Paths.BepInExRootPath, Path.Combine(Path.GetDirectoryName(base.Info.Location), nameof(KK_StudioMenuScrollingText))));
            if (!Directory.Exists(AddtionalFolder.Value)) {
                Directory.CreateDirectory(AddtionalFolder.Value);
            } else {
                foreach (string path in Directory.GetFiles(AddtionalFolder.Value).ToList<string>().Where(s => s.EndsWith(".csv"))) {
                    try {
                        File.ReadAllLines(path).Select(a => a.Split(',')).ToList().ForEach(s => {
                            if (s[0] == "Before") {
                                HeaderDict[int.Parse(s[1])] = s[2];
                            } else if (s[0] == "After") {
                                FooterDict[int.Parse(s[1])] = s[2];
                            }
                        });
                    } catch (Exception) {
                        Logger.LogWarning($"Load addtional text faild in: {path}");
                        return;
                    };
                }
            }

            Harmony.CreateAndPatchAll(typeof(Patches));
        }

        static string GetRelativePath(string basePath, string targetPath) {
            Uri baseUri = new Uri(basePath);
            Uri targetUri = new Uri(targetPath);
            return baseUri.MakeRelativeUri(targetUri).ToString().Replace(@"/", @"\");
        }
    }

    internal class ScrollingTextComponent : MonoBehaviour {
        const int DELAY = 10;
        private string FullText = "";
        private int anchor = 0;
        private int delayCount = DELAY;
        private int segmentLength = 13;
        private ListNode node;

        public void Awake() {
            if (this.GetComponentInParent<ListNode>() is ListNode n) {
                node = n;
                Text t = (node.GetField<ListNode>("content") ?? node.GetField<ListNode>("textMesh")?.GetProperty("text")) as Text;
                t.resizeTextForBestFit = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.fontSize = 13;
                t.alignment = TextAnchor.MiddleCenter;
                if (System.Text.Encoding.Default.GetBytes(node.text).Length > segmentLength) {
                    FullText = node.text + "      " + node.text;
                }
            }
        }

        public void Set(int s, string text = null) {
            segmentLength = s;
            if (null != text) {
                node.text = text;
                if (System.Text.Encoding.Default.GetBytes(text).Length > segmentLength) {
                    FullText = text + "      " + text;
                } else {
                    FullText = "";
                }
            }
        }

        public void Update() {
            if (FullText.Length >= segmentLength && null != node && delayCount-- < 0) {
                if (anchor >= ((FullText.Length - 6) / 2) + 6) anchor = 0;
                node.text = FullText.Substring(anchor, segmentLength);
                anchor++;
                delayCount = DELAY;
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

            //Header
            if (__instance is ItemCategoryList && KK_StudioMenuScrollingText.HeaderDict.TryGetValue((int)__instance.GetField<ItemCategoryList>("group"), out string str)) {
                makeFakeNode(str)?.transform.SetAsFirstSibling();
            }

            foreach (ListNode node in listNodes) {
                (node.gameObject.AddComponent(typeof(ScrollingTextComponent)) as ScrollingTextComponent).Set(13);
            }

            //Footer
            if (__instance is ItemCategoryList && KK_StudioMenuScrollingText.FooterDict.TryGetValue((int)__instance.GetField<ItemCategoryList>("group"), out str)) {
                makeFakeNode(str)?.transform.SetAsLastSibling();
            }

            return;

            ListNode makeFakeNode(string s) {
                if (__instance is ItemCategoryList && null != __instance.GetField<ItemCategoryList>("group")) {
                    GameObject gameObject = UnityEngine.Object.Instantiate(__instance.GetField<ItemCategoryList>("objectPrefab") as GameObject);
                    if (!gameObject.activeSelf) {
                        gameObject.SetActive(true);
                    }
                    gameObject.transform.SetParent(parent, false);
                    ListNode node = gameObject.GetComponent<ListNode>();

                    (node.gameObject.AddComponent(typeof(ScrollingTextComponent)) as ScrollingTextComponent).Set(13, s);
                    node.GetComponentInChildren<Button>().interactable = false;
                    Text t = (node.GetField<ListNode>("content") ?? node.GetField<ListNode>("textMesh")?.GetProperty("text")) as Text;
                    t.color = new Color(1, .64f, 0);
                    return node;
                }
                return null;
            }
        }
    }
}
