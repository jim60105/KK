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
using ExtensibleSaveFormat;
using Extension;
using HarmonyLib;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_FBIOpenUp {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("KKABMX.Core", BepInDependency.DependencyFlags.SoftDependency)]
    public class KK_FBIOpenUp : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "FBI Open Up";
        internal const string GUID = "com.jim60105.kk.fbiopenup";
        internal const string PLUGIN_VERSION = "19.11.02.3";
		internal const string PLUGIN_RELEASE_VERSION = "0.0.0";

        internal static bool _isenabled = false;
        internal static bool _isABMXExist = false;
        internal static GameMode nowGameMode;
        internal enum GameMode {
            Studio = 0,
            LOGO,
            MyRoom,
            MainGame,
            Maker,
            FreeHMenu,
            FreeH
        }
        internal static string videoPath;
        internal static float videoVolume = 0.1f;

        public static ConfigEntry<bool> Enable { get; private set; }
        public static ConfigEntry<string> Sample_chara { get; private set; }
        public static ConfigEntry<float> Change_rate { get; private set; }
        public static ConfigEntry<string> Video_related_path { get; private set; }
        public static ConfigEntry<float> Video_volume { get; private set; }

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            UIUtility.Init();
            HarmonyWrapper.PatchAll(typeof(Patches));
        }

        public void Update() {
            Patches.Update();
        }

        public void Start() {
            BaseUnityPlugin TryGetPluginInstance(string pluginName, Version minimumVersion = null) {
                BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(pluginName, out var target);
                if (null != target) {
                    if (target.Metadata.Version >= minimumVersion) {
                        return target.Instance;
                    }
                    Logger.LogMessage($" {pluginName} v{target.Metadata.Version.ToString()} is detacted OUTDATED.");
                    Logger.LogMessage($"Please update {pluginName} to at least v{minimumVersion.ToString()} to enable related feature.");
                }
                return null;
            }

            _isABMXExist = null != TryGetPluginInstance("KKABMX.Core", new Version(3, 3));

            //讀取config
            Enable = Config.AddSetting("Config", "Enable", false);
            _isenabled = Enable.Value;
 
            Sample_chara = Config.AddSetting("Config", "Sample chara", "");
            string sampleCharaPath = Sample_chara.Value;
            if (sampleCharaPath.Length == 0) {
                KK_FBIOpenUp.Logger.LogDebug("Use default chara");
                //KK_FBIOpenUp.Logger.LogDebug("FBI! Open Up!");
                Assembly ass = Assembly.GetExecutingAssembly();
                using (Stream stream = ass.GetManifestResourceStream("KK_FBIOpenUp.Resources.sample_chara.png")) {
                    Patches.LoadSampleChara(stream);
                }
            } else {
                KK_FBIOpenUp.Logger.LogDebug("Load path: " + sampleCharaPath);
                using (FileStream fileStream = new FileStream(sampleCharaPath, FileMode.Open, FileAccess.Read)) {
                    Patches.LoadSampleChara(fileStream);
                }
            }

            Change_rate = Config.AddSetting("Config", "Change rate", 0.77f);
            float rate = Change_rate.Value;
            Patches.ChangeRate = rate;
            KK_FBIOpenUp.Logger.LogDebug("Change Rate: " + rate);

            Video_related_path = Config.AddSetting("Config", "Video related path", "UserData/audio/FBI.mp4");
            var tempVideoPath = Video_related_path.Value;
            if (!File.Exists(tempVideoPath)) {
                KK_FBIOpenUp.Logger.LogError("Video Not Found: " + tempVideoPath);
            } else {
                videoPath = $"file://{Application.dataPath}/../{tempVideoPath}";
            }

            Video_volume = Config.AddSetting("Config", "Video volume", 0.06f);
            videoVolume = Video_volume.Value;
            KK_FBIOpenUp.Logger.LogDebug($"Set video volume to {videoVolume}");

            if (Application.productName == "CharaStudio") {
                nowGameMode = GameMode.Studio;
            } else {
                if (Manager.Game.Instance != null) {
                    nowGameMode = GameMode.MainGame;
                }
            }
        }

        internal static void ToggleEnabled() {
            _isenabled = !_isenabled;

            Enable.Value = _isenabled;
        }
    }
    internal class Patches {
        private static float keepRate;

        /// <summary>
        /// 要向sample改變的程度，範圍0(無替換)~1(全替換)
        /// </summary>
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

        private static class SampleChara {
            internal static List<float> shapeValueFace;
            internal static List<float> shapeValueBody;
            internal static PluginData ABMXData;
            internal static ChaFileControl chaFile;
        }

        /// <summary>
        /// 載入sample chara
        /// </summary>
        /// <param name="stream">角色圖片讀取為Stream</param>
        public static void LoadSampleChara(Stream stream) {
            blockChanging = true;
            SampleChara.chaFile = new ChaFileControl();
            SampleChara.chaFile.Invoke("LoadCharaFile", new object[] { stream, true, true });
            KK_FBIOpenUp.Logger.LogDebug("Loaded sample chara: " + SampleChara.chaFile.parameter.fullname);
            blockChanging = false;
            var face = MessagePackSerializer.Deserialize<ChaFileFace>(MessagePackSerializer.Serialize<ChaFileFace>(SampleChara.chaFile.custom.face));
            var body = MessagePackSerializer.Deserialize<ChaFileBody>(MessagePackSerializer.Serialize<ChaFileBody>(SampleChara.chaFile.custom.body));
            //KK_FBIOpenUp.Logger.LogMessage("Length Face: " + face.shapeValueFace.Length);
            //KK_FBIOpenUp.Logger.LogMessage("Length Body: " + body.shapeValueBody.Length);
            SampleChara.shapeValueFace = face.shapeValueFace.ToList();
            SampleChara.shapeValueBody = body.shapeValueBody.ToList();

            SampleChara.ABMXData = ExtendedSave.GetExtendedDataById(SampleChara.chaFile, "KKABMPlugin.ABMData");
            if (null != SampleChara.ABMXData) {
                KK_FBIOpenUp.Logger.LogDebug("Loaded sample chara ABMX");
            } else {
                KK_FBIOpenUp.Logger.LogDebug("NO sample chara ABMX");
            }
        }

        /// <summary>
        /// 替換過的chara之原始數據 Dict(ChaFileCustom, List[]{shapeValueFace.toList, shapeValueBody.toList})
        /// </summary>
        private static readonly Dictionary<ChaFileCustom, List<float>[]> chaFileCustomDict = new Dictionary<ChaFileCustom, List<float>[]>();

        /// <summary>
        /// 替換角色
        /// </summary>
        /// <param name="chaCtrl">目標chara</param>
        /// <param name="changeFace">是否替換臉部</param>
        /// <param name="changeBody">是否替換身體</param>
        public static void ChangeChara(ChaControl chaCtrl, bool changeFace = true, bool changeBody = true, bool disableDoubleChange = true) {
            if (blockChanging || null == chaCtrl || null == chaCtrl.chaFile) {
                return;
            }

            if (chaCtrl.chaFile.parameter.sex != SampleChara.chaFile.parameter.sex) {
                KK_FBIOpenUp.Logger.LogInfo("Skip changing because of wrong sex.");
                return;
            }

            //int j = 0;
            //KK_FBIOpenUp.Logger.LogInfo("" + ++j);
            List<float> originalShapeValueFace;
            List<float> originalShapeValueBody;
            ChaFileCustom chaFileCustom = chaCtrl.chaFile.custom;
            //KK_FBIOpenUp.Logger.LogInfo("" + ++j);

            //KK_FBIOpenUp.Logger.LogMessage("Length Face: " + chaFileCustom.face.shapeValueFace.Length);
            //KK_FBIOpenUp.Logger.LogMessage("Length Body: " + chaFileCustom.body.shapeValueBody.Length);
            originalShapeValueFace = chaFileCustom.face.shapeValueFace.ToList();
            originalShapeValueBody = chaFileCustom.body.shapeValueBody.ToList();
            List<float> result;
            //KK_FBIOpenUp.Logger.LogInfo("" + ++j);

            //如果角色第一次替換，紀錄其原始數據至dict
            //如果在dict內有找到替換紀錄，以其原始數據來做替換
            //(不block掉是因為，即使在單次Loading Chara內，此function也會被trigger不止一次)
            if (chaFileCustomDict.TryGetValue(chaFileCustom, out var chaFileCustomStored)) {
                if (disableDoubleChange) {
                    chaFileCustomDict[chaFileCustom] = new List<float>[] { new List<float>(originalShapeValueFace), new List<float>(originalShapeValueBody) };
                } else {
                    originalShapeValueFace = chaFileCustomStored[0].ToList();
                    originalShapeValueBody = chaFileCustomStored[1].ToList();
                }
            } else {
                chaFileCustomDict.Add(chaFileCustom, new List<float>[] { new List<float>(originalShapeValueFace), new List<float>(originalShapeValueBody) });
                KK_FBIOpenUp.Logger.LogDebug("chaFileCustomDict.Count: " + chaFileCustomDict.Count);
            }
            //KK_FBIOpenUp.Logger.LogInfo("" + ++j);

            if (null != SampleChara.shapeValueFace && changeFace) {
                if (originalShapeValueFace.Count == SampleChara.shapeValueFace.Count) {
                    result = new List<float>();
                    for (int i = 0; i < originalShapeValueFace.Count; i++) {
                        result.Add(SampleChara.shapeValueFace[i] + ((originalShapeValueFace[i] - SampleChara.shapeValueFace[i]) * keepRate));
                    }
                    chaFileCustom.face.shapeValueFace = result.ToArray();
                } else { KK_FBIOpenUp.Logger.LogError("Sample data is not match to target data!"); }
                KK_FBIOpenUp.Logger.LogDebug("Changed face finish");
            }
            //KK_FBIOpenUp.Logger.LogInfo("" + ++j);

            if (null != SampleChara.shapeValueBody && changeBody) {
                if (originalShapeValueBody.Count == SampleChara.shapeValueBody.Count) {
                    result = new List<float>();
                    for (int i = 0; i < originalShapeValueBody.Count; i++) {
                        result.Add(SampleChara.shapeValueBody[i] + ((originalShapeValueBody[i] - SampleChara.shapeValueBody[i]) * keepRate));
                    }
                    chaFileCustom.body.shapeValueBody = result.ToArray();
                    chaCtrl.Reload(true, false, true, false);
                } else { KK_FBIOpenUp.Logger.LogError("Sample data is not match to target data!"); }
                KK_FBIOpenUp.Logger.LogDebug("Changed body finish");
            }

            //KK_FBIOpenUp.Logger.LogInfo("" + ++j);
            //if (KK_FBIOpenUp._isABMXExist) {
            //    //取得BoneController
            //    object BoneController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
            //    if (null == BoneController) {
            //        KK_FBIOpenUp.Logger.LogDebug("No ABMX BoneController found");
            //        return;
            //    }

            //    //建立重用function
            //    void GetModifiers(Action<object> action) {
            //        foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames")) {
            //            var modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
            //            if (null != modifier) {
            //                action(modifier);
            //            }
            //        }
            //    }

            //    //取得舊角色衣服ABMX數據
            //    List<object> previousModifier = new List<object>();
            //    GetModifiers(x => {
            //        if ((bool)x.Invoke("IsCoordinateSpecific")) {
            //            previousModifier.Add(x);
            //        }
            //    });

            //    //將擴充資料由暫存複製到角色身上
            //    ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, "KKABMPlugin.ABMData", SampleChara.ABMXData);

            //    //把擴充資料載入ABMX插件
            //    BoneController.Invoke("OnReload", new object[] { 2, false });

            //    //清理新角色數據，將衣服數據刪除
            //    List<object> newModifiers = new List<object>();
            //    int i = 0;
            //    GetModifiers(x => {
            //        if ((bool)x.Invoke("IsCoordinateSpecific")) {
            //            KK_FBIOpenUp.Logger.LogDebug("Clean new coordinate ABMX BoneData: " + (string)x.GetProperty("BoneName"));
            //            x.Invoke("MakeNonCoordinateSpecific");
            //            var y = x.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)0 });
            //            y.Invoke("Clear");
            //            x.Invoke("MakeCoordinateSpecific");    //保險起見以免後面沒有成功清除
            //            i++;
            //        } else {
            //            newModifiers.Add(x);
            //        }
            //    });

            //    //將舊的衣服數據合併回到角色身上
            //    i = 0;
            //    foreach (var modifier in previousModifier) {
            //        string bonename = (string)modifier.GetProperty("BoneName");
            //        if (!newModifiers.Any(x => string.Equals(bonename, (string)x.GetProperty("BoneName")))) {
            //            BoneController.Invoke("AddModifier", new object[] { modifier });
            //            KK_FBIOpenUp.Logger.LogDebug("Rollback cooridnate ABMX BoneData: " + bonename);
            //        } else {
            //            KK_FBIOpenUp.Logger.LogError("Duplicate coordinate ABMX BoneData: " + bonename);
            //        }
            //        i++;
            //    }
            //    KK_FBIOpenUp.Logger.LogDebug($"Merge {i} previous ABMX Bone Modifiers");

            //    //重整
            //    BoneController.SetProperty("NeedsFullRefresh", true);
            //    BoneController.SetProperty("NeedsBaselineUpdate", true);
            //    BoneController.Invoke("LateUpdate");

            //    //把ABMX的數據存進擴充資料
            //    BoneController.Invoke("OnCardBeingSaved", new object[] { 1 });
            //    BoneController.Invoke("OnReload", new object[] { 2, false });

            //    //列出角色身上所有ABMX數據
            //    KK_FBIOpenUp.Logger.LogDebug("--List all exist ABMX BoneData--");
            //    foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames", null)) {
            //        var modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
            //        if (null != modifier) {
            //            KK_FBIOpenUp.Logger.LogDebug("" + boneName);
            //        }
            //    }
            //}
            KK_FBIOpenUp.Logger.LogDebug($"Changed.");
        }

        /// <summary>
        /// 將所有角色做替換
        /// </summary>
        public static void ChangeAllCharacters(bool rollback = false) {
            List<ChaControl> charList = new List<ChaControl>();
            KK_FBIOpenUp.Logger.LogDebug($"GameMode: {Enum.GetNames(typeof(KK_FBIOpenUp.GameMode))[(int)KK_FBIOpenUp.nowGameMode]}");
            switch (KK_FBIOpenUp.nowGameMode) {
                case KK_FBIOpenUp.GameMode.Studio:
                    charList = Studio.Studio.Instance.dicInfo.Values.OfType<Studio.OCIChar>().Select(x => x.charInfo).ToList();
                    break;
                case KK_FBIOpenUp.GameMode.MainGame:
                    charList = Singleton<Manager.Game>.Instance.HeroineList.Select(x => x.chaCtrl).ToList();
                    break;
            }
            if (null != charList) {
                KK_FBIOpenUp.Logger.LogDebug($"Get {charList.Count} charaters.");
                foreach (var chaCtrl in charList) {
                    if (rollback) {
                        RollbackChara(chaCtrl);
                    } else {
                        ChangeChara(chaCtrl, true, true, false);
                    }
                }
            } else { KK_FBIOpenUp.Logger.LogError("Get CharaList FAILED! This should not happen!"); }
        }

        public static void RollbackChara(ChaControl chaCtrl) {
            if (blockChanging || null == chaCtrl || null == chaCtrl.chaFile) {
                return;
            }

            if (chaCtrl.chaFile.parameter.sex != SampleChara.chaFile.parameter.sex) {
                KK_FBIOpenUp.Logger.LogInfo("Skip changing because of wrong sex.");
                return;
            }

            if (chaCtrl.chaFile.custom is ChaFileCustom chaFileCustom) {
                if (chaFileCustomDict.TryGetValue(chaFileCustom, out var chaFileCustomStored)) {
                    bool[] done = { false, false };
                    if (chaFileCustomStored[0].Count == chaFileCustom.face.shapeValueFace.Length) {
                        chaFileCustom.face.shapeValueFace = chaFileCustomStored[0].ToArray();
                        done[0] = true;
                    } else { KK_FBIOpenUp.Logger.LogError("Backup face data is not match to target data!"); }

                    if (chaFileCustomStored[1].Count == chaFileCustom.body.shapeValueBody.Length) {
                        chaFileCustom.body.shapeValueBody = chaFileCustomStored[1].ToArray();
                        done[1] = true;
                    } else { KK_FBIOpenUp.Logger.LogError("Backup body data is not match to target data!"); }

                    if (done[0] & done[1]) {
                        chaFileCustomDict.Remove(chaFileCustom);
                    }
                    chaCtrl.Reload(true, false, true, false);
                    KK_FBIOpenUp.Logger.LogDebug($"Rollbacked.");
                }
            }
        }

        #region Hooks
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomScene), "Start")]
        public static void CustomScene_Start() {
            if (Singleton<ChaCustom.CustomBase>.Instance != null) {
                KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.Maker;
            }
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(CustomScene), "OnDestroy")]
        //public static void CustomScene_Destroy() {
        //    KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.LOGO;
        //}

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            bool flag = string.Equals(__instance.name, "00_Female");
            if (SampleChara.chaFile.parameter.sex != 1) { flag = !flag; }
            if (flag) {
                KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.Studio;
                DrawRedBagBtn(__instance, KK_FBIOpenUp.GameMode.Studio);
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(ActionGame.NightMainMenu), "Start")]
        //public static void StartPostfix(ActionGame.NightMainMenu __instance) {
        //    KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.MyRoom;
        //    DrawRedBagBtn(__instance, KK_FBIOpenUp.GameMode.MyRoom);
        //}

        //[HarmonyPostfix, HarmonyPatch(typeof(TitleScene), "Start")]
        //public static void StartPostfix2(TitleScene __instance) {
        //    KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.LOGO;
        //    DrawRedBagBtn(__instance, KK_FBIOpenUp.GameMode.LOGO);
        //}

        [HarmonyPostfix, HarmonyPatch(typeof(ActionScene), "Start")]
        public static void StartPostfix3(ActionScene __instance) {
            KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.MainGame;
            KK_FBIOpenUp._isenabled = false;
            DrawRedBagBtn(__instance, KK_FBIOpenUp.GameMode.MainGame);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFemale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void AddPostfix(ChaControl _female, OICharInfo _info) {
            if (KK_FBIOpenUp._isenabled) {
                ChangeChara(_female);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), "ChangeChara")]
        public static void ChangeCharaPostfix(OCIChar __instance) {
            if (KK_FBIOpenUp._isenabled) {
                ChangeChara(__instance.charInfo);
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(SaveData), nameof(SaveData.Load), new[] { typeof(string), typeof(string) })]
        //public static void LoadPostfix() {
        //    KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.MainGame;
        //    if (KK_FBIOpenUp._isenabled) {
        //        ChangeAllCharacters();
        //    }
        //}

        private static bool blockChanging = false;
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPrefix() {
            SetInitFlag(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPostfix() {
            SetInitFlag(false);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "Awake")]
        public static void AwakePrefix() {
            SetInitFlag(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(SceneLoadScene), "OnClickClose")]
        public static void OnClickClosePostfix() {
            SetInitFlag(false);
        }

        public static void SetInitFlag(bool flag) {
            blockChanging = flag;
            if (flag) {
                chaFileCustomDict.Clear();
            }
            KK_FBIOpenUp.Logger.LogDebug($"Set Init: {flag}");
        }

        #endregion Hooks

        #region Unity Stuff
        /// <summary>
        /// 切換紅色書包圖標顯示
        /// </summary>
        /// <param name="showPic">是否顯示過場圖片</param>
        private static void ChangeRedBagBtn(GameObject redBagBtn, KK_FBIOpenUp.GameMode gameMode) {
            if (KK_FBIOpenUp._isenabled) {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                KK_FBIOpenUp.Logger.LogInfo("Enable Plugin");
            } else {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
                KK_FBIOpenUp.Logger.LogInfo("Disable Plugin");
            }
        }

        private static float btnClickTimer = 0;
        private static bool downState = false;
        private static void DrawRedBagBtn(object __instance, KK_FBIOpenUp.GameMode gameMode) {
            GameObject original, parent;
            Vector2 offsetMin, offsetMax;
            KK_FBIOpenUp.nowGameMode = gameMode;
            switch (gameMode) {
                case KK_FBIOpenUp.GameMode.Studio:
                    CharaList charaList = __instance as CharaList;
                    original = GameObject.Find($"StudioScene/Canvas Main Menu/01_Add/{charaList.name}/Button Change");
                    parent = original.transform.parent.gameObject;
                    offsetMin = new Vector2(-120, -270);
                    offsetMax = new Vector2(-40, -190);
                    break;
                //case KK_FBIOpenUp.GameMode.LOGO:
                //    original = GameObject.Find("TitleScene/Canvas/Panel/Buttons/FirstButtons/Button Start");
                //    parent = original.transform.parent.parent.parent.gameObject;
                //    offsetMin = new Vector2(0, -80);
                //    offsetMax = new Vector2(80, 0);
                //    break;
                //case KK_FBIOpenUp.GameMode.MyRoom:
                //    original = GameObject.Find("ActionScene/UI/ActionMenuCanvas/ModeAnimation/Status");
                //    parent = GameObject.Find("NightMenuScene/Canvas/Panel/base");
                //    //parent = original.transform.parent.gameObject;
                //    offsetMin = new Vector2(0, -80);
                //    offsetMax = new Vector2(80, 0);
                //    break;
                case KK_FBIOpenUp.GameMode.MainGame:
                    original = GameObject.Find("ActionScene/UI/ActionMenuCanvas/ModeAnimation/Status");
                    parent = original.transform.parent.gameObject;
                    offsetMin = new Vector2(0, -80);
                    offsetMax = new Vector2(80, 0);
                    break;
                default:
                    return;
            }
            GameObject redBagBtn = UnityEngine.Object.Instantiate(original, parent.transform);
            redBagBtn.name = "redBagBtn";
            redBagBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), offsetMin, offsetMax);

            redBagBtn.GetComponent<Button>().spriteState = new SpriteState();
            redBagBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.redBag.png", 100, 100);
            redBagBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            for (int i = 0; i < redBagBtn.GetComponent<Button>().onClick.GetPersistentEventCount(); i++) {
                redBagBtn.GetComponent<Button>().onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }
            redBagBtn.GetComponent<Button>().interactable = true;

            //因為要handle長按，改為監聽PointerDown、PointerUp Event
            //在Update()裡面有對Timer累加
            redBagBtn.AddComponent<EventTrigger>();
            EventTrigger trigger = redBagBtn.gameObject.GetComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerDown,
                callback = new EventTrigger.TriggerEvent()
            };
            pointerDown.callback.AddListener((baseEventData) => {
                btnClickTimer = 0;
                downState = true;
                //baseEventData.selectedObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.65f);
            });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerUp,
                callback = new EventTrigger.TriggerEvent()
            };
            pointerUp.callback.AddListener((baseEventData) => {
                downState = false;
                var clickDeltaTime = btnClickTimer;
                btnClickTimer = 0;
                //baseEventData.selectedObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                switch (KK_FBIOpenUp.nowGameMode) {
                    case KK_FBIOpenUp.GameMode.MainGame:
                        KK_FBIOpenUp.ToggleEnabled();
                        if (KK_FBIOpenUp._isenabled) {
                            DrawSlidePic(10, gameMode);
                        } else {
                            DrawSlidePic(20, gameMode);
                        }
                        ChangeRedBagBtn(redBagBtn, gameMode);
                        break;
                    //case KK_FBIOpenUp.GameMode.LOGO:
                    //case KK_FBIOpenUp.GameMode.MyRoom:
                    //    KK_FBIOpenUp.ToggleEnabled();
                    //    ChangeRedBagBtn(redBagBtn, true, gameMode);
                    //    break;
                    case KK_FBIOpenUp.GameMode.Studio:
                        KK_FBIOpenUp.ToggleEnabled();
                        if (clickDeltaTime <= 1f) {
                            if (KK_FBIOpenUp._isenabled) {
                                DrawSlidePic(1, gameMode);
                            } else {
                                DrawSlidePic(2, gameMode);
                            }
                            ChangeRedBagBtn(redBagBtn, gameMode);
                        } else {
                            if (KK_FBIOpenUp._isenabled) {
                                DrawSlidePic(10, gameMode);
                            } else {
                                DrawSlidePic(20, gameMode);
                            }
                            ChangeRedBagBtn(redBagBtn, gameMode);
                        }
                        break;
                }
            });
            trigger.triggers.Add(pointerUp);

            ChangeRedBagBtn(redBagBtn, gameMode);
        }

        internal class ShiftPicture {
            public enum Type {
                picture,
                video
            }

            internal Type type;
            internal float Width {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.sprite.rect.width;
                        case Type.video:
                            return video.texture.width;
                    }
                    return 0;
                }
            }
            internal float Height {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.sprite.rect.height;
                        case Type.video:
                            return video.texture.height;
                    }
                    return 0;
                }
            }
            internal Transform Transform {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.transform;
                        case Type.video:
                            return video.transform;
                    }
                    return null;
                }
            }
            internal Image image;
            internal RawImage video;
            internal float smoothTime = 0.5f;
            internal Vector3 velocity = Vector3.zero;
            internal Vector3 targetPosition = Vector3.zero;
        }
        private static float videoTimer = 0;
        private static ShiftPicture shiftPicture;
        /// <summary>
        /// 繪製轉場圖片
        /// </summary>
        /// <param name="_step">繪製完後要進入的腳本位置</param>
        /// <param name="sceneName">Scene名稱</param>
        private static void DrawSlidePic(int _step, KK_FBIOpenUp.GameMode sceneName) {
            GameObject parent;
            switch (sceneName) {
                case KK_FBIOpenUp.GameMode.Studio:
                    parent = GameObject.Find("StudioScene/Canvas Main Menu");
                    break;
                //case KK_FBIOpenUp.GameMode.LOGO:
                //    parent = GameObject.Find("TitleScene/Canvas/Panel");
                //    break;
                //case KK_FBIOpenUp.GameMode.MyRoom:
                //    parent = GameObject.Find("NightMenuScene/Canvas/Panel/base");
                //    break;
                case KK_FBIOpenUp.GameMode.MainGame:
                    parent = GameObject.Find("ActionScene/UI/ActionMenuCanvas/ModeAnimation");
                    break;
                default:
                    return;
            }
            GameObject gameObject = new GameObject();
            gameObject.transform.SetParent(parent.transform, false);
            gameObject.SetActive(false);
            if (null != shiftPicture) {
                GameObject.Destroy(shiftPicture.Transform.parent.gameObject);
                shiftPicture.image = null;
                shiftPicture.video = null;
                shiftPicture = null;
            }
            shiftPicture = new ShiftPicture();
            switch (_step) {
                case 1:
                    //小學生真是太棒了
                    shiftPicture.type = ShiftPicture.Type.picture;
                    shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.saikodaze.jpg", 800, 657));
                    shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f * 800 / 657, Screen.height / 1.5f);
                    Right2Center();
                    break;
                case 2:
                    //熊吉逮捕
                    shiftPicture.type = ShiftPicture.Type.picture;
                    shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.Kumakichi.jpg", 640, 480));
                    shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f * 640 / 480, Screen.height / 1.5f);
                    Left2Center();
                    break;
                case 10:
                    //幼女退光線
                    shiftPicture.type = ShiftPicture.Type.picture;
                    shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.beam.png", 700, 700));
                    shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.25f, Screen.height / 1.25f);
                    Right2Center();
                    break;
                case 20:
                    //FBI Open Up影片
                    if (null == KK_FBIOpenUp.videoPath) {
                        return;
                    }

                    shiftPicture.type = ShiftPicture.Type.video;

                    shiftPicture.video = UIUtility.CreateRawImage("", gameObject.transform);
                    shiftPicture.video.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f, Screen.height / 1.5f);

                    UnityEngine.Video.VideoPlayer videoPlayer = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                    AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                    videoPlayer.playOnAwake = false;
                    audioSource.playOnAwake = false;
                    videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.APIOnly;

                    //videoPlayer.url= "../UserData/audio/FBI.mp4";
                    videoPlayer.url = KK_FBIOpenUp.videoPath;

                    //Set Audio Output to AudioSource
                    videoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;

                    //Assign the Audio from Video to AudioSource to be played
                    videoPlayer.EnableAudioTrack(0, true);
                    videoPlayer.SetTargetAudioSource(0, audioSource);

                    KK_FBIOpenUp.Logger.LogDebug($"{videoPlayer.url}");
                    videoPlayer.isLooping = true;

                    //先把他移到螢幕外啟用，否則未啟用無法Prepare，而直接啟用會出現白色畫面
                    shiftPicture.Transform.position = new Vector3(-2 * Screen.width, Screen.height / 2);
                    gameObject.SetActive(true);

                    videoPlayer.Prepare();
                    videoPlayer.prepareCompleted += (source) => {
                        if (videoPlayer.texture == null) {
                            KK_FBIOpenUp.Logger.LogError("Video not found");
                            GameObject.Destroy(shiftPicture.Transform.parent.gameObject);
                            shiftPicture.video = null;
                            shiftPicture = null;
                            _step = 0;
                            return;
                        }

                        shiftPicture.video.texture = videoPlayer.texture;
                        videoTimer = 2;
                        videoPlayer.Play();
                        audioSource.Play();

                        //影片太大聲QQ
                        audioSource.volume = KK_FBIOpenUp.videoVolume;

                        Left2Center();
                    };
                    break;
            }

            KK_FBIOpenUp.Logger.LogDebug("Draw Slide Pic");

            void Right2Center() {
                //Right To Center
                shiftPicture.Transform.position = new Vector3(Screen.width + shiftPicture.Width / 2, Screen.height / 2);
                shiftPicture.targetPosition = new Vector3(Screen.width / 2, Screen.height / 2);
                gameObject.SetActive(true);
                step = _step;
            }
            void Left2Center() {
                //Left To Center
                shiftPicture.Transform.position = new Vector3(-1 * (Screen.width + shiftPicture.Width / 2), Screen.height / 2);
                shiftPicture.targetPosition = new Vector3(Screen.width / 2, Screen.height / 2);
                gameObject.SetActive(true);
                step = _step;
            }
        }

        private static float intensityFrom = 1f;
        private static float intensityTo = 5f;
        private static bool intensityState = false;
        private static CameraLightCtrl.LightInfo studioLightInfo;
        private static Vector3 sunlightInfoAngleBackup;
        private static SunLightInfo sunlight;
        private static SunLightInfo.Info sunlightInfo;
        private static SunLightInfo.Info.Type sunlightInfoType;
        private static object studioLightCalc;
        /// <summary>
        /// 調整角色燈光，製造爆亮轉場
        /// </summary>
        /// <param name="goLighter">True轉亮；False轉暗</param>
        private static void ToggleFlashLight(bool goLighter) {
            switch (KK_FBIOpenUp.nowGameMode) {
                case KK_FBIOpenUp.GameMode.Studio:
                    if (null == studioLightInfo || null == studioLightCalc) {
                        studioLightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");
                        studioLightInfo = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
                    }
                    if (goLighter) {
                        intensityFrom = studioLightInfo.intensity;
                        intensityTo = 5f;
                    } else {
                        intensityTo = intensityFrom;
                        intensityFrom = studioLightInfo.intensity;
                    }
                    break;
                case KK_FBIOpenUp.GameMode.MainGame:
                    //ActionGame.ActionMap.sunLightInfo : SunLightInfo
                    var actionMap = Singleton<Manager.Game>.Instance.actScene.Map;
                    sunlight = actionMap.sunLightInfo;
                    sunlightInfoType = ActionGame.ActionMap.CycleToType(actionMap.nowCycle);
                    sunlightInfo = sunlight.infos.FirstOrDefault((SunLightInfo.Info p) => p.type == sunlightInfoType);
                    if (goLighter) {
                        sunlightInfoAngleBackup = sunlightInfo.angle;
                        sunlightInfo.angle = new Vector3(90, 90, 0);
                        intensityFrom = sunlightInfo.intensity;
                        intensityTo = 5f;
                    } else {
                        intensityTo = intensityFrom;
                        intensityFrom = sunlightInfo.intensity;
                    }
                    break;
            }
            intensityState = true;
        }

        private static int step = 0;
        private static int reflectCount = 0;
        internal static void Update() {
            if (videoTimer > 0) {
                videoTimer -= Time.deltaTime;
                return;
            }

            //過場圖片腳本邏輯
            if (null != shiftPicture && null != shiftPicture.Transform) {
                shiftPicture.Transform.position = Vector3.SmoothDamp(shiftPicture.Transform.position, shiftPicture.targetPosition, ref shiftPicture.velocity, shiftPicture.smoothTime);
                //KK_FBIOpenUp.Logger.LogDebug($"Velocity:{velocity} ; Image.position:{shiftPicture.Transform.position}");
                if ((shiftPicture.Transform.position - shiftPicture.targetPosition).sqrMagnitude < 1f) {
                    if (intensityState && reflectCount < 60) {
                        switch (KK_FBIOpenUp.nowGameMode) {
                            case KK_FBIOpenUp.GameMode.Studio:
                                studioLightInfo.intensity += (intensityTo - intensityFrom) / 60;
                                studioLightCalc.Invoke("Reflect");
                                break;
                            case KK_FBIOpenUp.GameMode.MainGame:
                                KK_FBIOpenUp.Logger.LogDebug($"intensity: {sunlightInfo.intensity}");
                                sunlightInfo.intensity += (intensityTo - intensityFrom) / 60;
                                sunlight.Set(sunlightInfoType, Camera.main);
                                break;
                        }
                        reflectCount++;
                    } else {
                        KK_FBIOpenUp.Logger.LogDebug($"At Step: {step}");
                        switch (step) {
                            case 0:
                                //消滅圖片
                                if (null != shiftPicture.Transform.parent.gameObject) {
                                    GameObject.Destroy(shiftPicture.Transform.parent.gameObject);
                                    shiftPicture.image = null;
                                    shiftPicture.video = null;
                                    shiftPicture = null;
                                }
                                break;
                            case 1:
                                //由中間移動到左邊
                                shiftPicture.targetPosition = new Vector3(0 - (shiftPicture.Width / 2), Screen.height / 2);
                                stepSet(0);
                                break;
                            case 2:
                                //由中間移動到右邊
                                shiftPicture.targetPosition = new Vector3(Screen.width + shiftPicture.Width / 2, Screen.height / 2);
                                stepSet(0);
                                break;
                            case 10:
                                //將角色全部替換
                                //加亮角色光
                                reflectCount = 0;
                                ToggleFlashLight(true);
                                stepAdd();
                                break;
                            case 11:
                                intensityState = false;
                                ChangeAllCharacters(false);
                                reflectCount = 0;
                                ToggleFlashLight(false);
                                stepAdd();
                                break;
                            case 12:
                                intensityState = false;
                                if (KK_FBIOpenUp.nowGameMode == KK_FBIOpenUp.GameMode.MainGame) {
                                    sunlightInfo.angle = sunlightInfoAngleBackup;
                                    sunlight.Set(sunlightInfoType, Camera.main);
                                }
                                stepSet(1);
                                break;
                            case 20:
                                //將角色換回
                                //加亮角色光
                                reflectCount = 0;
                                ToggleFlashLight(true);
                                stepAdd();
                                break;
                            case 21:
                                intensityState = false;
                                ChangeAllCharacters(true);
                                reflectCount = 0;
                                ToggleFlashLight(false);
                                stepAdd();
                                break;
                            case 22:
                                intensityState = false;
                                if (KK_FBIOpenUp.nowGameMode == KK_FBIOpenUp.GameMode.MainGame) {
                                    sunlightInfo.angle = sunlightInfoAngleBackup;
                                    sunlight.Set(sunlightInfoType, Camera.main);
                                }
                                stepSet(2);
                                break;
                        }
                    }
                }

                void stepAdd() {
                    step++;
                }

                void stepSet(int st) {
                    step = st;
                }
            }

            //長按計時
            if (downState) {
                btnClickTimer += Time.deltaTime;
            }
        }
        #endregion Unity Stuff
    }
}
