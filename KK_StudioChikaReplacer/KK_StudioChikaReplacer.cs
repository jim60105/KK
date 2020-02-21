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
        internal const string PLUGIN_VERSION = "20.02.21.0";
        internal const string PLUGIN_RELEASE_VERSION = "0.0.0";

        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        public static ConfigEntry<string> Sample_chara { get; private set; }
        internal static new ManualLogSource Logger;
        public void Start() {
            Logger = base.Logger;
            Hotkey = Config.Bind<KeyboardShortcut>("Hotkey", "Change", new KeyboardShortcut(UnityEngine.KeyCode.Quote, new UnityEngine.KeyCode[] { UnityEngine.KeyCode.LeftControl, UnityEngine.KeyCode.LeftShift, UnityEngine.KeyCode.RightShift }));

            Sample_chara = Config.Bind<string>("Config", "Chara to change", "", "Leave blank to use Chika, or use paths like UserData/chara/female/*.png");

            HarmonyWrapper.PatchAll(typeof(Patches));
        }

        public void Update() => Patches.Update();
    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioChikaReplacer.Logger;
        //internal static ChaControl SampleChara;
        private static ChaControl tmpCtrl;

        internal static void Update() {
            if (KK_StudioChikaReplacer.Hotkey.Value.IsDown()) {
                //Logger.LogDebug("KeyDown");

                List<OCIChar> ociCharList = new List<OCIChar>();
                Dictionary<OCIChar, float> mouthOpen = new Dictionary<OCIChar, float>();
                ociCharList = Studio.Studio.Instance.dicInfo.Values.OfType<OCIChar>().Select(delegate (OCIChar x) {
                    //處理嘴巴open歸零問題
                    mouthOpen.Add(x, x.oiCharInfo.mouthOpen);
                    return x;
                }).ToList();
                Logger.LogDebug($"Get {ociCharList.Count} charaters.");

                foreach (OCIChar ociChaCtrl in ociCharList) {
                    if (null == ociChaCtrl) {
                        continue;
                    }
                    if (ociChaCtrl.charInfo.chaFile.parameter.sex != 1) {
                        Logger.LogWarning($"Skip changes that he is not a girl: {ociChaCtrl.charInfo.fileParam.fullname}");
                        return;
                    }

                    //Backup and change to Chika
                    ChaFileCustom chaFileCustom = ociChaCtrl.charInfo.chaFile.custom;
                    List<float> originalShapeValueBody;
                    originalShapeValueBody = chaFileCustom.body.shapeValueBody.ToList();

                    tmpCtrl = ociChaCtrl.charInfo;
                    blockLoadFlag = true;
                    ociChaCtrl.ChangeChara(KK_StudioChikaReplacer.Sample_chara.Value);
                    blockLoadFlag = false;
                    chaFileCustom = ociChaCtrl.charInfo.chaFile.custom;

                    //Restore Body
                    if (originalShapeValueBody.Count == chaFileCustom.body.shapeValueBody.Length) {
                        for (int i = 0; i < originalShapeValueBody.Count; i++) {
                            chaFileCustom.body.shapeValueBody[i] = originalShapeValueBody[i];
                        }
                    } else { Logger.LogError("Sample data is not match to target data!"); }
                    ociChaCtrl.charInfo.Reload();
                }

                //處理嘴巴open歸零問題
                foreach (KeyValuePair<Studio.OCIChar, float> kv in mouthOpen) {
                    kv.Key.ChangeMouthOpen(kv.Value);
                }
                Logger.LogDebug("Changed all finish");
            }
        }

        private static bool blockLoadFlag = false;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), "LoadCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
        public static bool LoadCharaFilePrefix() {
            if (blockLoadFlag && KK_StudioChikaReplacer.Sample_chara.Value.IsNullOrEmpty()) {
                tmpCtrl.LoadPreset(tmpCtrl.sex, tmpCtrl.exType);

                return false;
            } else {
                return true;
            }
        }
    }
}
