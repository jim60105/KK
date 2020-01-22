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
        internal const string PLUGIN_VERSION = "20.01.22.1";
        internal const string PLUGIN_RELEASE_VERSION = "1.2.0";

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

        private readonly List<byte[]> Resources = new List<byte[]>();
        private readonly Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> OverlayTable = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>>();

        /// <summary>
        /// 此插件內儲存的Overlay。Get method返回之Dictionary只能讀，寫入需以Set完整寫入
        /// </summary>
        public Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> Overlays {
            get {
                Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> overlays = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
                foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> kvp in OverlayTable) {
                    TryGetOverlayByCoordinateType(kvp.Key, out Dictionary<TexType, byte[]> overlay);
                    if (null == overlay) {
                        overlay = new Dictionary<TexType, byte[]>();
                        foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                            overlay[type] = null;
                        }
                    }
                    overlays[kvp.Key] = overlay;
                }
                return overlays;
            }
            internal set {
                OverlayTable.Clear();
                Resources.Clear();
                foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> kvp in value) {
                    SetOverlayByCoordinateType(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// 現在穿著的Overlay。Get method只能讀，寫入須以Set整個寫
        /// </summary>
        public Dictionary<TexType, byte[]> CurrentOverlay {
            get {
                Dictionary<TexType, byte[]> overlay;
                while (!TryGetOverlayByCoordinateType(CurrentCoordinate.Value, out overlay)) {
                    CurrentOverlay = GetOverlayLoaded();
                }
                return overlay;
            }
            private set {
                SetOverlayByCoordinateType(CurrentCoordinate.Value, value);
            }
        }

        public int InputOverlayTexture(byte[] b) {
            //永遠在最前面放一個空內容
            if (Resources.Count == 0) Resources.Add(new byte[] { });

            if (null == b || b.Length == 0) return 0;

            var tmp = Resources.Select((texture, index) => new { texture, index }).Where(x => x.texture.SequenceEqual(b));
            if (tmp.Any()) {
                return tmp.Single().index;
            } else {
                Resources.Add(b);
                return Resources.Count - 1;
            }
        }

        public void SetOverlayByCoordinateType(ChaFileDefine.CoordinateType type, Dictionary<TexType, byte[]> overlay) {
            OverlayTable[type] = new Dictionary<TexType, int>();
            foreach (KeyValuePair<TexType, byte[]> kvp in overlay) {
                OverlayTable[type][kvp.Key] = InputOverlayTexture(kvp.Value);
                Logger.LogDebug($"Input Texture: {type.ToString()}({kvp.Key.ToString()}): {OverlayTable[type][kvp.Key]}");
            }
        }

        public bool TryGetOverlayByCoordinateType(ChaFileDefine.CoordinateType type, out Dictionary<TexType, byte[]> result) {
            result = new Dictionary<TexType, byte[]>();
            if (OverlayTable.TryGetValue(type, out Dictionary<TexType, int> tmp)) {
                foreach (KeyValuePair<TexType, int> kvp in tmp) {
                    result.Add(kvp.Key, kvp.Value < Resources.Count ? Resources[kvp.Value] : null);
                }
            } else {
                result = null;
                return false;
            }
            return true;
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

            SetOverlayByCoordinateType(BackCoordinateType, GetOverlayLoaded());
            FillAllEmptyOutfits();

            RebuildOverlayTableAndResources();
            //保存此插件資料
            PluginData pd = new PluginData();
            pd.data.Add("AllCharaOverlayTable", PrepareOverlayTableToSave());
            pd.data.Add("AllCharaResources", MessagePack.MessagePackSerializer.Serialize(Resources));
            pd.version = 2;

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
        public void ReadPluginData(PluginData data) {
            if (data == null) {
                Logger.LogWarning("No PluginData Existed");
            } else {
                if ((!data.data.TryGetValue("AllCharaOverlayTable", out object tmpOverlayTable) || tmpOverlayTable == null) ||
                     (!data.data.TryGetValue("AllCharaResources", out object tmpResources) || null==tmpResources)) {
                    Logger.LogWarning("Wrong PluginData Existed");
                } else {
                    Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> overlays = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
                    List<byte[]> resourceList = MessagePack.MessagePackSerializer.Deserialize<List<byte[]>>((byte[])tmpResources);
                    foreach (KeyValuePair<ChaFileDefine.CoordinateType, object> kvp in tmpOverlayTable.ToDictionary<ChaFileDefine.CoordinateType, object>()) {
                        Dictionary<TexType, byte[]> coordinate = new Dictionary<TexType, byte[]>();
                        foreach (KeyValuePair<TexType, int> kvp2 in kvp.Value.ToDictionary<TexType, int>()) {
                            coordinate.Add(kvp2.Key, resourceList[kvp2.Value]);
                        }
                        overlays.Add(kvp.Key, coordinate);
                    }
                    Overlays = overlays;
                    KSOXController.Invoke("OnReload", new object[] { KoikatuAPI.GetCurrentGameMode(), false });
                }
            }
            FillAllEmptyOutfits();
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate) {
            if (!CheckEnableSaving(false)) {
                return;
            }
            SetOverlayByCoordinateType(BackCoordinateType, GetOverlayLoaded());

            PluginData pd = new PluginData();
            Dictionary<TexType, int> overlayTable = PrepareOverlayToSave(OverlayTable[CurrentCoordinate.Value], out int[] resourceUsed);
            List<byte[]> resources = new List<byte[]>();
            foreach (int i in resourceUsed) {
                resources.Add(Resources[i]);
            }
            Dictionary<TexType, int> resultTable = new Dictionary<TexType, int>();
            foreach (KeyValuePair<TexType, int> kvp in overlayTable) {
                resultTable[kvp.Key] = resourceUsed.ToList().IndexOf(kvp.Value);
            }

            pd.data.Add("CharaOverlayTable", resultTable);
            pd.data.Add("CharaResources", MessagePack.MessagePackSerializer.Serialize(resources));
            pd.version = 2;

            SetCoordinateExtendedData(coordinate, pd);
            Logger.LogDebug("Save Coordinate data");
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState) {
            if (!CheckLoad(false) || maintainState) return;

            PluginData data = GetCoordinateExtendedData((ChaFileCoordinate)CheckForOldGUID(coordinate));
            if ((!data.data.TryGetValue("CharaOverlayTable", out object tmpOverlayTable) || tmpOverlayTable == null) ||
                 (!data.data.TryGetValue("CharaResources", out object tmpResources) || null==tmpResources)) {
                Logger.LogWarning("No Exist Data found from Coordinate.");
            } else {
                Dictionary<TexType, byte[]> coordinateData = new Dictionary<TexType, byte[]>();
                List<byte[]> resourceList = MessagePack.MessagePackSerializer.Deserialize<List<byte[]>>((byte[])tmpResources);
                foreach (KeyValuePair<TexType, int> kvp in tmpOverlayTable.ToDictionary<TexType, int>()) {
                    coordinateData.Add(kvp.Key, resourceList[kvp.Value]);
                }

                CurrentOverlay = coordinateData;

                foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                    if (!OverlayTable[CurrentCoordinate.Value].TryGetValue(type, out _)) {
                        OverlayTable[CurrentCoordinate.Value][type] = 0;
                    }
                }
                OverwriteOverlay();

                //OnCardBeingSaved(KoikatuAPI.GetCurrentGameMode());
            }
        }
        #endregion

        #region Model
        /// <summary>
        /// 重新建構資源，捨棄沒有用到的資源，不要頻繁呼叫
        /// </summary>
        public void RebuildOverlayTableAndResources() => Overlays = Overlays;

        //取得現在載入的Overlay
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

        /// <summary>
        /// 依照儲存設定，返回整個整理好的OverlayTable
        /// </summary>
        public Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> PrepareOverlayTableToSave() {
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                return OverlayTable;
            }

            FillAllEmptyOutfits();
            Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> result = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>>();
            foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> coordKVP in OverlayTable) {
                Dictionary<TexType, int> tmpCoord = PrepareOverlayToSave(coordKVP.Value, out _);
                result.Add(coordKVP.Key, tmpCoord);
            }
            return result;
        }

        /// <summary>
        /// 依照儲存設定，返回整理好的OverlayTable
        /// </summary>
        /// <param name="coordinateData">要整理的Coordinate OverlayTable</param>
        /// <param name="resourceUsed">使用到的Resources序號</param>
        /// <returns></returns>
        public Dictionary<TexType, int> PrepareOverlayToSave(Dictionary<TexType, int> coordinateData, out int[] resourceUsed) {
            List<int> resourceUsedList = new List<int>();
            Dictionary<TexType, int> result = new Dictionary<TexType, int>();
            Dictionary<TexType, int> overlayLoaded = OverlayTable[CurrentCoordinate.Value];
            try {
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Eyes_Overlay.Value, TexType.EyeOver, TexType.EyeUnder);
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Face_Overlay.Value, TexType.FaceOver, TexType.FaceUnder);
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Body_Overlay.Value, TexType.BodyOver, TexType.BodyUnder);
                resourceUsed = resourceUsedList.ToArray();
            } catch (KeyNotFoundException) {
                FillAllEmptyOutfits();
                return PrepareOverlayToSave(coordinateData, out resourceUsed);
            }
            return result;

            void doMain(bool b, TexType t1, TexType t2) {
                if (b) {
                    doMain2(t1);
                    doMain2(t2);
                } else {
                    result.Add(t1, overlayLoaded[t1]);
                    result.Add(t2, overlayLoaded[t2]);
                }
            }
            void doMain2(TexType t) {
                result.Add(t, coordinateData[t]);
                if (!resourceUsedList.Where(x => x == coordinateData[t]).Any()) {
                    resourceUsedList.Add(coordinateData[t]);
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
                SetOverlayByCoordinateType(BackCoordinateType, GetOverlayLoaded());
                OverwriteOverlay(true);
                BackCoordinateType = CurrentCoordinate.Value;
            }
        }

        //複寫Overlay
        /// <summary>
        /// 往KSOX複寫Overlay
        /// </summary>
        /// <param name="force">略過OverlayTable outfit檢查，正常不應該發生被檢查擋住的狀況</param>
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

        /// <summary>
        /// 存檔前的警告檢查
        /// </summary>
        /// <param name="charaEntry">True為儲存角色，False為儲存衣裝</param>
        /// <returns>依照設定判斷，是否要執行存檔動作</returns>
        private bool CheckEnableSaving(bool charaEntry) {
            if (charaEntry) {
                if (OverlayTable.Where(x => x.Key != CurrentCoordinate.Value).Select(x => x.Value).Where(delegate (Dictionary<TexType, int> x) {
                    bool flag2 = false;
                    if (!KK_CharaOverlaysBasedOnCoordinate.Save_Eyes_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value) {
                        flag2 = 0 != x[TexType.EyeOver] || 0 != x[TexType.EyeUnder];
                    }
                    if (!flag2 && (!KK_CharaOverlaysBasedOnCoordinate.Save_Face_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value)) {
                        flag2 = 0 != x[TexType.FaceOver] || 0 != x[TexType.FaceUnder];
                    }
                    if (!flag2 && (!KK_CharaOverlaysBasedOnCoordinate.Save_Body_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value)) {
                        flag2 = 0 != x[TexType.BodyOver] || 0 != x[TexType.BodyUnder];
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

        /// <summary>
        /// CharaMaker讀檔時下方的勾選項檢查
        /// </summary>
        /// <param name="charaEntry"></param>
        /// <returns>是否要讀檔</returns>
        private bool CheckLoad(bool charaEntry) {
            if (charaEntry) {
                return !CharacterApi.GetRegisteredBehaviour("KSOX").MaintainState;
            } else {
                return !CharacterApi.GetRegisteredBehaviour(ExtendedDataId).MaintainState;
            }
        }

        /// <summary>
        /// 拷貝當前Outfit的指定Overlay至所有Outfits
        /// </summary>
        /// <param name="type">要拷貝的Overlay</param>
        public void CopyOverlayToAllOutfits(TexType type) {
            SetOverlayByCoordinateType(BackCoordinateType, GetOverlayLoaded());
            foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> coordKVP in OverlayTable) {
                coordKVP.Value[type] = OverlayTable[CurrentCoordinate.Value][type];
            }
        }

        /// <summary>
        /// 填滿空的Outfits
        /// </summary>
        /// <param name="fillWithNull">True填入空白，False填入當前Outfit</param>
        /// <returns>所填入的Overlay內容</returns>
        public Dictionary<TexType, int> FillAllEmptyOutfits(bool fillWithNull = false) {
            CurrentOverlay = GetOverlayLoaded();
            Dictionary<TexType, int> fillIn = OverlayTable[CurrentCoordinate.Value];
            if (fillWithNull || null == fillIn) {
                fillIn = new Dictionary<TexType, int>();
                foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                    fillIn[type] = 0;
                }
            }

            foreach (ChaFileDefine.CoordinateType type in Enum.GetValues(typeof(ChaFileDefine.CoordinateType))) {
                if (!OverlayTable.TryGetValue(type, out Dictionary<TexType, int> o) || null == o) {
                    OverlayTable[type] = fillIn;
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

        /// <summary>
        /// 舊的com.jim60105.kk.irisoverlaybycoordinate存檔轉換
        /// </summary>
        /// <param name="input">要過轉換的ChaFile或ChaFileCoordinate</param>
        /// <returns>轉換完成的ChaFile或ChaFileCoordinate</returns>
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
