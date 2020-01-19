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
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Extension;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KoiSkinOverlayX;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace KK_CharaOverlaysBasedOnCoordinate {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("KSOX", "5.1.1")]
    //去掉這條就可以在KSOX_GUI之前載入，且基本上不會有「有KSOX卻沒有KSOX_GUI」的情況發生，這裡可以略過依賴
    //[BepInDependency("KSOX_GUI", BepInDependency.DependencyFlags.SoftDependency)] 
    [BepInDependency("marco.kkapi", "1.9.5")]
    [BepInIncompatibility("com.jim60105.kk.irisoverlaybycoordinate")]
    class KK_CharaOverlaysBasedOnCoordinate : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Chara Overlays Based On Coordinate";
        internal const string GUID = "com.jim60105.kk.charaoverlaysbasedoncoordinate";
        internal const string PLUGIN_VERSION = "20.01.20.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.0";

        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> Enable_Saving_To_Chara { get; private set; }
        public static ConfigEntry<bool> Enable_Saving_To_Coordinate { get; private set; }
        public static ConfigEntry<bool> Save_Eyes_Overlay { get; private set; }
        public static ConfigEntry<bool> Save_Face_Overlay { get; private set; }
        public static ConfigEntry<bool> Save_Body_Overlay { get; private set; }
        public static ConfigEntry<bool> Warning_Message { get; private set; }

        public void Awake() {
            Logger = base.Logger;
            Enable_Saving_To_Chara = Config.Bind<bool>("Main Config", "Saving to character outfits (7 outfits)", true);
            Enable_Saving_To_Coordinate = Config.Bind<bool>("Main Config", "Saving to Coordinate files", false, "[Warning] It is highly recommended to enable this ONLY WHEN NEEDED");
            Warning_Message = Config.Bind<bool>("Main Config", "Warning Message", true, "Enable/Disable warning message when saving files");
            Save_Eyes_Overlay = Config.Bind<bool>("Save By Coordinate (Enable Main Config first)", "Eyes Overlay", true, "This setting will only react when main config enabled");
            Save_Face_Overlay = Config.Bind<bool>("Save By Coordinate (Enable Main Config first)", "Face Overlay", false, "This setting will only react when main config enabled");
            Save_Body_Overlay = Config.Bind<bool>("Save By Coordinate (Enable Main Config first)", "Body Overlay", false, "This setting will only react when main config enabled");
            CharacterApi.RegisterExtraBehaviour<CharaOverlaysBasedOnCoordinateController>(GUID);
        }

        public void Start() {
            //Maker UI
            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
        }

        #region View
        private void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e) {
            KK_CharaOverlaysBasedOnCoordinate owner = GetComponent<KK_CharaOverlaysBasedOnCoordinate>();
            MakerCoordinateLoadToggle loadToggle = e.AddCoordinateLoadToggle(new MakerCoordinateLoadToggle("Skin/eye overlays"));
            loadToggle.ValueChanged.Subscribe(newValue => {
                CharacterApi.GetRegisteredBehaviour(GUID).MaintainState = !newValue;
                //Logger.LogDebug("LoadCoordinate Toggled!");
            });

            SetupEyeInterface(e, owner);
            SetupBodyInterface(e, owner);
        }

        private void SetupEyeInterface(RegisterSubCategoriesEvent e, KK_CharaOverlaysBasedOnCoordinate owner) {
            MakerCategory irisCategory = MakerConstants.Face.Iris;
            MakerCategory eyeCategory = new MakerCategory(irisCategory.CategoryName, "tglEyeOverlayKSOX", irisCategory.Position + 5, "Iris Overlays");
            e.AddSubCategory(eyeCategory);

            e.AddControl(new MakerButton("Copy iris overlay to all outfits", eyeCategory, owner))
                .OnClick.AddListener(delegate () {
                    CharaOverlaysBasedOnCoordinateController controller = MakerAPI.GetCharacterControl().gameObject.GetComponent<CharaOverlaysBasedOnCoordinateController>();
                    controller.CopyOverlayToAllOutfits(TexType.EyeOver);
                    controller.CopyOverlayToAllOutfits(TexType.EyeUnder);
                    Logger.LogMessage("[Chara Overlays Based On Coordinate] Successfully copied iris overlay");
                }
            );
        }

        private void SetupBodyInterface(RegisterSubCategoriesEvent e, KK_CharaOverlaysBasedOnCoordinate owner) {
            MakerCategory paintCategory = MakerConstants.Body.Paint;
            MakerCategory makerCategory = new MakerCategory(paintCategory.CategoryName, "tglOverlayKSOX", paintCategory.Position + 5, "Skin Overlays");
            e.AddSubCategory(makerCategory);

            e.AddControl(new MakerButton("Copy face overlay to all outfits", makerCategory, owner))
                .OnClick.AddListener(delegate () {
                    CharaOverlaysBasedOnCoordinateController controller = MakerAPI.GetCharacterControl().gameObject.GetComponent<CharaOverlaysBasedOnCoordinateController>();
                    controller.CopyOverlayToAllOutfits(TexType.FaceOver);
                    controller.CopyOverlayToAllOutfits(TexType.FaceUnder);
                    Logger.LogMessage("[Chara Overlays Based On Coordinate] Successfully copied face overlay");
                }
            );
            e.AddControl(new MakerButton("Copy body overlay to all outfits", makerCategory, owner))
                .OnClick.AddListener(delegate () {
                    CharaOverlaysBasedOnCoordinateController controller = MakerAPI.GetCharacterControl().gameObject.GetComponent<CharaOverlaysBasedOnCoordinateController>();
                    controller.CopyOverlayToAllOutfits(TexType.BodyOver);
                    controller.CopyOverlayToAllOutfits(TexType.BodyUnder);
                    Logger.LogMessage("[Chara Overlays Based On Coordinate] Successfully copied body overlay");
                }
            );
        }
        #endregion
    }

    internal class CharaOverlaysBasedOnCoordinateController : CharaCustomFunctionController {
        private static readonly ManualLogSource Logger = KK_CharaOverlaysBasedOnCoordinate.Logger;
        private KoiSkinOverlayController KSOXController;
        private KoiSkinOverlayGui KoiSkinOverlayGui;
        private ChaFileDefine.CoordinateType BackCoordinateType;
        public readonly Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> Overlays = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
        public Dictionary<TexType, byte[]> CurrentOverlay {
            get {
                if (!Overlays.TryGetValue(CurrentCoordinate.Value, out _)) {
                    CurrentOverlay = GetOverlayLoaded();
                    //CurrentOverlay = null;
                }
                return Overlays[CurrentCoordinate.Value];
            }
            private set {
                Overlays[CurrentCoordinate.Value] = value;
            }
        }

        protected override void Start() {
            KoiSkinOverlayGui = Extension.Extension.TryGetPluginInstance("KSOX_GUI") as KoiSkinOverlayGui;
            CurrentCoordinate.Subscribe(onNext: delegate { ChangeCoordinate(); });

            if (KKAPI.Studio.StudioAPI.StudioLoaded) {
                KKAPI.Studio.SaveLoad.StudioSaveLoadApi.SceneSave += new EventHandler(delegate (object sender, EventArgs e) {
                    OnCardBeingSaved(GameMode.Studio);
                });
            }

            base.Start();
        }

        #region SaveLoad
        protected override void OnCardBeingSaved(GameMode currentGameMode) {
            //ChangeCoordinate();
            if (currentGameMode == GameMode.Maker && !CheckEnableSaving(true)) {
                return;
            }

            Overlays[BackCoordinateType] = GetOverlayLoaded();
            FillAllEmptyOutfits();

            //保存此插件資料
            PluginData pd = new PluginData();
            pd.data.Add("AllCharaOverlays", PrepareAllOverlaysToSave());
            pd.version = 1;

            SetExtendedData(pd);
            Logger.LogDebug("Saved Chara data");
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState) {
            KSOXController = CharacterApi.GetRegisteredBehaviour(typeof(KoiSkinOverlayController)).Instances.Where(x => x.ChaControl == base.ChaControl).Single() as KoiSkinOverlayController;
            BackCoordinateType = CurrentCoordinate.Value;
            //沒有打勾就不載入
            if (!CheckLoad(true) || maintainState) return;

            CheckForOldGUID((ChaFile)ChaFileControl);
            PluginData data = GetExtendedData(true);
            ReadPluginData(data);
            Logger.LogDebug($"{ChaFileControl.parameter.fullname} Load Extended Data");
            OverwriteOverlay(true);
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate) {
            if (!CheckEnableSaving(false)) {
                return;
            }
            PluginData pd = new PluginData();
            pd.data.Add("CharaOverlay", PrepareOverlayToSave(GetOverlayLoaded()));
            pd.version = 1;

            SetCoordinateExtendedData(coordinate, pd);
            Logger.LogDebug("Save Coordinate data");
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState) {
            if (!CheckLoad(false) || maintainState) return;


            PluginData data = GetCoordinateExtendedData((ChaFileCoordinate)CheckForOldGUID(coordinate));
            if (data != null && (data.data.TryGetValue("CharaOverlay", out object tmpOverlay) || data.data.TryGetValue("IrisOverlay", out tmpOverlay)) && tmpOverlay != null) {
                CurrentOverlay = tmpOverlay.ToDictionary<TexType, byte[]>();

                foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                    if (!CurrentOverlay.TryGetValue(type, out _)) {
                        CurrentOverlay[type] = null;
                    }
                }
                OverwriteOverlay();

                OnCardBeingSaved(KoikatuAPI.GetCurrentGameMode());
            } else {
                Logger.LogWarning("No Exist Data found from Coordinate.");
                //CurrentOverlay = null;
            }
        }
        #endregion

        #region Model
        //取得現在載入的Iris Overlay
        public Dictionary<TexType, byte[]> GetOverlayLoaded() {
            Dictionary<TexType, byte[]> result = new Dictionary<TexType, byte[]>();
            Dictionary<TexType, OverlayTexture> ori = KSOXController.Overlays.ToDictionary<TexType, OverlayTexture>();
            if (null != ori) {
                foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                    result.Add(type, ori.TryGetValue(type, out OverlayTexture texture) ? texture.Data : null);
                }
            } else {
                result = null;
            }
            return result;
        }

        public Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> PrepareAllOverlaysToSave() {
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                return Overlays;
            }

            Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> result = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
            foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> coordKVP in Overlays) {
                Dictionary<TexType, byte[]> tmpCoord = PrepareOverlayToSave(coordKVP.Value);
                result.Add(coordKVP.Key, tmpCoord);
            }
            return result;
        }

        public Dictionary<TexType, byte[]> PrepareOverlayToSave(Dictionary<TexType, byte[]> coordinateData) {
            Dictionary<TexType, byte[]> result = new Dictionary<TexType, byte[]>();
            Dictionary<TexType, byte[]> overlayLoaded = GetOverlayLoaded();
            try {
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Eyes_Overlay.Value, TexType.EyeOver, TexType.EyeUnder);
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Face_Overlay.Value, TexType.FaceOver, TexType.FaceUnder);
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Body_Overlay.Value, TexType.BodyOver, TexType.BodyUnder);
            } catch (KeyNotFoundException) {
                FillAllEmptyOutfits();
                return PrepareOverlayToSave(coordinateData);
            }
            return result;

            void doMain(bool b, TexType t1, TexType t2) {
                if (b) {
                    result.Add(t1, coordinateData[t1]);
                    result.Add(t2, coordinateData[t2]);
                } else {
                    result.Add(t1, overlayLoaded[t1]);
                    result.Add(t2, overlayLoaded[t2]);
                }
            }
        }

        //切換服裝
        private void ChangeCoordinate() {
            Logger.LogDebug("Change Overlay");
            if (null == KSOXController || !KSOXController.Started) {
                return;
            }

            if (BackCoordinateType != CurrentCoordinate.Value) {
                Overlays[BackCoordinateType] = GetOverlayLoaded();
                OverwriteOverlay(true);
                BackCoordinateType = CurrentCoordinate.Value;
            }
        }

        //複寫Overlay
        public void OverwriteOverlay(bool force = false) {
            if (null == CurrentOverlay && !force) {
                Logger.LogDebug("Skip OverWrite Overlay");
                return;
            }
            foreach (TexType texType in Enum.GetValues(typeof(TexType))) {
                if (null != CurrentOverlay && CurrentOverlay.TryGetValue(texType, out byte[] texture)) {
                    KSOXController.SetOverlayTex(texture, texType);
                } else {
                    KSOXController.SetOverlayTex(null, texType);
                }
            }
            if (MakerAPI.InsideAndLoaded && null != KoiSkinOverlayGui) {
                KoiSkinOverlayGui.Invoke("UpdateInterface", new object[] { KSOXController });
            }

            Logger.LogDebug("OverWrite Overlay");
        }

        private bool CheckEnableSaving(bool charaEntry) {
            if (charaEntry) {
                if (Overlays.Where(x => x.Key != CurrentCoordinate.Value).Select(x => x.Value).Where(delegate (Dictionary<TexType, byte[]> x) {
                    bool flag2 = false;
                    if (!KK_CharaOverlaysBasedOnCoordinate.Save_Eyes_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value) {
                        flag2 = null != x[TexType.EyeOver] || null != x[TexType.EyeUnder];
                    }
                    if (!flag2 && (!KK_CharaOverlaysBasedOnCoordinate.Save_Face_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value)) {
                        flag2 = null != x[TexType.FaceOver] || null != x[TexType.FaceUnder];
                    }
                    if (!flag2 && (!KK_CharaOverlaysBasedOnCoordinate.Save_Body_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value)) {
                        flag2 = null != x[TexType.BodyOver] || null != x[TexType.BodyUnder];
                    }

                    return flag2;
                }).Any()) {
                    Logger.LogInfo("There are overlays on other outfits but the saving setting is not enabled.");
                    ShowMessage("[WARNING] Chara overlays on other outfits are not saved to Chara files.");
                }
                return KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value;
            } else {
                if (KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Coordinate.Value) {
                    if (!GetOverlayLoaded().Values.Where(x => null != x).Any()) {
                        Logger.LogInfo("Chara overlay save to coordinate file is enable but no overlay loaded.");
                        ShowMessage("[WARNING] Chara overlay is saving NOTHING to the Coordinate file.");
                        ShowMessage("[WARNING] This will cause the chara overlay to BE CLEARED when loading this coordinate file.");
                    }
                }
                return KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Coordinate.Value;
            }

            void ShowMessage(string message) {
                if (KK_CharaOverlaysBasedOnCoordinate.Warning_Message.Value) {
                    Logger.LogMessage(message);
                } else {
                    Logger.LogWarning(message);
                }
            }
        }

        private bool CheckLoad(bool charaEntry) {
            if (charaEntry) {
                return !CharacterApi.GetRegisteredBehaviour("KSOX").MaintainState;
            } else {
                return !CharacterApi.GetRegisteredBehaviour(ExtendedDataId).MaintainState;
            }
        }

        public void CopyOverlayToAllOutfits(TexType type) {
            foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> coordKVP in Overlays) {
                coordKVP.Value[type] = CurrentOverlay[type];
            }
        }

        public void ReadPluginData(PluginData data) {
            Overlays.Clear();
            if (data != null && (data.data.TryGetValue("AllCharaOverlays", out object tmpOverlays) || data.data.TryGetValue("AllIrisOverlays", out tmpOverlays)) && tmpOverlays != null) {
                foreach (KeyValuePair<ChaFileDefine.CoordinateType, object> kvp in tmpOverlays.ToDictionary<ChaFileDefine.CoordinateType, object>()) {
                    Overlays.Add(kvp.Key, kvp.Value.ToDictionary<TexType, byte[]>());
                }
                KSOXController.Invoke("OnReload", new object[] { KoikatuAPI.GetCurrentGameMode(), false });
            } else {
                Logger.LogWarning("No PluginData Existed");
            }
            FillAllEmptyOutfits();
        }

        public Dictionary<TexType, byte[]> FillAllEmptyOutfits(bool fillWithNull = false) {
            Dictionary<TexType, byte[]> fillIn = GetOverlayLoaded();
            if (fillWithNull || null == fillIn) {
                fillIn = new Dictionary<TexType, byte[]>();
                foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                    fillIn[type] = null;
                }
            }

            foreach (ChaFileDefine.CoordinateType type in Enum.GetValues(typeof(ChaFileDefine.CoordinateType))) {
                if (!Overlays.TryGetValue(type, out Dictionary<TexType, byte[]> o) || null == o) {
                    Overlays[type] = fillIn;
                } else {
                    foreach (TexType type2 in Enum.GetValues(typeof(TexType))) {
                        if (!o.ContainsKey(type2)) {
                            o[type2] = fillIn[type2];
                        }
                    }
                }
            }
            return fillIn;
        }

        private object CheckForOldGUID(object input) {
            if (input is ChaFile chaFile) {
                PluginData data = ExtendedSave.GetExtendedDataById(chaFile, "com.jim60105.kk.irisoverlaybycoordinate");
                if (null != data) {
                    ExtendedSave.SetExtendedDataById(chaFile, "com.jim60105.kk.irisoverlaybycoordinate", null);
                    SetExtendedData(data);
                }
                return chaFile;
            } else if (input is ChaFileCoordinate coordinate) {
                PluginData data = ExtendedSave.GetExtendedDataById(coordinate, "com.jim60105.kk.irisoverlaybycoordinate");
                if (null != data) {
                    ExtendedSave.SetExtendedDataById(coordinate, "com.jim60105.kk.irisoverlaybycoordinate", null);
                    SetCoordinateExtendedData(coordinate, data);
                }
                return coordinate;
            }
            return null;
        }
        #endregion
    }
}
