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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_FBIOpenUp {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_FBIOpenUp : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "FBI Open Up";
        internal const string GUID = "com.jim60105.kk.fbiopenup";
        internal const string PLUGIN_VERSION = "19.07.06.1";

        internal static bool isenabled = false;
        public void Awake() {
            UIUtility.Init();
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
            //讀取config
            BepInEx.Config.ReloadConfig();
            isenabled = String.Equals(BepInEx.Config.GetEntry("enabled", "False", PLUGIN_NAME), "True");
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

        public void Update() => Patches.Update();

        internal static void ToggleEnabled() {
            isenabled = !isenabled;
            BepInEx.Config.SetEntry("enabled", isenabled ? "True" : "False", PLUGIN_NAME);
            BepInEx.Config.ReloadConfig();
        }
    }

    class Patches {
        private static List<float> sampleShapeValueFace;
        private static List<float> sampleShapeValueBody;
        private static List<float> originalShapeValueFace;
        private static List<float> originalShapeValueBody;
        private static List<float> result;
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
            isIniting = true;
            ChaFile chaFile = new ChaFile();
            chaFile.Invoke("LoadFile", new object[] { stream, true, true });
            Logger.Log(LogLevel.Debug, "[KK_FBIOU] Loaded sample chara: " + chaFile.parameter.fullname);
            isIniting = false;
            var face = MessagePackSerializer.Deserialize<ChaFileFace>(MessagePackSerializer.Serialize<ChaFileFace>(chaFile.custom.face));
            var body = MessagePackSerializer.Deserialize<ChaFileBody>(MessagePackSerializer.Serialize<ChaFileBody>(chaFile.custom.body));
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Face: " + face.shapeValueFace.Length);
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Body: " + body.shapeValueBody.Length);
            sampleShapeValueFace = face.shapeValueFace.ToList();
            sampleShapeValueBody = body.shapeValueBody.ToList();
        }

        /// <summary>
        /// 替換過的chara之原始數據 Dict(ChaFileCustom, List[]{shapeValueFace.toList, shapeValueBody.toList})
        /// </summary>
        private static Dictionary<ChaFileCustom, List<float>[]> chaFileCustomDict = new Dictionary<ChaFileCustom, List<float>[]>();

        /// <summary>
        /// 替換角色
        /// </summary>
        /// <param name="chaFileCustom">目標chara的身體數據</param>
        /// <param name="changeFace">是否替換臉部</param>
        /// <param name="changeBody">是否替換身體</param>
        public static void ChangeChara(ChaFileCustom chaFileCustom, bool changeFace = true, bool changeBody = true, bool forceChange = true) {
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Face: " + chaFileCustom.face.shapeValueFace.Length);
            //Logger.Log(LogLevel.Message, "[KK_FBIOU] Length Body: " + chaFileCustom.body.shapeValueBody.Length);
            originalShapeValueFace = chaFileCustom.face.shapeValueFace.ToList();
            originalShapeValueBody = chaFileCustom.body.shapeValueBody.ToList();

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

            if (null != sampleShapeValueBody && changeBody) {
                if (originalShapeValueBody.Count == sampleShapeValueBody.Count) {
                    result = new List<float>();
                    for (int i = 0; i < originalShapeValueBody.Count; i++) {
                        result.Add(sampleShapeValueBody[i] + ((originalShapeValueBody[i] - sampleShapeValueBody[i]) * keepRate));
                    }
                    chaFileCustom.body.shapeValueBody = result.ToArray();
                } else { Logger.Log(LogLevel.Error, "[KK_FBIOU] Sample data is not match to target data!"); }
                Logger.Log(LogLevel.Debug, "[KK_FBIOU] Changed body finish");
            }
        }

        /// <summary>
        /// 將所有角色做替換
        /// </summary>
        public static void ChangeAllCharacters() {
            List<OCIChar> charList = Studio.Studio.Instance.dicInfo.Values.OfType<Studio.OCIChar>().ToList();
            charList.ForEach(new Action<OCIChar>(delegate (OCIChar ocichar) {
                ChangeChara(ocichar.charInfo.chaFile.custom, true, true, false);
                ocichar.charInfo.Reload(true, false, true, false);
                Logger.Log(LogLevel.Debug, $"[KK_FBIOU] Changed {ocichar.charInfo.name}");
            }));
        }

        #region Hooks
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            if (String.Equals(__instance.name, "00_Female")) {
                DrawRedBagBtn(__instance);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCustom), "LoadBytes")]
        public static void LoadBytesPostfix(ChaFileCustom __instance) {
            if (KK_FBIOpenUp.isenabled && !isIniting) {
                ChangeChara(__instance);
            }
        }

        private static bool isIniting = false;
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPrefix(CharaList __instance) {
            isIniting = true;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPostfix(CharaList __instance) {
            isIniting = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo), "Load", new Type[] { typeof(string) })]
        public static void LoadPrefix(CharaList __instance) {
            isIniting = true;
            chaFileCustomDict.Clear();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Load", new Type[] { typeof(string) })]
        public static void LoadPostfix(CharaList __instance) {
            isIniting = false;
        }

        #endregion Hooks

        #region Unity Stuff
        /// <summary>
        /// 切換紅色書包圖標顯示
        /// </summary>
        /// <param name="showPic">是否顯示過場圖片</param>
        private static void ChangeRedBagBtn(bool showPic = true) {
            if (KK_FBIOpenUp.isenabled) {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                if (showPic) {
                    DrawSlidePic(1);
                }
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Enable Plugin");
            } else {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
                Logger.Log(LogLevel.Info, "[KK_FBIOU] Disable Plugin");
            }
        }

        private static float btnClickTimer = 0;
        private static bool downState = false;
        private static void DrawRedBagBtn(CharaList __instance) {
            var original = GameObject.Find($"StudioScene/Canvas Main Menu/01_Add/{__instance.name}/Button Change");
            redBagBtn = UnityEngine.Object.Instantiate(original, original.transform.parent.transform);
            redBagBtn.name = "redBagBtn";
            redBagBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(-120, -270), new Vector2(-40, -190));
            redBagBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_FBIOpenUp.Resources.redBag.png", 100, 100);
            redBagBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            redBagBtn.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            redBagBtn.GetComponent<Button>().interactable = true;

            //因為要handle長按，改為監聽PointerDown、PointerUp Event
            redBagBtn.AddComponent<EventTrigger>();
            EventTrigger trigger = redBagBtn.gameObject.GetComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback = new EventTrigger.TriggerEvent();
            pointerDown.callback.AddListener(delegate {
                btnClickTimer = 0;
                downState = true;
            });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback = new EventTrigger.TriggerEvent();
            pointerUp.callback.AddListener(delegate {
                downState = false;
                var clickDeltaTime = btnClickTimer;
                btnClickTimer = 0;
                if (clickDeltaTime > 1f) {
                    DrawSlidePic(10);
                } else {
                    KK_FBIOpenUp.ToggleEnabled();
                    ChangeRedBagBtn();
                }
            });
            trigger.triggers.Add(pointerUp);

            ChangeRedBagBtn(false);
        }

        public static float smoothTime = 0.5f;
        private static Vector3 velocity = Vector3.zero;
        private static Image image;
        private static Vector3 targetPosition = Vector3.zero;
        /// <summary>
        /// 繪製轉場圖片
        /// </summary>
        /// <param name="_step">繪製完後要進入的腳本位置</param>
        private static void DrawSlidePic(int _step) {
            var parent = GameObject.Find("StudioScene/Canvas Main Menu");
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
