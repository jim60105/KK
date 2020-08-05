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
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioCharaLightLinkedToCamera {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    public class KK_StudioCharaLightLinkedToCamera : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Chara Light Linked To Camera";
        internal const string GUID = "com.jim60105.kk.studiocharalightlinkedtocamera";
        internal const string PLUGIN_VERSION = "20.08.05.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.7";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            Extension.Extension.LogPrefix = $"[{PLUGIN_NAME}]";
            Harmony harmonyInstance = Harmony.CreateAndPatchAll(typeof(Patches));

            harmonyInstance.Patch(
                typeof(Studio.CameraLightCtrl).GetNestedType("LightCalc", BindingFlags.NonPublic).GetMethod("OnValueChangeAxis", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnValueChangeAxisPostfix))
            );
        }
    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioCharaLightLinkedToCamera.Logger;
        public static bool Locked { get; private set; } = false;
        private static readonly object studioLightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");
        private static Studio.CameraLightCtrl.LightInfo chaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
        private static Studio.CameraControl cameraControl;
        private static Vector2 preCamEuler = Vector2.zero;

        #region View
        static internal GameObject LockBtn;

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraLightCtrl), "Init")]
        public static void InitPostfix() {
            if (null != GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Chara Light Lock Btn")) {
                return;
            }
            GameObject original = GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis X");
            GameObject parent = original.transform.parent.gameObject;

            LockBtn = UnityEngine.Object.Instantiate(original, parent.transform);
            LockBtn.name = "Chara Light Lock Btn";
            LockBtn.transform.localPosition = new Vector3(157.5f, -59f, 0);
            LockBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(157.5f, -82f), new Vector2(180.5f, -59));
            LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
            LockBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            LockBtn.GetComponent<Button>().interactable = true;

            LockBtn.GetComponent<Button>().onClick.AddListener(delegate { ToggleLocked(); });

            RegisterSaveEvent();
        }

        public static void ToggleLocked(bool? b = null) {
            cameraControl = Object.FindObjectsOfType<Studio.CameraControl>().OrderBy(m => m.transform.GetSiblingIndex()).Last();
            Logger.LogDebug($"Get CameraControl: {cameraControl.gameObject.name}");

            if (null == b) {
                Locked = !Locked;
            } else {
                Locked = (bool)b;
            }

            if (Locked) {
                LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock.png", 36, 36);

                //Set value for calc
                Vector3 ligLocal = (studioLightCalc.GetField("light") as Light).transform.localEulerAngles;
                ComputeAngle.SetAngle(
                    ligLocal,
                    new Vector2(chaLight.rot[0], chaLight.rot[1]),
                    cameraControl.cameraAngle
                );
            } else {
                LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
            }

            Logger.LogDebug("Locked status: " + Locked);
        }

        public static void OnValueChangeAxisPostfix() {
            if (Locked && !(bool)studioLightCalc.GetField("isUpdateInfo")) {
                ComputeAngle.AttachedLightEuler = new Vector2(chaLight.rot[0], chaLight.rot[1]);
                ComputeAngle.AttachedCameraEuler = new Vector2(cameraControl.cameraAngle.x, cameraControl.cameraAngle.y);
            }
        }
        #endregion

        #region SaveAndLoad
        public static void RegisterSaveEvent() {
            ExtendedSave.SceneBeingSaved += path => {
                ExtendedSave.SetSceneExtendedDataById(KK_StudioCharaLightLinkedToCamera.GUID, new PluginData() {
                    data = new System.Collections.Generic.Dictionary<string, object> {
                        { "locked", Locked ? "true" : "false" },
                        { "attachedLightAngle", new System.Collections.Generic.Dictionary<string,float>{
                            {"x", ComputeAngle.AttachedLightEuler.x },
                            {"y", ComputeAngle.AttachedLightEuler.y }
                        }},
                        { "attachedCameraAngle", new System.Collections.Generic.Dictionary<string,float>{
                            {"x", ComputeAngle.AttachedCameraEuler.x },
                            {"y", ComputeAngle.AttachedCameraEuler.y }
                        }}
                    },
                    version = 3
                });
                Logger.LogDebug("Scene Saved");
            };
            ExtendedSave.SceneBeingLoaded += path => {
                PluginData pd = ExtendedSave.GetSceneExtendedDataById(KK_StudioCharaLightLinkedToCamera.GUID);
                if (null != pd && pd.version == 3 &&
                    pd.data.TryGetValue("locked", out object l) && l is string boolstring &&
                    pd.data.TryGetValue("attachedLightAngle", out object lig) &&
                    pd.data.TryGetValue("attachedCameraAngle", out object cam)) {

                    System.Collections.Generic.Dictionary<string, float> ligAngle = lig.ToDictionary<string, float>();
                    System.Collections.Generic.Dictionary<string, float> camAngle = cam.ToDictionary<string, float>();

                    ToggleLocked(boolstring == "true");

                    ComputeAngle.AttachedLightEuler = new Vector2(ligAngle["x"], ligAngle["y"]);
                    ComputeAngle.AttachedCameraEuler = new Vector2(camAngle["x"], camAngle["y"]);

                    Logger.LogDebug("Scene Load PluginData");
                } else {
                    ToggleLocked(false);
                }
                chaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
            };
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix() {
            //LoadScene以前先解除Locked狀態
            ToggleLocked(false);
        }
        #endregion

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "CameraUpdate")]
        public static void CameraUpdatePostfix(Studio.CameraControl __instance) {
            if (!Locked || __instance != cameraControl) return;
            if (__instance.cameraAngle.x == preCamEuler.x && __instance.cameraAngle.y == preCamEuler.y) return;
            preCamEuler = __instance.cameraAngle;

            Vector2 vec = ComputeAngle.GetLightEuler(__instance.cameraAngle.x, __instance.cameraAngle.y);
            chaLight.rot[0] = vec.x;
            chaLight.rot[1] = vec.y;

            studioLightCalc.Invoke("UpdateUI");
            studioLightCalc.Invoke("Reflect");
        }
    }

    //wits-fe@github done these math jobs!
    class ComputeAngle {
        private static float alpha, beta, alphaR, betaR;
        private static float cosr, agls, sign;

        private static int situ;
        private static bool isInited;

        private static Vector2 lightLocalEuler;

        private static Vector2 preLigEuler;
        public static Vector2 AttachedLightEuler {
            get => preLigEuler;
            set {
                preLigEuler = value;
                isInited = false;
            }
        }

        private static Vector2 preCamEuler;
        public static Vector2 AttachedCameraEuler {
            get => preCamEuler;
            set {
                preCamEuler = value;
                isInited = false;
            }
        }

        public static void SetAngle(Vector2 lightLocalEuler, Vector2 lightEuler, Vector2 cameraEuler) {
            ComputeAngle.lightLocalEuler = lightLocalEuler;
            preLigEuler = lightEuler;
            preCamEuler = cameraEuler;

            // The direction preset of light must be in zy plane, where zy is z axis and y axis. 
            // Otherwise post-applied euler rotation with only x and y angle can't reach 
            // arbitrary direction of light.
            // Thus, when local euler preset of light with only x and y angle is taken into account,
            // the y angle must suffice that of y % 180 == 0.
            sign = lightLocalEuler.y % 360f == 0f ? 1f : (lightLocalEuler.y % 180f == 0f ? -1f : throw new System.Exception("Unsupported rotation preset of light."));
            situ = Init();
        }

        private static int Init() {
            isInited = true;

            float y = preLigEuler.y + lightLocalEuler.y - preCamEuler.y;
            alpha = (-180f <= y && y <= 180f) ? y : (y < 0 ? (y - 180f) % 360f + 180f : (y + 180f) % 360f - 180f);

            float x = lightLocalEuler.x + sign * preLigEuler.x;
            beta = (-90f <= x && x <= 90f) ? x : (x < 0 ? (x - 90f) % 180f + 90f : (x + 90f) % 180f - 90f);

            if (alpha == 0f) return 1;
            if (alpha == 180f || alpha == -180f) return 2;
            if (beta == 90f || beta == -90f) return 3;

            alphaR = alpha * Mathf.Deg2Rad;
            betaR = beta * Mathf.Deg2Rad;

            float sc = Mathf.Sin(alphaR) * Mathf.Cos(betaR);
            cosr = Mathf.Sqrt(1f - sc * sc);
            if (alpha < -90f || alpha > 90f) cosr = -cosr;
            float bc = cosr == 0f ? 0f : Mathf.Sin(betaR) / cosr;
            agls = bc > 1f ? Mathf.Asin(1f) : (bc < -1f ? Mathf.Asin(-1f) : Mathf.Asin(bc));
            return 0;
        }

        private static Vector2 GetLightEulerOffset(float camDeltaX, float camDeltaY) {
            if (situ == 1) return new Vector2(camDeltaX, camDeltaY);
            if (situ == 2) return new Vector2(-camDeltaX, camDeltaY);
            if (situ == 3) {
                if (alpha > 90f) return new Vector2(-camDeltaX, camDeltaY + 180f - alpha);
                if (alpha < -90f) return new Vector2(-camDeltaX, camDeltaY - 180f - alpha);
                return new Vector2(camDeltaX, camDeltaY - alpha);
            }
            return Vector2.zero;
        }

        public static Vector2 GetLightEuler(float curCamEulerX, float curCamEulerY) {
            if (!isInited) situ = Init();

            float camDeltaX = curCamEulerX - preCamEuler.x;
            float camDeltaY = curCamEulerY - preCamEuler.y;
            if (camDeltaX == 0f && camDeltaY == 0f) return preLigEuler;

            if (situ > 0) {
                Vector2 vec = GetLightEulerOffset(camDeltaX, camDeltaY);
                return GetFixedAngle(preLigEuler.x + sign * vec.x, preLigEuler.y + vec.y);
            }

            float m = agls + camDeltaX * Mathf.Deg2Rad;
            float bx = Mathf.Asin(Mathf.Sin(m) * cosr);

            float cbx = Mathf.Cos(bx);
            float cmcx = cbx == 0f ? 1f : Mathf.Cos(m) * cosr / cbx;
            float ay = cmcx > 1f ? Mathf.Acos(1f) : (cmcx < -1f ? Mathf.Acos(-1f) : Mathf.Acos(cmcx));
            if (alpha < 0f) ay = -ay;

            float x = bx * Mathf.Rad2Deg - beta;
            float y = camDeltaY + ay * Mathf.Rad2Deg - alpha;

            return GetFixedAngle(preLigEuler.x + sign * x, preLigEuler.y + y);
        }

        private static Vector2 GetFixedAngle(float px, float py) {
            px %= 360;
            py %= 360;
            return new Vector2() {
                x = px >= 0 ? px : px + 360,
                y = py >= 0 ? py : py + 360
            };
        }
    }
}
