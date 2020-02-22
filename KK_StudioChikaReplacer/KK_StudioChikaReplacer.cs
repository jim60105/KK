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
using BepInEx.Harmony;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KK_StudioChikaReplacer {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioChikaReplacer : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Chika Replacer";
        internal const string GUID = "com.jim60105.kk.studiochikareplacer";
        internal const string PLUGIN_VERSION = "20.02.23.1";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.0";

        public static ConfigEntry<KeyboardShortcut> HotkeyAll { get; set; }
        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        public static ConfigEntry<string> Sample_chara { get; set; }
        public static ConfigEntry<bool> Save_before_change { get; set; }
        internal static new ManualLogSource Logger;
        public void Start() {
            Logger = base.Logger;

            Hotkey = Config.Bind<KeyboardShortcut>("Hotkey", "Change selected characters", new KeyboardShortcut(UnityEngine.KeyCode.Quote, new UnityEngine.KeyCode[] { UnityEngine.KeyCode.LeftControl, UnityEngine.KeyCode.LeftShift, UnityEngine.KeyCode.RightShift }));
            HotkeyAll = Config.Bind<KeyboardShortcut>("Hotkey", "Change all characters", new KeyboardShortcut(UnityEngine.KeyCode.Return, new UnityEngine.KeyCode[] { UnityEngine.KeyCode.LeftControl, UnityEngine.KeyCode.LeftShift, UnityEngine.KeyCode.RightShift }));
            Save_before_change = Config.Bind<bool>("Config", "Save before change characters", true);
            Sample_chara = Config.Bind<string>("Config", "Chara to change", "", "Leave blank to use Chika, or use paths like UserData/chara/female/*.png");

            HarmonyWrapper.PatchAll(typeof(Patches));
        }

        public void Update() => Patches.Update();
    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioChikaReplacer.Logger;
        private static bool blockLoadFlag = false;
        private static ChaControl tmpChaCtrl;
        private static List<float> originalShapeValueBody;

        internal static void Update() {
            //監聽滑鼠按下
            //換選取的
            if (KK_StudioChikaReplacer.Hotkey.Value.IsDown()) {
                Change((from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                        select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                        where v != null
                        select v).ToList(),true);
            }
            //全換
            if (KK_StudioChikaReplacer.HotkeyAll.Value.IsDown()) {
                Change(Studio.Studio.Instance.dicInfo.Values.OfType<OCIChar>().ToList());
            }
        }

        /// <summary>
        /// 主要的交換邏輯
        /// </summary>
        private static void Change(List<OCIChar> ociCharList,bool skipSexCheck = false) {
            Logger.LogDebug($"Get {ociCharList.Count} charaters.");
            //先存檔
            if (KK_StudioChikaReplacer.Save_before_change.Value) {
                Singleton<Studio.Studio>.Instance.SaveScene();
            }

            Dictionary<OCIChar, float> mouthOpen = new Dictionary<OCIChar, float>();
            ociCharList.ForEach(x => {
                mouthOpen.Add(x, x.oiCharInfo.mouthOpen);
            });

            foreach (OCIChar ociChaCtrl in ociCharList) {
                if (null == ociChaCtrl) {
                    continue;
                }
                if (ociChaCtrl.charInfo.chaFile.parameter.sex != 1 && !skipSexCheck) {
                    Logger.LogWarning($"Skip changes that he is not a girl: {ociChaCtrl.charInfo.fileParam.fullname}");
                    continue;
                }

                //Backup body 
                originalShapeValueBody = ociChaCtrl.charInfo.chaFile.custom.body.shapeValueBody.ToList();
                tmpChaCtrl = ociChaCtrl.charInfo;

                blockLoadFlag = true;
                ociChaCtrl.ChangeChara(KK_StudioChikaReplacer.Sample_chara.Value);
                blockLoadFlag = false;
            }

            //處理嘴巴open歸零問題
            foreach (KeyValuePair<Studio.OCIChar, float> kv in mouthOpen) {
                kv.Key.ChangeMouthOpen(kv.Value);
            }
            Logger.LogDebug("Changes are all finished.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), "LoadCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
        public static bool LoadCharaFilePrefix(ref bool __result) {
            //攔截要載入千佳的狀況
            if (blockLoadFlag && KK_StudioChikaReplacer.Sample_chara.Value.IsNullOrEmpty()) {
                tmpChaCtrl.LoadPreset(tmpChaCtrl.sex, tmpChaCtrl.exType);
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), "LoadCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
        public static void LoadCharaFilePostfix() {
            if (blockLoadFlag) {
                //Restore
                ChaFileCustom chaFileCustom = tmpChaCtrl.chaFile.custom;
                if (originalShapeValueBody.Count == chaFileCustom.body.shapeValueBody.Length) {
                    for (int i = 0; i < originalShapeValueBody.Count; i++) {
                        chaFileCustom.body.shapeValueBody[i] = originalShapeValueBody[i];
                    }
                } else { Logger.LogError("Sample data is not match to target data!"); }
                originalShapeValueBody.Clear();
                Logger.LogDebug($"Restore body: {tmpChaCtrl.chaFile.parameter.fullname}");
            }
        }
    }
}
