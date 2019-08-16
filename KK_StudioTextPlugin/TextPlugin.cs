using BepInEx.Logging;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioTextPlugin {
    static class TextPlugin {
        private static Material Font3DMaterial;
        internal static bool DisablePreview = true;
        private static readonly Dictionary<string, Font> DynamicFonts = new Dictionary<string, Font>();
        private static string[] FontList;
        internal static void Start() {
            //載入font3DMaterial，為了解決UI文字穿透其他物品的問題
            //因為文字無法編輯Shader，只能做Material來用
            if (AssetBundle.LoadFromMemory(Properties.Resources.text) is AssetBundle assetBundle) {
                Font3DMaterial = assetBundle.LoadAsset<Material>("Font3DMaterial");
                Font3DMaterial.color = Color.white;
                assetBundle.Unload(false);
            } else {
                Logger.Log(LogLevel.Error, "[KK_STP] Load assetBundle faild");
            }
        }

        /// <summary>
        /// 取得動態字體清單
        /// </summary>
        /// <returns>動態字體清單</returns>
        public static List<string> GetDynamicFontNames() {
            if (DisablePreview) {
                return FontList.ToList();
            } else {
                return DynamicFonts.Keys.ToList();
            }
        }

        /// <summary>
        /// 建立動態字體字典，如果總數超過500則用到時再生成，否則一次建立完畢
        /// </summary>
        /// <returns>字典內已生成的字體總數</returns>
        internal static int CreateDynamicFonts() {
            DynamicFonts.Clear();
            List<string> fontlist = Font.GetOSInstalledFontNames().ToList();

            for (int i = 0; i < fontlist.Count; i++) {
                if (i > 0 && fontlist[i].Replace("Bold", "").Replace("Italic", "").TrimEnd().Equals(fontlist[i - 1])) {
                    fontlist.RemoveAt(i);
                    i--;
                    continue;
                }
            }
            FontList = fontlist.ToArray();
            if (fontlist.Count >= 500) {
                DynamicFonts.Add("Arial", Resources.GetBuiltinResource<Font>("Arial.ttf"));
                Logger.Log(LogLevel.Info, $"[KK_STP] Detact {fontlist.Count} fonts in your system.");
                Logger.Log(LogLevel.Info, $"[KK_STP] Based on Unity's limitations, this number is more than that can be generated.");
                Logger.Log(LogLevel.Info, $"[KK_STP] I am sorry to tell you that I have to disable your fonts preview.");
            } else {
                DisablePreview = false;
                if (fontlist.Remove("Arial")) {
                    DynamicFonts.Add("Arial", Resources.GetBuiltinResource<Font>("Arial.ttf"));
                }
                foreach (var fontName in fontlist) {
                    DynamicFonts.Add(fontName, Font.CreateDynamicFontFromOSFont(new string[] { fontName, "Arial" }, 30));
                }
                Logger.Log(LogLevel.Info, $"[KK_STP] Generate {DynamicFonts.Count} System Fonts");
            }
            return DynamicFonts.Count;
        }

        /// <summary>
        /// 取得字體
        /// </summary>
        /// <param name="fontName">字體名稱</param>
        /// <returns></returns>
        public static Font GetFont(string fontName) {
            if (!CheckFontInOS(fontName)) {
                Logger.Log(LogLevel.Message, $"[KK_STP] Cannot find {fontName} in your System.");
                FallbackFont();
            } else if (DynamicFonts.Count >= 499) {
                Logger.Log(LogLevel.Message, $"[KK_STP] Based on Unity's limitations, you can't generate more than 500 different fonts at the same time.");
                Logger.Log(LogLevel.Message, $"[KK_STP] Please restart Studio.");
                FallbackFont();
            }
            if (!DynamicFonts.ContainsKey(fontName)) {
                DynamicFonts.Add(fontName, Font.CreateDynamicFontFromOSFont(new string[] { fontName, "Arial" }, 30));
            }
            return DynamicFonts[fontName];

            void FallbackFont() {
                if (CheckFontInOS("MS Gothic")) {
                    Logger.Log(LogLevel.Message, $"[KK_STP] Fallback to MS Gothic");
                    fontName = "MS Gothic";
                } else {
                    Logger.Log(LogLevel.Message, $"[KK_STP] Use Unity BuiltinResource Arial.");
                    fontName = "Arial";
                }
            }
        }

        /// <summary>
        /// 字型設定項目
        /// </summary>
        public enum Config {
            All = 0,
            Font,
            FontSize,
            FontStyle,
            Color,
            Anchor,
            Align
        }

        /// <summary>
        /// 創建設定資料夾結構，或是變更設定中的特定項目
        /// </summary>
        /// <param name="textTreeNodeObject">要變更的OCIFolder.treeNodeObject</param>
        /// <param name="config">要設定的項目</param>
        public static void MakeAndSetConfigStructure(TreeNodeObject textTreeNodeObject, Config config = Config.All) {
            Patches.isCreatingTextStructure = true;
            OCIFolder textOCIFolder = Studio.Studio.GetCtrlInfo(textTreeNodeObject) as OCIFolder;
            TextMesh t = textOCIFolder.objectItem.GetComponentInChildren<TextMesh>(true);
            MeshRenderer m = textOCIFolder.objectItem.GetComponentInChildren<MeshRenderer>(true);
            TreeNodeCtrl treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;

            TreeNodeObject nConfig = doMain(Patches.TextConfigPrefix, "", textTreeNodeObject);
            if (config == Config.Font || config == Config.All)
                doMain(Patches.TextConfigFontPrefix, t.font.name, nConfig);
            if (config == Config.FontSize || config == Config.All)
                doMain(Patches.TextConfigFontSizePrefix, (t.characterSize * 500).ToString(), nConfig);
            if (config == Config.FontStyle || config == Config.All)
                doMain(Patches.TextConfigFontStylePrefix, t.fontStyle.ToString(), nConfig);
            if (config == Config.Color || config == Config.All)
                doMain(Patches.TextConfigColorPrefix, '#' + ColorUtility.ToHtmlStringRGBA(m.material.color), nConfig);
            if (config == Config.Anchor|| config == Config.All)
                doMain(Patches.TextConfigAnchorPrefix, t.anchor.ToString(), nConfig);
            if (config == Config.Align|| config == Config.All)
                doMain(Patches.TextConfigAlignPrefix, t.alignment.ToString(), nConfig);

            Patches.isCreatingTextStructure = false;

            TreeNodeObject doMain(string prefix, string value, TreeNodeObject nRoot) {
                TreeNodeObject node = nRoot.child?.Where((x) =>
                    Studio.Studio.GetCtrlInfo(x).objectInfo.kind == 3 &&
                    (Studio.Studio.GetCtrlInfo(x) is OCIFolder y) &&
                    y.name.Contains(prefix)
                ).FirstOrDefault();
                OCIFolder folder;
                if (null == node) {
                    //沒有找到就創建
                    folder = AddObjectFolder.Add();
                    treeNodeCtrl.SetParent(folder.treeNodeObject, nRoot);
                    folder.objectInfo.changeAmount.Reset();
                    node = folder.treeNodeObject;
                } else {
                    folder = Studio.Studio.GetCtrlInfo(node) as OCIFolder;
                }
                folder.name = folder.objectItem.name = prefix + value;
                return node;
            }
        }

        /// <summary>
        /// 讀取資料夾結構內的設定值
        /// </summary>
        /// <param name="textTreeNodeObject">要讀取的OCIFolder.treeNodeObject</param>
        /// <param name="config">要讀取的項目</param>
        /// <returns></returns>
        public static string GetConfig(TreeNodeObject textTreeNodeObject, Config config) {
            if (null == textTreeNodeObject) {
                textTreeNodeObject = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                      select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                      where v != null
                                      select v.treeNodeObject).FirstOrDefault();
            }
            OCIFolder textOCIFolder = Studio.Studio.GetCtrlInfo(textTreeNodeObject) as OCIFolder;
            TextMesh t = textOCIFolder.objectItem.GetComponentInChildren<TextMesh>(true);
            MeshRenderer m = textOCIFolder.objectItem.GetComponentInChildren<MeshRenderer>(true);
            TreeNodeObject GetChildNode(string prefix, TreeNodeObject nRoot) {
                return nRoot?.child?.Where((x) =>
                    Studio.Studio.GetCtrlInfo(x).objectInfo.kind == 3 &&
                    (Studio.Studio.GetCtrlInfo(x) is OCIFolder y) &&
                    y.name.Contains(prefix)
                ).FirstOrDefault();
            }

            TreeNodeObject nConfig = GetChildNode(Patches.TextConfigPrefix, textTreeNodeObject);
            string GetValue(string prefix) {
                if (Studio.Studio.GetCtrlInfo(GetChildNode(prefix, nConfig)) is OCIFolder f) {
                    return f.name.Replace(prefix, "");
                } else {
                    return null;
                }
            }

            switch (config) {
                case Config.Font:
                    return GetValue(Patches.TextConfigFontPrefix);
                case Config.FontSize:
                    return GetValue(Patches.TextConfigFontSizePrefix);
                case Config.FontStyle:
                    return GetValue(Patches.TextConfigFontStylePrefix);
                case Config.Color:
                    return GetValue(Patches.TextConfigColorPrefix);
                case Config.Anchor:
                    return GetValue(Patches.TextConfigAnchorPrefix);
                case Config.Align:
                    return GetValue(Patches.TextConfigAlignPrefix);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 在給定的OCIFolder GameObject下添加TextMesh
        /// </summary>
        /// <param name="folder">要添加TextMesh的OCIFolder</param>
        /// <param name="text">預設文字</param>
        /// <returns>新建立的TextMesh</returns>
        public static TextMesh MakeTextObj(OCIFolder folder, string text) {
            Patches.DrawConfigPanel();
            folder.objectItem.layer = 10;
            GameObject go = new GameObject();
            go.transform.SetParent(folder.objectItem.transform);
            go.layer = 10;
            go.transform.localPosition = Vector3.zero;
            TextMesh t = go.AddComponent<TextMesh>();
            t.fontSize = 500;
            t.text = text;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            SetFont(folder);
            SetCharacterSize(folder);
            SetColor(folder,Color.white);
            SetFontStyle(folder);
            SetAlignment(folder);
            SetAnchor(folder);

            Logger.Log(LogLevel.Info, "[KK_STP] Create Text");
            return t;
        }

        /// <summary>
        /// 更改選取項目的字型
        /// </summary>
        /// <param name="fontName">字型名稱</param>
        public static void ChangeFont(string fontName) {
            OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                       select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                       where v != null
                                       select v).ToArray();
            foreach (var oCIFolder in folderArray) {
                SetFont(oCIFolder, fontName);
                MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.Font);
            }
        }

        /// <summary>
        /// 設定字型，預設為MS Gothic
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="fontName">字型名稱</param>
        public static void SetFont(OCIFolder folder, string fontName = "MS Gothic") {
            Color colorBackup = folder.objectItem.GetComponentInChildren<MeshRenderer>(true).material.color;
            TextMesh textMesh = folder.objectItem.GetComponentInChildren<TextMesh>(true);
            textMesh.font = GetFont(fontName);
            textMesh.font.RequestCharactersInTexture(textMesh.text);
            folder.objectItem.GetComponentInChildren<MeshRenderer>(true).material = Font3DMaterial;
            folder.objectItem.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_MainTex", textMesh.font.material.mainTexture);
            folder.objectItem.GetComponentInChildren<MeshRenderer>(true).material.EnableKeyword("_NORMALMAP");
            SetColor(folder, colorBackup);
        }

        /// <summary>
        /// 檢查OS中是否有安裝給定字型
        /// </summary>
        /// <param name="fontName">字型名稱</param>
        /// <returns>系統是否能使用給訂字型</returns>
        public static bool CheckFontInOS(string fontName) {
            var i = Array.IndexOf(Font.GetOSInstalledFontNames(), fontName);
            if (i >= 0) {
                return true;
            }
            Logger.Log(LogLevel.Message, "[KK_STP] Missing font: " + fontName);
            return false;
        }

        /// <summary>
        /// 更改選取項目的字體大小
        /// </summary>
        /// <param name="size">字體大小</param>
        public static void ChangeCharacterSize(float size) {
            OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                       select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                       where v != null
                                       select v).ToArray();
            foreach (var oCIFolder in folderArray) {
                SetCharacterSize(oCIFolder, size);
                MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.FontSize);
            }
        }

        /// <summary>
        /// 設定字體大小，單位放大五百倍
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="size">字體大小</param>
        public static void SetCharacterSize(OCIFolder folder, string size) {
            if(!float.TryParse(size, out float f)) {
                f = 1f;
            }
            SetCharacterSize(folder, f);
        }
        /// <summary>
        /// 設定字體大小，單位放大五百倍
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="size">字體大小</param>
        public static void SetCharacterSize(OCIFolder folder, float size = 1f) {
            folder.objectItem.GetComponentInChildren<TextMesh>(true).characterSize = 0.002f * size;
        }

        /// <summary>
        /// 更改選取項目的字型顏色
        /// </summary>
        /// <param name="color">字型顏色</param>
        public static void ChangeColor(Color color) {
            OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                       select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                       where v != null
                                       select v).ToArray();
            foreach (var oCIFolder in folderArray) {
                SetColor(oCIFolder, color);
                MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.Color);
                Patches.panel.transform.Find("colorBtn").GetComponent<Button>().image.color = color;
            }
        }

        /// <summary>
        /// 設定字型顏色
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="color">字型顏色</param>
        public static void SetColor(OCIFolder folder,string color) {
            if (!ColorUtility.TryParseHtmlString(color, out var c)) {
                c = default;
            }
            SetColor(folder, c);
        }
        /// <summary>
        /// 設定字型顏色
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="color">字型顏色</param>
        public static void SetColor(OCIFolder folder, Color color = default) {
            folder.objectItem.GetComponentInChildren<MeshRenderer>(true).material.color = color;
        }

        /// <summary>
        /// 更改選取項目的字體樣式
        /// </summary>
        /// <param name="style">字體樣式</param>
        public static void ChangeFontStyle(string style) {
            OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                       select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                       where v != null
                                       select v).ToArray();
            foreach (var oCIFolder in folderArray) {
                SetFontStyle(oCIFolder, style);
                MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.FontStyle);
            }
        }

        /// <summary>
        /// 設定字體樣式
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="style">字體樣式</param>
        public static void SetFontStyle(OCIFolder folder, string style = "Normal") {
            try {
                if (style == "") {
                    style = "Normal";
                }
                folder.objectItem.GetComponentInChildren<TextMesh>(true).fontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), style);
            } catch (OverflowException) {
                Logger.Log(LogLevel.Error, "[KK_STP] OverflowException: Please use a correct FontStyle.");
                Logger.Log(LogLevel.Error, "[KK_STP] Fallback to FontStyle.Normal");
                folder.objectItem.GetComponentInChildren<TextMesh>(true).fontStyle = FontStyle.Normal;
            }
        }

        /// <summary>
        /// 更改選取項目的字體錨點
        /// </summary>
        /// <param name="anchor">字體錨點</param>
        public static void ChangeAnchor(string anchor) {
            OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                       select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                       where v != null
                                       select v).ToArray();
            foreach (var oCIFolder in folderArray) {
                SetAnchor(oCIFolder, anchor);
                MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.Anchor);
            }
        }

        /// <summary>
        /// 設定字體錨點
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="anchor">字體錨點</param>
        public static void SetAnchor(OCIFolder folder, string anchor = "MiddleCenter") {
            try {
                if (anchor == "") {
                    anchor = "MiddleCenter";
                }
                folder.objectItem.GetComponentInChildren<TextMesh>(true).anchor = (TextAnchor)Enum.Parse(typeof(TextAnchor), anchor);
            } catch (OverflowException) {
                Logger.Log(LogLevel.Error, "[KK_STP] OverflowException: Please use a correct Anchor(Upper/Lower/Middle + Left/Right/Center).");
                Logger.Log(LogLevel.Error, "[KK_STP] Fallback to TextAnchor.MiddleCenter");
                folder.objectItem.GetComponentInChildren<TextMesh>(true).anchor = TextAnchor.MiddleCenter;
            }
        }

        /// <summary>
        /// 更改選取項目的字體對齊
        /// </summary>
        /// <param name="alignment">字體對齊(Left, Right, Center)</param>
        public static void ChangeAlignment(string alignment) {
            OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                       select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                       where v != null
                                       select v).ToArray();
            foreach (var oCIFolder in folderArray) {
                SetAlignment(oCIFolder, alignment);
                MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.Align);
            }
        }

        /// <summary>
        /// 設定字體對齊
        /// </summary>
        /// <param name="folder">對象OCIFolder</param>
        /// <param name="alignment">字體對齊(Left, Right, Center)</param>
        public static void SetAlignment(OCIFolder folder, string alignment = "Center") {
            try {
                if (alignment == "") {
                    alignment = "Center";
                }
                folder.objectItem.GetComponentInChildren<TextMesh>(true).alignment = (TextAlignment)Enum.Parse(typeof(TextAlignment), alignment);
            } catch (OverflowException) {
                Logger.Log(LogLevel.Error, "[KK_STP] OverflowException: Please use a correct Alignment (Left, Right, Center).");
                Logger.Log(LogLevel.Error, "[KK_STP] Fallback to TextAlignment.Center");
                folder.objectItem.GetComponentInChildren<TextMesh>(true).alignment = TextAlignment.Center;
            }
        }
    }
}
