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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Extension;
using Harmony;
using MessagePack;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_FBIOpenUp {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_FBIOpenUp : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "FBI Open Up";
        internal const string GUID = "com.jim60105.kk.fbiopenup";
        internal const string PLUGIN_VERSION = "19.07.05.0";

        internal static bool isenabled = false;
        public void Awake() {
            UIUtility.Init();
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
            BepInEx.Config.ReloadConfig();
            isenabled = String.Equals(BepInEx.Config.GetEntry("enabled", "False", PLUGIN_NAME), "True");
            string path = BepInEx.Config.GetEntry("sample_chara", "", PLUGIN_NAME);
            if (float.TryParse(BepInEx.Config.GetEntry("change_rate", "0.77", PLUGIN_NAME), out float rate)) {
                Patches.ChangeRate = rate;
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Change Rate: " + rate);
                ;
            } else {
                Patches.ChangeRate = 0.77f;
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Read Change Rate FAILD. Set to default: 0.77");
                BepInEx.Config.SetEntry("change_rate", "0.77", PLUGIN_NAME);
            }
            if (path.Length == 0) {
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Use default chara");
                //Logger.Log(LogLevel.Debug, "[KK_FBIOU] FBI! Open Up!");
                Assembly ass = Assembly.GetExecutingAssembly();
                using (Stream stream = ass.GetManifestResourceStream("KK_FBIOpenUp.Resources.sample_chara.png")) {
                    Patches.LoadSampleChara(stream);
                }
            } else {
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Load path: " + path);
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    Patches.LoadSampleChara(fileStream);
                }
            }
        }

        internal static void ToggleEnabled() {
            isenabled = !isenabled;
            BepInEx.Config.SetEntry("enabled", isenabled ? "True" : "False", PLUGIN_NAME);
            BepInEx.Config.ReloadConfig();
        }
    }

    class Patches {
        private static ChaFileBody body;
        private static ChaFileFace face;
        private static float[] originalShapeValueFace;
        private static float[] originalShapeValueBody;
        private static float[] sampleShapeValueFace;
        private static float[] sampleShapeValueBody;
        private static float[] result;
        private static float keepRate;
        private static GameObject redBagBtn;
        public static float ChangeRate {
            get => 1f - keepRate;
            set {
                if (keepRate > 1) {
                    keepRate = 1;
                } else if (keepRate < 0) {
                    keepRate = 0;
                } else {
                    keepRate = 1f - value;
                }
            }
        }

        public static void LoadSampleChara(Stream stream) {
            ChaFile chaFile = new ChaFile();
            chaFile.Invoke("LoadFile", new object[] { stream, true, true });
            Logger.Log(LogLevel.Debug, "[KK_FBIOU] Loaded sample chara: " + chaFile.parameter.fullname);
            body = MessagePackSerializer.Deserialize<ChaFileBody>(MessagePackSerializer.Serialize<ChaFileBody>(chaFile.custom.body));
            face = MessagePackSerializer.Deserialize<ChaFileFace>(MessagePackSerializer.Serialize<ChaFileFace>(chaFile.custom.face));
        }

        public static void ChangeChara(ChaFileCustom chaFileCustom, bool changeFace = true, bool changeBody = true) {
            if (null != body && changeBody) {
                originalShapeValueBody = (float[])chaFileCustom.body.shapeValueBody.Clone();
                sampleShapeValueBody = (float[])body.shapeValueBody.Clone();
                if (originalShapeValueBody.Length == sampleShapeValueBody.Length) {
                    result = new float[originalShapeValueBody.Length];
                    for (int i = 0; i < originalShapeValueBody.Length; i++) {
                        result[i] = sampleShapeValueBody[i] + ((originalShapeValueBody[i] - sampleShapeValueBody[i]) * keepRate);
                    }
                    chaFileCustom.body.shapeValueBody = (float[])result.Clone();
                }
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Changed body finish");
            }
            if (null != face && changeFace) {
                originalShapeValueFace = (float[])chaFileCustom.face.shapeValueFace.Clone();
                sampleShapeValueFace = (float[])face.shapeValueFace.Clone();
                if (originalShapeValueFace.Length == sampleShapeValueFace.Length) {
                    result = new float[originalShapeValueFace.Length];
                    for (int i = 0; i < originalShapeValueFace.Length; i++) {
                        result[i] = sampleShapeValueFace[i] + ((originalShapeValueFace[i] - sampleShapeValueFace[i]) * keepRate);
                    }
                    chaFileCustom.face.shapeValueFace = (float[])result.Clone();
                }
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Changed face finish");
            }
        }

        public static void ChangeAllCharacters() {
            List<OCIChar> charList = Studio.Studio.Instance.dicInfo.Values.OfType<Studio.OCIChar>().ToList();
            charList.ForEach(new Action<OCIChar>(delegate (OCIChar ocichar) { ChangeChara(ocichar.charInfo.chaFile.custom); }));
        }

        private static void ChangeRedBagBtn() {
            if (KK_FBIOpenUp.isenabled) {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Enable Plugin");
            } else {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Disnable Plugin");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            if (String.Equals(__instance.name, "00_Female")) {
                var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/00_Female/Button Change");
                redBagBtn = UnityEngine.Object.Instantiate(original, original.transform.parent.transform);
                redBagBtn.name = "redBagBtn";
                redBagBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(-120, -270), new Vector2(-40, -190));
                redBagBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.redBag.png", 100, 100);
                redBagBtn.GetComponent<Button>().onClick.RemoveAllListeners();
                redBagBtn.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                redBagBtn.GetComponent<Button>().interactable = true;
                redBagBtn.GetComponent<Button>().onClick.AddListener(() => {
                    KK_FBIOpenUp.ToggleEnabled();
                    ChangeRedBagBtn();
                });
                ChangeRedBagBtn();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCustom), "LoadBytes")]
        public static void LoadBytesPostfix(ChaFileCustom __instance) {
            if (KK_FBIOpenUp.isenabled) {
                ChangeChara(__instance);
            }
        }
    }
}
