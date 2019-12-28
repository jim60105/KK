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

using ActionGame.H;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using Extension;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;

namespace KK_FBIOpenUp {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("KKABMX.Core", BepInDependency.DependencyFlags.SoftDependency)]
    public class KK_FBIOpenUp : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "FBI Open Up";
        internal const string GUID = "com.jim60105.kk.fbiopenup";
        internal const string PLUGIN_VERSION = "19.12.29.0";
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
            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        public void Update() => UnityStuff.Update();

        public void Start() {
            _isABMXExist = null != Extension.Extension.TryGetPluginInstance("KKABMX.Core", new Version(3, 3));

            //讀取config
            Enable = Config.Bind<bool>("Config", "Enable", false);
            _isenabled = Enable.Value;

            Sample_chara = Config.Bind<string>("Config", "Sample chara", "", "Leave blank to use my default loli");
            string sampleCharaPath = Sample_chara.Value;
            if (sampleCharaPath.Length == 0) {
                Logger.LogDebug("Use default chara");
                //Logger.LogDebug("FBI! Open Up!");
                Assembly ass = Assembly.GetExecutingAssembly();
                using (Stream stream = ass.GetManifestResourceStream("KK_FBIOpenUp.Resources.sample_chara.png")) {
                    Patches.LoadSampleChara(stream);
                }
            } else {
                Logger.LogDebug("Load path: " + sampleCharaPath);
                using (FileStream fileStream = new FileStream(sampleCharaPath, FileMode.Open, FileAccess.Read)) {
                    Patches.LoadSampleChara(fileStream);
                }
            }

            Change_rate = Config.Bind<float>("Config", "Change rate", 0.77f, "Proportion of change from original character to sample character, 1 is completely changed");
            float rate = Change_rate.Value;
            Patches.ChangeRate = rate;
            Logger.LogDebug("Change Rate: " + rate);

            Video_related_path = Config.Bind<string>("Config", "Video related path", "UserData/audio/FBI.mp4", "Relative path where FBI Open Up video is located");
            string tempVideoPath = Video_related_path.Value;
            if (!File.Exists(tempVideoPath)) {
                Logger.LogError("Video Not Found: " + tempVideoPath);
                videoPath = null;
            } else {
                videoPath = $"file://{Application.dataPath}/../{tempVideoPath}";
            }

            Video_volume = Config.Bind<float>("Config", "Video volume", 0.06f);
            videoVolume = Video_volume.Value;
            Logger.LogDebug($"Set video volume to {videoVolume}");

            //if (Application.productName == "CharaStudio") {
            //    nowGameMode = GameMode.Studio;
            //} else {
            //    if (Manager.Game.Instance != null) {
            //        nowGameMode = GameMode.MainGame;
            //    }
            //}
        }

        internal static void SetEnabled() {
            SetEnabled(!_isenabled);
        }
        internal static void SetEnabled(bool b) {
            _isenabled = b;
            Enable.Value = b;
        }
    }

    static class SampleChara {
        internal static List<float> shapeValueFace;
        internal static List<float> shapeValueBody;
        //internal static PluginData ABMXData;
        internal static ChaFileControl chaFile;
    }

    internal static class Patches {
        private static readonly ManualLogSource Logger = KK_FBIOpenUp.Logger;
        /// <summary>
        /// 替換過的chara之原始數據 Dict(ChaFileCustom, List[]{shapeValueFace.toList, shapeValueBody.toList})
        /// </summary>
        internal static readonly Dictionary<ChaFileCustom, List<float>[]> chaFileCustomDict = new Dictionary<ChaFileCustom, List<float>[]>();

        /// <summary>
        /// 要向sample改變的程度，範圍0(無替換)~1(全替換)
        /// </summary>
        public static float ChangeRate { get; set; }

        /// <summary>
        /// 載入sample chara
        /// </summary>
        /// <param name="stream">角色圖片讀取為Stream</param>
        public static void LoadSampleChara(Stream stream) {
            Hooks.BlockChanging = true;
            SampleChara.chaFile = new ChaFileControl();
            SampleChara.chaFile.Invoke("LoadCharaFile", new object[] { stream, true, true });
            Logger.LogDebug("Loaded sample chara: " + SampleChara.chaFile.parameter.fullname);
            Hooks.BlockChanging = false;
            ChaFileFace face = MessagePackSerializer.Deserialize<ChaFileFace>(MessagePackSerializer.Serialize<ChaFileFace>(SampleChara.chaFile.custom.face));
            ChaFileBody body = MessagePackSerializer.Deserialize<ChaFileBody>(MessagePackSerializer.Serialize<ChaFileBody>(SampleChara.chaFile.custom.body));
            //Logger.LogMessage("Length Face: " + face.shapeValueFace.Length);
            //Logger.LogMessage("Length Body: " + body.shapeValueBody.Length);
            SampleChara.shapeValueFace = face.shapeValueFace.ToList();
            SampleChara.shapeValueBody = body.shapeValueBody.ToList();

            //SampleChara.ABMXData = ExtendedSave.GetExtendedDataById(SampleChara.chaFile, "KKABMPlugin.ABMData");
            //if (null != SampleChara.ABMXData) {
            //    Logger.LogDebug("Loaded sample chara ABMX");
            //} else {
            //    Logger.LogDebug("NO sample chara ABMX");
            //}
        }

        /// <summary>
        /// 替換角色
        /// </summary>
        /// <param name="chaCtrl">目標chara</param>
        /// <param name="changeFace">是否替換臉部</param>
        /// <param name="changeBody">是否替換身體</param>
        public static void ChangeChara(ChaControl chaCtrl, bool changeFace = true, bool changeBody = true, bool disableDoubleChange = true) {
            if (Hooks.BlockChanging || null == chaCtrl || null == chaCtrl.chaFile) {
                return;
            }

            if (chaCtrl.chaFile.parameter.sex != SampleChara.chaFile.parameter.sex) {
                Logger.LogInfo("Skip changing because of wrong sex.");
                return;
            }

            List<float> originalShapeValueFace;
            List<float> originalShapeValueBody;
            ChaFileCustom chaFileCustom = chaCtrl.chaFile.custom;

            originalShapeValueFace = chaFileCustom.face.shapeValueFace.ToList();
            originalShapeValueBody = chaFileCustom.body.shapeValueBody.ToList();

            //如果角色第一次替換，紀錄其原始數據至dict
            //如果在dict內有找到替換紀錄，以其原始數據來做替換
            //(不block掉是因為，即使在單次Loading Chara內，此function也會被trigger不止一次)
            if (chaFileCustomDict.TryGetValue(chaFileCustom, out List<float>[] chaFileCustomStored)) {
                if (disableDoubleChange) {
                    chaFileCustomDict[chaFileCustom] = new List<float>[] { new List<float>(originalShapeValueFace), new List<float>(originalShapeValueBody) };
                } else {
                    originalShapeValueFace = chaFileCustomStored[0].ToList();
                    originalShapeValueBody = chaFileCustomStored[1].ToList();
                }
            } else {
                chaFileCustomDict.Add(chaFileCustom, new List<float>[] { new List<float>(originalShapeValueFace), new List<float>(originalShapeValueBody) });
                Logger.LogDebug("chaFileCustomDict.Count: " + chaFileCustomDict.Count);
            }

            if (null != SampleChara.shapeValueFace && changeFace) {
                if (originalShapeValueFace.Count == SampleChara.shapeValueFace.Count && originalShapeValueFace.Count == chaFileCustom.face.shapeValueFace.Length) {
                    for (int i = 0; i < originalShapeValueFace.Count; i++) {
                        chaFileCustom.face.shapeValueFace[i] = originalShapeValueFace[i] + ((SampleChara.shapeValueFace[i] - originalShapeValueFace[i]) * ChangeRate);
                    }
                } else { Logger.LogError("Sample data is not match to target data!"); }
                Logger.LogDebug("Changed face finish");
            }

            if (null != SampleChara.shapeValueBody && changeBody) {
                if (originalShapeValueBody.Count == SampleChara.shapeValueBody.Count && originalShapeValueBody.Count == chaFileCustom.body.shapeValueBody.Length) {
                    for (int i = 0; i < originalShapeValueBody.Count; i++) {
                        chaFileCustom.body.shapeValueBody[i] = originalShapeValueBody[i] + ((SampleChara.shapeValueBody[i] - originalShapeValueBody[i]) * ChangeRate);
                    }
                } else { Logger.LogError("Sample data is not match to target data!"); }
                Logger.LogDebug("Changed body finish");
            }
            //chaCtrl.Reload();

            #region ABMX
            //if (KK_FBIOpenUp._isABMXExist) {
            //    //取得BoneController
            //    object BoneController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
            //    if (null == BoneController) {
            //        Logger.LogDebug("No ABMX BoneController found");
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
            //            Logger.LogDebug("Clean new coordinate ABMX BoneData: " + (string)x.GetProperty("BoneName"));
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
            //            Logger.LogDebug("Rollback cooridnate ABMX BoneData: " + bonename);
            //        } else {
            //            Logger.LogError("Duplicate coordinate ABMX BoneData: " + bonename);
            //        }
            //        i++;
            //    }
            //    Logger.LogDebug($"Merge {i} previous ABMX Bone Modifiers");

            //    //重整
            //    BoneController.SetProperty("NeedsFullRefresh", true);
            //    BoneController.SetProperty("NeedsBaselineUpdate", true);
            //    BoneController.Invoke("LateUpdate");

            //    //把ABMX的數據存進擴充資料
            //    BoneController.Invoke("OnCardBeingSaved", new object[] { 1 });
            //    BoneController.Invoke("OnReload", new object[] { 2, false });

            //    //列出角色身上所有ABMX數據
            //    Logger.LogDebug("--List all exist ABMX BoneData--");
            //    foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames", null)) {
            //        var modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
            //        if (null != modifier) {
            //            Logger.LogDebug("" + boneName);
            //        }
            //    }
            //}
            #endregion
            Logger.LogDebug($"Changed.");
        }

        /// <summary>
        /// 將所有角色做替換
        /// </summary>
        public static void ChangeAllCharacters(bool rollback = false) {
            List<ChaControl> charList = new List<ChaControl>();
            Logger.LogDebug($"GameMode: {Enum.GetNames(typeof(KK_FBIOpenUp.GameMode))[(int)KK_FBIOpenUp.nowGameMode]}");
            switch (KK_FBIOpenUp.nowGameMode) {
                case KK_FBIOpenUp.GameMode.Studio:
                    charList = Studio.Studio.Instance.dicInfo.Values.OfType<Studio.OCIChar>().Select(x => x.charInfo).ToList();
                    break;
                case KK_FBIOpenUp.GameMode.Maker:
                    charList.Add(Singleton<ChaCustom.CustomBase>.Instance.chaCtrl);
                    break;
                case KK_FBIOpenUp.GameMode.MainGame:
                    charList = Singleton<Manager.Game>.Instance.HeroineList.Select(x => x.chaCtrl).ToList();
                    break;
                case KK_FBIOpenUp.GameMode.FreeH:
                    charList = Hooks.hSceneProc.GetField("lstFemale").ToList<ChaControl>();
                    break;
            }
            if (null == charList) {
                Logger.LogError("Get CharaList FAILED! This should not happen!");
                return;
            }
            Logger.LogDebug($"Get {charList.Count} charaters.");
            foreach (ChaControl chaCtrl in charList) {
                if (null == chaCtrl) {
                    continue;
                }
                if (rollback) {
                    RollbackChara(chaCtrl);
                } else {
                    ChangeChara(chaCtrl, true, true, false);
                }
                switch (KK_FBIOpenUp.nowGameMode) {
                    case KK_FBIOpenUp.GameMode.Maker:
                        Singleton<ChaCustom.CustomBase>.Instance.updateCustomUI = true;
                        break;
                    default:
                        chaCtrl.Reload();
                        break;
                }
            }
        }

        public static void RollbackChara(ChaControl chaCtrl) {
            if (Hooks.BlockChanging || null == chaCtrl || null == chaCtrl.chaFile) {
                return;
            }

            if (chaCtrl.chaFile.parameter.sex != SampleChara.chaFile.parameter.sex) {
                Logger.LogInfo("Skip changing because of wrong sex.");
                return;
            }

            if (chaCtrl.chaFile.custom is ChaFileCustom chaFileCustom) {
                if (chaFileCustomDict.TryGetValue(chaFileCustom, out List<float>[] chaFileCustomStored)) {
                    bool[] done = { false, false };
                    if (chaFileCustomStored[0].Count == chaFileCustom.face.shapeValueFace.Length) {
                        chaFileCustom.face.shapeValueFace = chaFileCustomStored[0].ToArray();
                        done[0] = true;
                    } else { Logger.LogError("Backup face data is not match to target data!"); }

                    if (chaFileCustomStored[1].Count == chaFileCustom.body.shapeValueBody.Length) {
                        chaFileCustom.body.shapeValueBody = chaFileCustomStored[1].ToArray();
                        done[1] = true;
                    } else { Logger.LogError("Backup body data is not match to target data!"); }

                    if (done[0] & done[1]) {
                        chaFileCustomDict.Remove(chaFileCustom);
                    }
                    chaCtrl.Reload(true, false, true, false);
                    Logger.LogDebug($"Rollbacked.");
                } else {
                    Logger.LogInfo($"No rollback data found for {chaCtrl.chaFile.parameter.fullname}");
                }
            }
        }
    }
}
