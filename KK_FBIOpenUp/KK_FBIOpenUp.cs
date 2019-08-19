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
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Extension;
using Harmony;
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
using Logger = BepInEx.Logger;

namespace KK_FBIOpenUp {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    //[BepInProcess("CharaStudio")]
    public class KK_FBIOpenUp : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "FBI Open Up";
        internal const string GUID = "com.jim60105.kk.fbiopenup";
        internal const string PLUGIN_VERSION = "19.08.18.0";

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
        public void Awake() {
            UIUtility.Init();
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
        }

        public void Update() {
            Patches.Update();
        }

        public void Start() {
            bool IsPluginExist(string pluginName) {
                return null != BepInEx.Bootstrap.Chainloader.Plugins.Select(MetadataHelper.GetMetadata).FirstOrDefault(x => x.GUID == pluginName);
            }

            _isABMXExist = IsPluginExist("KKABMX.Core");// && ABMX_Support.LoadAssembly();

            //讀取config
            BepInEx.Config.ReloadConfig();
            _isenabled = string.Equals(BepInEx.Config.GetEntry("enabled", "False", PLUGIN_NAME), "True");
            string path = BepInEx.Config.GetEntry("sample_chara", "", PLUGIN_NAME);
            if (float.TryParse(BepInEx.Config.GetEntry("change_rate", "0.77", PLUGIN_NAME), out float rate)) {
                Patches.ChangeRate = rate;
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Change Rate: " + rate);
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
            BepInEx.Config.SetEntry("enabled", _isenabled ? "True" : "False", PLUGIN_NAME);
            BepInEx.Config.ReloadConfig();
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
            Logger.Log(LogLevel.Debug, "[KK_FBIOU] Loaded sample chara: " + SampleChara.chaFile.parameter.fullname);
            blockChanging = false;
            var face = MessagePackSerializer.Deserialize<ChaFileFace>(MessagePackSerializer.Serialize<ChaFileFace>(SampleChara.chaFile.custom.face));
            var body = MessagePackSerializer.Deserialize<ChaFileBody>(MessagePackSerializer.Serialize<ChaFileBody>(SampleChara.chaFile.custom.body));
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Face: " + face.shapeValueFace.Length);
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Body: " + body.shapeValueBody.Length);
            SampleChara.shapeValueFace = face.shapeValueFace.ToList();
            SampleChara.shapeValueBody = body.shapeValueBody.ToList();

            SampleChara.ABMXData = ExtendedSave.GetExtendedDataById(SampleChara.chaFile, "KKABMPlugin.ABMData");
            if (null != SampleChara.ABMXData) {
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Loaded sample chara ABMX");
            } else {
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] NO sample chara ABMX");
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
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Skip changing because of wrong sex.");
                return;
            }

            //int j = 0;
            //Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++j);
            List<float> originalShapeValueFace;
            List<float> originalShapeValueBody;
            ChaFileCustom chaFileCustom = chaCtrl.chaFile.custom;
            //Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++j);

            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Face: " + chaFileCustom.face.shapeValueFace.Length);
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Body: " + chaFileCustom.body.shapeValueBody.Length);
            originalShapeValueFace = chaFileCustom.face.shapeValueFace.ToList();
            originalShapeValueBody = chaFileCustom.body.shapeValueBody.ToList();
            List<float> result;
            //Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++j);

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
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] chaFileCustomDict.Count: " + chaFileCustomDict.Count);
            }
            //Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++j);

            if (null != SampleChara.shapeValueFace && changeFace) {
                if (originalShapeValueFace.Count == SampleChara.shapeValueFace.Count) {
                    result = new List<float>();
                    for (int i = 0; i < originalShapeValueFace.Count; i++) {
                        result.Add(SampleChara.shapeValueFace[i] + ((originalShapeValueFace[i] - SampleChara.shapeValueFace[i]) * keepRate));
                    }
                    chaFileCustom.face.shapeValueFace = result.ToArray();
                } else { Logger.Log(LogLevel.Error, "[KK_FBIOU] Sample data is not match to target data!"); }
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Changed face finish");
            }
            //Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++j);

            if (null != SampleChara.shapeValueBody && changeBody) {
                if (originalShapeValueBody.Count == SampleChara.shapeValueBody.Count) {
                    result = new List<float>();
                    for (int i = 0; i < originalShapeValueBody.Count; i++) {
                        result.Add(SampleChara.shapeValueBody[i] + ((originalShapeValueBody[i] - SampleChara.shapeValueBody[i]) * keepRate));
                    }
                    chaFileCustom.body.shapeValueBody = result.ToArray();
                    chaCtrl.Reload(true, false, true, false);
                } else { Logger.Log(LogLevel.Error, "[KK_FBIOU] Sample data is not match to target data!"); }
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Changed body finish");
            }

            //Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++j);
            if (KK_FBIOpenUp._isABMXExist) {
                //取得BoneController
                object BoneController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
                if (null == BoneController) {
                    Logger.Log(LogLevel.Debug, "[KK_FBIOU] No ABMX BoneController found");
                    return;
                }

                //建立重用function
                void GetModifiers(Action<object> action) {
                    foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames")) {
                        var modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
                        if (null != modifier) {
                            action(modifier);
                        }
                    }
                }

                //取得舊角色衣服ABMX數據
                List<object> previousModifier = new List<object>();
                GetModifiers(x => {
                    if ((bool)x.Invoke("IsCoordinateSpecific")) {
                        previousModifier.Add(x);
                    }
                });

                //將擴充資料由暫存複製到角色身上
                ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, "KKABMPlugin.ABMData", SampleChara.ABMXData);

                //把擴充資料載入ABMX插件
                BoneController.Invoke("OnReload", new object[] { 2, false });

                //清理新角色數據，將衣服數據刪除
                List<object> newModifiers = new List<object>();
                int i = 0;
                GetModifiers(x => {
                    if ((bool)x.Invoke("IsCoordinateSpecific")) {
                        Logger.Log(LogLevel.Debug, "[KK_FBIOU] Clean new coordinate ABMX BoneData: " + (string)x.GetProperty("BoneName"));
                        x.Invoke("MakeNonCoordinateSpecific");
                        var y = x.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)0 });
                        y.Invoke("Clear");
                        x.Invoke("MakeCoordinateSpecific");    //保險起見以免後面沒有成功清除
                        i++;
                    } else {
                        newModifiers.Add(x);
                    }
                });

                //將舊的衣服數據合併回到角色身上
                i = 0;
                foreach (var modifier in previousModifier) {
                    string bonename = (string)modifier.GetProperty("BoneName");
                    if (!newModifiers.Any(x => string.Equals(bonename, (string)x.GetProperty("BoneName")))) {
                        BoneController.Invoke("AddModifier", new object[] { modifier });
                        Logger.Log(LogLevel.Debug, "[KK_FBIOU] Rollback cooridnate ABMX BoneData: " + bonename);
                    } else {
                        Logger.Log(LogLevel.Error, "[KK_FBIOU] Duplicate coordinate ABMX BoneData: " + bonename);
                    }
                    i++;
                }
                Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Merge {i} previous ABMX Bone Modifiers");

                //重整
                BoneController.SetProperty("NeedsFullRefresh", true);
                BoneController.SetProperty("NeedsBaselineUpdate", true);
                BoneController.Invoke("LateUpdate");

                //把ABMX的數據存進擴充資料
                BoneController.Invoke("OnCardBeingSaved", new object[] { 1 });
                BoneController.Invoke("OnReload", new object[] { 2, false });

                //列出角色身上所有ABMX數據
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] --List all exist ABMX BoneData--");
                foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames", null)) {
                    var modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
                    if (null != modifier) {
                        Logger.Log(LogLevel.Debug, "[KK_FBIOU] " + boneName);
                    }
                }
            }
        }

        /// <summary>
        /// 將所有角色做替換
        /// </summary>
        public static void ChangeAllCharacters() {
            List<ChaControl> charList = new List<ChaControl>();
            Logger.Log(LogLevel.Debug, $"[KK_FBIOU] GameMode: {Enum.GetNames(typeof(KK_FBIOpenUp.GameMode))[(int)KK_FBIOpenUp.nowGameMode]}");
            switch (KK_FBIOpenUp.nowGameMode) {
                case KK_FBIOpenUp.GameMode.Studio:
                    charList = Studio.Studio.Instance.dicInfo.Values.OfType<Studio.OCIChar>().Select(x => x.charInfo).ToList();
                    break;
                case KK_FBIOpenUp.GameMode.MainGame:
                    charList = Singleton<Manager.Game>.Instance.HeroineList.Select(x => x.chaCtrl).ToList();
                    break;
            }
            Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Get {charList.Count} charaters.");
            if (null != charList) {
                foreach (var chaCtrl in charList) {
                    ChangeChara(chaCtrl, true, true, false);
                    //Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Changed {chaCtrl.chaFile.na}");
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

        [HarmonyPostfix, HarmonyPatch(typeof(TitleScene), "Start")]
        public static void StartPostfix2(TitleScene __instance) {
            KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.LOGO;
            DrawRedBagBtn(__instance, KK_FBIOpenUp.GameMode.LOGO);
        }

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
            Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Set Init: {flag}");
        }

        #endregion Hooks

        #region Unity Stuff
        /// <summary>
        /// 切換紅色書包圖標顯示
        /// </summary>
        /// <param name="showPic">是否顯示過場圖片</param>
        private static void ChangeRedBagBtn(GameObject redBagBtn, bool showPic, KK_FBIOpenUp.GameMode gameMode) {
            if (KK_FBIOpenUp._isenabled) {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                if (showPic) {
                    DrawSlidePic(1, gameMode);
                }
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Enable Plugin");
            } else {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Disable Plugin");
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
                        if (KK_FBIOpenUp._isenabled) {
                            DrawSlidePic(20, gameMode);
                        } else {
                            DrawSlidePic(10, gameMode);
                        }
                        KK_FBIOpenUp._isenabled = !KK_FBIOpenUp._isenabled;
                        ChangeRedBagBtn(redBagBtn, false, gameMode);
                        break;
                    case KK_FBIOpenUp.GameMode.LOGO:
                    case KK_FBIOpenUp.GameMode.MyRoom:
                        KK_FBIOpenUp.ToggleEnabled();
                        ChangeRedBagBtn(redBagBtn, true, gameMode);
                        break;
                    case KK_FBIOpenUp.GameMode.Studio:
                        if (clickDeltaTime <= 1f) {
                            KK_FBIOpenUp.ToggleEnabled();
                            ChangeRedBagBtn(redBagBtn, true, gameMode);
                        } else {
                            DrawSlidePic(10, gameMode);
                        }
                        break;
                }
            });
            trigger.triggers.Add(pointerUp);

            ChangeRedBagBtn(redBagBtn, false, gameMode);
        }

        internal class ShiftPicture {
            public enum Type {
                picture,
                movie
            }

            internal Type type;
            internal float Width {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.sprite.rect.width;
                        case Type.movie:
                            return movie.texture.width;
                    }
                    return 0;
                }
            }
            internal float Height {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.sprite.rect.height;
                        case Type.movie:
                            return movie.texture.height;
                    }
                    return 0;
                }
            }
            internal Transform Transform {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.transform;
                        case Type.movie:
                            return movie.transform;
                    }
                    return null;
                }
            }
            internal Image image;
            internal RawImage movie;
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
            if (null == shiftPicture) {
                shiftPicture = new ShiftPicture();
                switch (_step) {
                    case 1:
                        shiftPicture.type = ShiftPicture.Type.picture;
                        shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.saikodaze.jpg", 800, 657));
                        shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f * 800 / 657, Screen.height / 1.5f);
                        goto case -1;
                    case 10:
                        shiftPicture.type = ShiftPicture.Type.picture;
                        shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.beam.png", 700, 700));
                        shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.25f, Screen.height / 1.25f);
                        goto case -1;
                    case 20:
                        shiftPicture.type = ShiftPicture.Type.movie;
                        WWW www = new WWW("file://" + Application.dataPath + "/../UserData/audio/FBI.ogg");
                        MovieTexture movieTexture = WWWAudioExtensions.GetMovieTexture(www);
                        movieTexture.loop = true;
                        shiftPicture.movie = UIUtility.CreateRawImage("", gameObject.transform, movieTexture);
                        shiftPicture.movie.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f, Screen.height / 1.5f);
                        AudioSource audio = shiftPicture.movie.gameObject.AddComponent<AudioSource>();
                        audio.clip = movieTexture.audioClip;
                        audio.loop = true;
                        movieTexture.Play();
                        audio.Play();
                        videoTimer = 2;
                        goto case -2;
                    case -1:
                        //Right To Center
                        shiftPicture.Transform.position = new Vector3(Screen.width + shiftPicture.Width / 2, Screen.height / 2);
                        shiftPicture.targetPosition = new Vector3(Screen.width / 2, Screen.height / 2);
                        break;
                    case -2:
                        //Left To Center
                        shiftPicture.Transform.position = new Vector3(-1 * (Screen.width + shiftPicture.Width / 2), Screen.height / 2);
                        shiftPicture.targetPosition = new Vector3(Screen.width / 2, Screen.height / 2);
                        break;
                }
            }
            step = _step;

            //Logger.Log(LogLevel.Info, "[KK_FBIOU] Draw Pic Finish");
        }

        private static float intensityBackup = 1f;
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
                        intensityBackup = studioLightInfo.intensity;
                        intensityTo = 5f;
                    } else {
                        intensityTo = intensityBackup;
                        intensityBackup = studioLightInfo.intensity;
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
                        intensityBackup = sunlightInfo.intensity;
                        intensityTo = 5f;
                    } else {
                        intensityTo = intensityBackup;
                        intensityBackup = sunlightInfo.intensity;
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
                //Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Velocity:{velocity} ; Image.position:{shiftPicture.Transform.position}");
                if ((shiftPicture.Transform.position - shiftPicture.targetPosition).sqrMagnitude < 1f) {
                    if (intensityState && reflectCount < 60) {
                        switch (KK_FBIOpenUp.nowGameMode) {
                            case KK_FBIOpenUp.GameMode.Studio:
                                studioLightInfo.intensity += (intensityTo - intensityBackup) / 60;
                                studioLightCalc.Invoke("Reflect");
                                break;
                            case KK_FBIOpenUp.GameMode.MainGame:
                                Logger.Log(LogLevel.Debug, $"[KK_FBIOU] intensity: {sunlightInfo.intensity}");
                                sunlightInfo.intensity += (intensityTo - intensityBackup) / 60;
                                sunlight.Set(sunlightInfoType, Camera.main);
                                break;
                        }
                        reflectCount++;
                    } else {
                        Logger.Log(LogLevel.Debug, $"[KK_FBIOU] At Step: {step}");
                        switch (step) {
                            case 1:
                                //由中間移動到左邊
                                shiftPicture.targetPosition = new Vector3(0 - (shiftPicture.Width / 2), Screen.height / 2);
                                stepSet(3);
                                break;
                            case 2:
                                //由中間移動到右邊
                                shiftPicture.targetPosition = new Vector3(Screen.width + shiftPicture.Width / 2, Screen.height / 2);
                                stepSet(3);
                                break;
                            case 3:
                                //消滅圖片
                                GameObject.Destroy(shiftPicture.Transform.parent.gameObject);
                                shiftPicture.image = null;
                                shiftPicture.movie = null;
                                shiftPicture = null;
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
                                ChangeAllCharacters();
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
                                //ChangeAllCharacters();
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
