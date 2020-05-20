using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_TransparentBackground {
    public static class TransparentUI {
        private static bool enable = false;
        private static float alpha = 0.8f;

        //https://github.com/IllusionMods/HideAllUI/blob/v2.1/src/HideAllUI.Koikatu/HideStudioUI.cs#L10
        private static readonly string[] gameCanviNames = { "Canvas", "CustomRoot", "InfomationH", "FrontUIGroup" }; //用於String.Contain()的關鍵字
        private static readonly string[] pluginCanviNames = { "KKPECanvas(Clone)", "BepInEx_Manager/MaterialEditorCanvas", "QuickAccessBoxCanvas(Clone)" };
        private static List<GameObject> canvasList;

        private static readonly List<CanvasGroup> canvasGroups = new List<CanvasGroup>();

        public static float Alpha {
            get => alpha;
            set {
                alpha = value;
                if (canvasGroups.Count == 0) BuildCamvasGroup();
                if (canvasGroups.Count != 0) {
                    foreach (CanvasGroup cg in canvasGroups) {
                        if (null == cg) {
                            //Rebuild and retry
                            canvasGroups.Clear();
                            Alpha = alpha;
                            return;
                        }

                        cg.alpha = (enable) ? alpha : 1f;
                    }
                    //KK_TransparentBackground.Logger.LogDebug($"Set UI transparency to {transparency * 100}%");
                }
            }
        }

        public static bool Enable {
            get => enable;
            set {
                enable = value;
                Alpha = alpha;
            }
        }

        public static void BuildCamvasGroup() {
            canvasList = Object.FindObjectsOfType<Canvas>()
                               .Where(x => gameCanviNames.Where(y => x.gameObject.name.Contains(y)).Any())
                               .Select(x=>x.gameObject)
                               .ToList();

            #region 例外處理
            //Studio "CvsColor", "Canvas Pattern"已經是用CanvasGroup alpha控制顯示與否，放棄透明它們
            canvasList.RemoveAll(x => x.name == "Canvas Pattern");

            //Maker中的UI上層不是Canvas
            var g = GameObject.Find("CustomRoot");
            if (null != g) canvasList.Add(g);
            #endregion

            canvasGroups.Clear();

            foreach (GameObject go in canvasList.Where(x => null != x)) {
                canvasGroups.Add(
                    go.gameObject.GetComponentInChildren<CanvasGroup>() ?? go.gameObject.AddComponent<CanvasGroup>()
                );
            }

            foreach (string objectName in pluginCanviNames) {
                Canvas canvas = GameObject.Find(objectName)?.GetComponent<Canvas>();
                if (canvas != null) {
                    canvasGroups.Add(
                        canvas.gameObject.GetComponentInChildren<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>()
                    );
                }
            }
        }
    }
}
