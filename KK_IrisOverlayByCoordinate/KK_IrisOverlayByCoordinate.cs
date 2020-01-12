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
using KKAPI;
using KKAPI.Chara;
using KoiSkinOverlayX;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace KK_IrisOverlayByCoordinate {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("KSOX", "5.1")]
    [BepInDependency("KSOX_GUI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("marco.kkapi", "1.7")]
    class KK_IrisOverlayByCoordinate : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Iris Overlay By Coordinate";
        internal const string GUID = "com.jim60105.kk.irisoverlaybycoordinate";
        internal const string PLUGIN_VERSION = "20.01.13.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.0";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            CharacterApi.RegisterExtraBehaviour<IrisOverlayByCoordinateController>(GUID);
        }
    }

    internal class IrisOverlayByCoordinateController : CharaCustomFunctionController {
        private static readonly ManualLogSource Logger = KK_IrisOverlayByCoordinate.Logger;
        private KoiSkinOverlayController KSOXController;
        private KoiSkinOverlayGui KoiSkinOverlayGui;
        protected override void Start() {
            KSOXController = CharacterApi.GetRegisteredBehaviour(typeof(KoiSkinOverlayController)).Instances.First() as KoiSkinOverlayController;
            KoiSkinOverlayGui = Extension.Extension.TryGetPluginInstance("KSOX_GUI") as KoiSkinOverlayGui;
            BackCoordinateType = CurrentCoordinate.Value;

            //切換服裝
            CurrentCoordinate.Subscribe(onNext: delegate { ChangeCoordinate(); });
            OnReload(KoikatuAPI.GetCurrentGameMode());
        }

        //儲存服裝的瞳孔資料
        public readonly Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> Overlays = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
        public Dictionary<TexType, byte[]> CurrentIrisOverlay {
            get {
                if (!Overlays.TryGetValue(CurrentCoordinate.Value, out _)) {
                    CurrentIrisOverlay = GetIrisOverlayLoaded();
                }
                return Overlays[CurrentCoordinate.Value];
            }
            private set {
                Overlays[CurrentCoordinate.Value] = value;
            }
        }
        private ChaFileDefine.CoordinateType BackCoordinateType;

        protected override void OnCardBeingSaved(GameMode currentGameMode) {
            ChangeCoordinate();
            //保存此插件資料
            PluginData pd = new PluginData();
            pd.data.Add("AllIrisOverlays", Overlays);

            SetExtendedData(pd);
        }

        protected override void OnReload(GameMode currentGameMode) {
            PluginData data = GetExtendedData();
            Overlays.Clear();
            if (data != null && data.data.TryGetValue("AllIrisOverlays", out object tmpOverlays) && tmpOverlays != null) {
                foreach (KeyValuePair<ChaFileDefine.CoordinateType, object> kvp in tmpOverlays.ToDictionary<ChaFileDefine.CoordinateType, object>()) {
                    Overlays.Add(kvp.Key, kvp.Value.ToDictionary<TexType, byte[]>());
                }
            } else {
                Logger.LogWarning("No Exist Data found from Chara. Use now iris overlay as default.");
                foreach (ChaFileDefine.CoordinateType type in Enum.GetValues(typeof(ChaFileDefine.CoordinateType))) {
                    Overlays.Add(type, GetIrisOverlayLoaded());
                }
            }
            OverWriteIrisOverlay();
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate) {
            ChangeCoordinate();
            PluginData pd = new PluginData();
            pd.data.Add("IrisOverlay", CurrentIrisOverlay);

            SetCoordinateExtendedData(coordinate, pd);
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate) {
            PluginData data = GetCoordinateExtendedData(coordinate);
            if (data != null && data.data.TryGetValue("IrisOverlay", out object tmpOverlay) && tmpOverlay != null) {
                CurrentIrisOverlay = tmpOverlay.ToDictionary<TexType, byte[]>();
            } else {
                Logger.LogWarning("No Exist Data found from Coordinate. Use now iris overlay as default.");
                CurrentIrisOverlay = GetIrisOverlayLoaded();
            }
            OverWriteIrisOverlay();
        }

        //取得現在載入的Iris Overlay
        public Dictionary<TexType, byte[]> GetIrisOverlayLoaded() {
            Dictionary<TexType, byte[]> result = new Dictionary<TexType, byte[]>();
            Dictionary<TexType, OverlayTexture> ori = KSOXController.Overlays.ToDictionary<TexType, OverlayTexture>();
            result.Add(TexType.EyeOver, ori.ContainsKey(TexType.EyeOver) ? ori[TexType.EyeOver].Data : null);
            result.Add(TexType.EyeUnder, ori.ContainsKey(TexType.EyeUnder) ? ori[TexType.EyeUnder].Data : null);

            return result;
        }

        private void ChangeCoordinate() {
            if (!KSOXController.Started) {
                return;
            }

            Overlays[BackCoordinateType] = GetIrisOverlayLoaded();
            if (BackCoordinateType != CurrentCoordinate.Value) {
                OverWriteIrisOverlay();
                BackCoordinateType = CurrentCoordinate.Value;
            }
        }


        //複寫Iris Overlay
        public void OverWriteIrisOverlay() {
            KSOXController.SetOverlayTex(CurrentIrisOverlay[TexType.EyeOver], TexType.EyeOver);
            KSOXController.SetOverlayTex(CurrentIrisOverlay[TexType.EyeUnder], TexType.EyeUnder);
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Maker && null != KoiSkinOverlayGui) {
                KoiSkinOverlayGui.Invoke("UpdateInterface", new object[] { KSOXController });
            }

            Logger.LogDebug("OverWrite Iris");
        }
    }
}
