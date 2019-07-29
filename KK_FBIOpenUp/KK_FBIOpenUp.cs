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
using ExtensibleSaveFormat;
using Extension;
using Harmony;
using MessagePack;
using Studio;
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
        internal const string PLUGIN_VERSION = "19.07.07.1";

        internal static bool _isenabled = false;
        internal static bool _isABMXExist = false;
        internal enum SceneName {
            Studio = 0,
            MainTitle
        }
        public void Awake() {
            UIUtility.Init();
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
        }

        public void Update() => Patches.Update();

        public void Start() {
            bool IsPluginExist(string pluginName) {
                return null != BepInEx.Bootstrap.Chainloader.Plugins.Select(MetadataHelper.GetMetadata).FirstOrDefault(x => x.GUID == pluginName);
            }

            _isABMXExist = IsPluginExist("KKABMX.Core");// && ABMX_Support.LoadAssembly();

            //讀取config
            BepInEx.Config.ReloadConfig();
            _isenabled = String.Equals(BepInEx.Config.GetEntry("enabled", "False", PLUGIN_NAME), "True");
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
        }

        internal static void ToggleEnabled() {
            _isenabled = !_isenabled;
            BepInEx.Config.SetEntry("enabled", _isenabled ? "True" : "False", PLUGIN_NAME);
            BepInEx.Config.ReloadConfig();
        }
    }
    internal class Patches {
        private static List<float> sampleShapeValueFace;
        private static List<float> sampleShapeValueBody;
        private static PluginData sampleABMXData;
        private static float keepRate;
        private static GameObject redBagBtn;

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

        /// <summary>
        /// 載入sample chara
        /// </summary>
        /// <param name="stream">角色圖片讀取為Stream</param>
        public static void LoadSampleChara(Stream stream) {
            blockChanging = true;
            ChaFileControl chaFile = new ChaFileControl();
            chaFile.Invoke("LoadCharaFile", new object[] { stream, true, true });
            Logger.Log(LogLevel.Debug, "[KK_FBIOU] Loaded sample chara: " + chaFile.parameter.fullname);
            blockChanging = false;
            var face = MessagePackSerializer.Deserialize<ChaFileFace>(MessagePackSerializer.Serialize<ChaFileFace>(chaFile.custom.face));
            var body = MessagePackSerializer.Deserialize<ChaFileBody>(MessagePackSerializer.Serialize<ChaFileBody>(chaFile.custom.body));
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Face: " + face.shapeValueFace.Length);
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Body: " + body.shapeValueBody.Length);
            sampleShapeValueFace = face.shapeValueFace.ToList();
            sampleShapeValueBody = body.shapeValueBody.ToList();

            sampleABMXData = ExtendedSave.GetExtendedDataById(chaFile, "KKABMPlugin.ABMData");
            if (null != sampleABMXData) {
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Loaded sample chara ABMX");
            } else {
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] NO sample chara ABMX");
            }
        }

        /// <summary>
        /// 替換過的chara之原始數據 Dict(ChaFileCustom, List[]{shapeValueFace.toList, shapeValueBody.toList})
        /// </summary>
        private static Dictionary<ChaFileCustom, List<float>[]> chaFileCustomDict = new Dictionary<ChaFileCustom, List<float>[]>();

        /// <summary>
        /// 替換角色
        /// </summary>
        /// <param name="chaCtrl">目標chara</param>
        /// <param name="changeFace">是否替換臉部</param>
        /// <param name="changeBody">是否替換身體</param>
        public static void ChangeChara(ChaControl chaCtrl, bool changeFace = true, bool changeBody = true, bool forceChange = true) {
            if (blockChanging || !KK_FBIOpenUp._isenabled) {
                return;
            }

            if (chaCtrl.chaFile.parameter.sex != 1) {
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Skip changing because of wrong sex.");
                return;
            }

            int iii = 0;
            Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++iii);
            List<float> originalShapeValueFace;
            List<float> originalShapeValueBody;
            ChaFileCustom chaFileCustom = chaCtrl.chaFile.custom;
            Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++iii);

            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Face: " + chaFileCustom.face.shapeValueFace.Length);
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Body: " + chaFileCustom.body.shapeValueBody.Length);
            originalShapeValueFace = chaFileCustom.face.shapeValueFace.ToList();
            originalShapeValueBody = chaFileCustom.body.shapeValueBody.ToList();
            List<float> result;
            Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++iii);

            //如果角色第一次替換，紀錄其原始數據至dict
            //如果在dict內有找到替換紀錄，以其原始數據來做替換
            //(不block掉是因為，即使在單次Loading Chara內，此function也會被trigger不止一次)
            if (chaFileCustomDict.TryGetValue(chaFileCustom, out var chaFileCustomStored)) {
                if (forceChange) {
                    chaFileCustomDict[chaFileCustom] = new List<float>[] { new List<float>(originalShapeValueFace), new List<float>(originalShapeValueBody) };
                } else {
                    originalShapeValueFace = chaFileCustomStored[0].ToList();
                    originalShapeValueBody = chaFileCustomStored[1].ToList();
                }
            } else {
                chaFileCustomDict.Add(chaFileCustom, new List<float>[] { new List<float>(originalShapeValueFace), new List<float>(originalShapeValueBody) });
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] chaFileCustomDict.Count: " + chaFileCustomDict.Count);
            }
            Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++iii);

            if (null != sampleShapeValueFace && changeFace) {
                if (originalShapeValueFace.Count == sampleShapeValueFace.Count) {
                    result = new List<float>();
                    for (int i = 0; i < originalShapeValueFace.Count; i++) {
                        result.Add(sampleShapeValueFace[i] + ((originalShapeValueFace[i] - sampleShapeValueFace[i]) * keepRate));
                    }
                    chaFileCustom.face.shapeValueFace = result.ToArray();
                } else { Logger.Log(LogLevel.Error, "[KK_FBIOU] Sample data is not match to target data!"); }
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Changed face finish");
            }
            Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++iii);

            if (null != sampleShapeValueBody && changeBody) {
                if (originalShapeValueBody.Count == sampleShapeValueBody.Count) {
                    result = new List<float>();
                    for (int i = 0; i < originalShapeValueBody.Count; i++) {
                        result.Add(sampleShapeValueBody[i] + ((originalShapeValueBody[i] - sampleShapeValueBody[i]) * keepRate));
                    }
                    chaFileCustom.body.shapeValueBody = result.ToArray();
                    chaCtrl.Reload(true, false, true, false);
                } else { Logger.Log(LogLevel.Error, "[KK_FBIOU] Sample data is not match to target data!"); }
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Changed body finish");
            }

            Logger.Log(LogLevel.Info, "[KK_FBIOU] " + ++iii);
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
                ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, "KKABMPlugin.ABMData", sampleABMXData);

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
                    if (!newModifiers.Any(x => String.Equals(bonename, (string)x.GetProperty("BoneName")))) {
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
            List<OCIChar> charList = Studio.Studio.Instance.dicInfo.Values.OfType<Studio.OCIChar>().ToList();
            charList.ForEach(new Action<OCIChar>(delegate (OCIChar ocichar) {
                ChangeChara(ocichar.charInfo, true, true, false);
                Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Changed {ocichar.charInfo.name}");
            }));
        }

        #region Hooks
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            if (String.Equals(__instance.name, "00_Female")) {
                DrawRedBagBtn(__instance, KK_FBIOpenUp.SceneName.Studio);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TitleScene), "Start")]
        public static void StartPostfix(TitleScene __instance) {
            DrawRedBagBtn(__instance, KK_FBIOpenUp.SceneName.MainTitle);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFemale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void AddPostfix(ChaControl _female, OICharInfo _info) => ChangeChara(_female);

        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), "ChangeChara")]
        public static void ChangeCharaPostfix(OCIChar __instance) => ChangeChara(__instance.charInfo);

        //[HarmonyPostfix, HarmonyPatch(typeof(SaveData.CharaData), "Load")]
        //public static void LoadPostfix(SaveData.CharaData __instance) => ChangeChara(__instance.chaCtrl);

        private static bool blockChanging = false;
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPrefix() => SetInitFlag(true);
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPostfix() => SetInitFlag(false);

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "Awake")]
        public static void LoadPrefix() => SetInitFlag(true);
        [HarmonyPostfix, HarmonyPatch(typeof(SceneLoadScene), "OnClickClose")]
        public static void LoadPostfix() => SetInitFlag(false);

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
        private static void ChangeRedBagBtn(bool showPic = true,KK_FBIOpenUp.SceneName sceneName = KK_FBIOpenUp.SceneName.Studio) {
            if (KK_FBIOpenUp._isenabled) {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                if (showPic) {
                    DrawSlidePic(1,sceneName);
                }
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Enable Plugin");
            } else {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Disable Plugin");
            }
        }

        private static float btnClickTimer = 0;
        private static bool downState = false;
        private static void DrawRedBagBtn(object __instance, KK_FBIOpenUp.SceneName sceneName) {
            GameObject original, parent;
            Vector2 offsetMin, offsetMax;
            switch (sceneName) {
                case KK_FBIOpenUp.SceneName.Studio:
                    CharaList charaList = __instance as CharaList;
                    original = GameObject.Find($"StudioScene/Canvas Main Menu/01_Add/{charaList.name}/Button Change");
                    parent = original.transform.parent.gameObject;
                    offsetMin = new Vector2(-120, -270);
                    offsetMax = new Vector2(-40, -190);
                    break;
                case KK_FBIOpenUp.SceneName.MainTitle:
                    original = GameObject.Find("TitleScene/Canvas/Panel/Buttons/FirstButtons/Button Start");
                    parent = original.transform.parent.parent.parent.gameObject;
                    offsetMin = new Vector2(0, -80);
                    offsetMax = new Vector2(80, 0);
                    break;
                default:
                    return;
            }
            redBagBtn = UnityEngine.Object.Instantiate(original, parent.transform);
            redBagBtn.name = "redBagBtn";
            redBagBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), offsetMin, offsetMax);

            redBagBtn.GetComponent<Button>().spriteState = new SpriteState();
            redBagBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.redBag.png", 100, 100);
            redBagBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            redBagBtn.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            redBagBtn.GetComponent<Button>().interactable = true;

            //因為要handle長按，改為監聽PointerDown、PointerUp Event
            redBagBtn.AddComponent<EventTrigger>();
            EventTrigger trigger = redBagBtn.gameObject.GetComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerDown,
                callback = new EventTrigger.TriggerEvent()
            };
            pointerDown.callback.AddListener(delegate {
                btnClickTimer = 0;
                downState = true;
            });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerUp,
                callback = new EventTrigger.TriggerEvent()
            };
            pointerUp.callback.AddListener(delegate {
                downState = false;
                var clickDeltaTime = btnClickTimer;
                btnClickTimer = 0;
                if (clickDeltaTime <= 1f || sceneName==KK_FBIOpenUp.SceneName.MainTitle) {
                    KK_FBIOpenUp.ToggleEnabled();
                    ChangeRedBagBtn(true, sceneName);
                } else {
                    DrawSlidePic(10, sceneName);
                }
            });
            trigger.triggers.Add(pointerUp);

            ChangeRedBagBtn(false,sceneName);
        }

        public static float smoothTime = 0.5f;
        private static Vector3 velocity = Vector3.zero;
        private static Image image;
        private static Vector3 targetPosition = Vector3.zero;
        /// <summary>
        /// 繪製轉場圖片
        /// </summary>
        /// <param name="_step">繪製完後要進入的腳本位置</param>
        /// <param name="sceneName">Scene名稱</param>
        private static void DrawSlidePic(int _step, KK_FBIOpenUp.SceneName sceneName) {
            GameObject parent;
            switch (sceneName) {
                case KK_FBIOpenUp.SceneName.Studio:
                    parent = GameObject.Find("StudioScene/Canvas Main Menu");
                    break;
                case KK_FBIOpenUp.SceneName.MainTitle:
                    parent = GameObject.Find("TitleScene/Canvas/Panel");
                    break;
                default:
                    return;
            }
            GameObject gameObject = new GameObject();
            gameObject.transform.SetParent(parent.transform, false);
            if (null == image) {
                switch (_step) {
                    case 1:
                        image = UIUtility.CreateImage("", gameObject.transform, Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.saikodaze.jpg", 800, 657));
                        image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f * 800 / 657, Screen.height / 1.5f);
                        break;
                    case 10:
                        image = UIUtility.CreateImage("", gameObject.transform, Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.beam.png", 700, 700));
                        image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.25f, Screen.height / 1.25f);
                        break;
                }
            }
            image.transform.position = new Vector3(Screen.width + image.sprite.rect.width / 2, Screen.height / 2);
            targetPosition = new Vector3(Screen.width / 2, Screen.height / 2);
            step = _step;

            Logger.Log(LogLevel.Info, "[KK_FBIOU] Draw Pic Finish");
        }

        private static float intensityBackup = 1f;
        private static float intensityTo = 5f;
        private static bool intensityState = false;
        private static CameraLightCtrl.LightInfo lightInfo;
        private static object lightCalc;
        /// <summary>
        /// 調整角色燈光，製造爆亮轉場
        /// </summary>
        /// <param name="goLighter">True轉亮；False轉暗</param>
        private static void ToggleCharaLight(bool goLighter) {
            if (null == lightInfo || null == lightCalc) {
                lightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");
                lightInfo = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
            }
            if (goLighter) {
                intensityBackup = lightInfo.intensity;
                intensityTo = 5f;
                intensityState = true;
            } else {
                intensityTo = intensityBackup;
                intensityBackup = lightInfo.intensity;
                intensityState = true;
            }
        }

        private static int step = 0;
        private static int reflectCount = 0;
        internal static void Update() {
            //過場圖片腳本邏輯
            if (null != image) {
                image.transform.position = Vector3.SmoothDamp(image.transform.position, targetPosition, ref velocity, smoothTime);
                //Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Velocity:{velocity} ; Image.position:{image.transform.position}");
                if ((image.transform.position - targetPosition).sqrMagnitude < 1f) {
                    Logger.Log(LogLevel.Debug, $"[KK_FBIOU] At Step: {step}");
                    if (intensityState && null != lightInfo && reflectCount < 60) {
                        lightInfo.intensity += (intensityTo - intensityBackup) / 60;
                        lightCalc.Invoke("Reflect");
                        reflectCount++;
                    } else {
                        switch (step) {
                            case 1:
                                //由中間移動到左邊
                                targetPosition = new Vector3(0 - (image.sprite.rect.width / 2), Screen.height / 2);
                                stepAdd();
                                break;
                            case 2:
                                //消滅圖片
                                GameObject.Destroy(image.transform.parent.gameObject);
                                stepSet(0);
                                break;
                            case 10:
                                //加亮角色光
                                reflectCount = 0;
                                ToggleCharaLight(true);
                                stepAdd();
                                break;
                            case 11:
                                intensityState = false;
                                ChangeAllCharacters();
                                reflectCount = 0;
                                ToggleCharaLight(false);
                                stepAdd();
                                break;
                            case 12:
                                intensityState = false;
                                stepSet(1);
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
