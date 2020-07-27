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
using HarmonyLib;
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
        internal const string PLUGIN_VERSION = "20.07.27.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.3.6";

        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> Enable_Saving_To_Chara { get; private set; }
        public static ConfigEntry<bool> Enable_Saving_To_Coordinate { get; private set; }
        public static ConfigEntry<bool> Save_Eyes_Overlay { get; private set; }
        public static ConfigEntry<bool> Save_Face_Overlay { get; private set; }
        public static ConfigEntry<bool> Save_Body_Overlay { get; private set; }
        public static ConfigEntry<bool> Warning_Message { get; private set; }
        internal static MakerRadioButtons IrisSideRadioBtn = null;

        public void Awake() {
            Logger = base.Logger;
            Enable_Saving_To_Chara = Config.Bind<bool>("Main Config", "Saving to character outfits (7 outfits)", true, "Enable this to save the plugin data to the character file");
            Enable_Saving_To_Coordinate = Config.Bind<bool>("Main Config", "Saving to Coordinate files", false, "[Warning] It is highly recommended to enable this ONLY WHEN NEEDED");
            Warning_Message = Config.Bind<bool>("Main Config", "Warning Message", true, "Enable/Disable warning message when saving files");
            Save_Eyes_Overlay = Config.Bind<bool>("Save By Coordinate (Enable Main Config first)", "Eyes Overlay", true, "This setting will only react when main config enabled");
            Save_Face_Overlay = Config.Bind<bool>("Save By Coordinate (Enable Main Config first)", "Face Overlay", true, "This setting will only react when main config enabled");
            Save_Body_Overlay = Config.Bind<bool>("Save By Coordinate (Enable Main Config first)", "Body Overlay", true, "This setting will only react when main config enabled");
            CharacterApi.RegisterExtraBehaviour<CharaOverlaysBasedOnCoordinateController>(GUID);
            Harmony.CreateAndPatchAll(typeof(Patches));
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
            CharaOverlaysBasedOnCoordinateController controller = MakerAPI.GetCharacterControl().gameObject.GetComponent<CharaOverlaysBasedOnCoordinateController>();
            KoiSkinOverlayController KSOXController = MakerAPI.GetCharacterControl().gameObject.GetComponent<KoiSkinOverlayController>();

            MakerCategory irisCategory = MakerConstants.Face.Iris;
            MakerCategory eyeCategory = new MakerCategory(irisCategory.CategoryName, "tglEyeOverlayKSOX", irisCategory.Position + 5, "Iris Overlays");
            e.AddSubCategory(eyeCategory);

            e.AddControl(new MakerButton("Copy iris overlay to all outfits", eyeCategory, owner))
                .OnClick.AddListener(delegate () {
                    controller.CopyOverlayToAllOutfits(TexType.EyeOver);
                    controller.CopyOverlayToAllOutfits(TexType.EyeUnder);
                    Logger.LogMessage("[Chara Overlays Based On Coordinate] Successfully copied iris overlay");
                }
            );
            IrisSideRadioBtn = new MakerRadioButtons(eyeCategory, owner, "Side to display", "Both", "Left", "Right");
            e.AddControl(IrisSideRadioBtn)
                .ValueChanged.Subscribe(side => {
                    controller.ChangeIrisDisplayside(side);
                    //Logger.LogDebug("Changed RadioBtn: " + side);
                }
            );
        }

        private void SetupBodyInterface(RegisterSubCategoriesEvent e, KK_CharaOverlaysBasedOnCoordinate owner) {
            CharaOverlaysBasedOnCoordinateController controller = MakerAPI.GetCharacterControl().gameObject.GetComponent<CharaOverlaysBasedOnCoordinateController>();
            MakerCategory paintCategory = MakerConstants.Body.Paint;
            MakerCategory makerCategory = new MakerCategory(paintCategory.CategoryName, "tglOverlayKSOX", paintCategory.Position + 5, "Skin Overlays");
            e.AddSubCategory(makerCategory);

            e.AddControl(new MakerButton("Copy face overlay to all outfits", makerCategory, owner))
                .OnClick.AddListener(delegate () {
                    controller.CopyOverlayToAllOutfits(TexType.FaceOver);
                    controller.CopyOverlayToAllOutfits(TexType.FaceUnder);
                    Logger.LogMessage("[Chara Overlays Based On Coordinate] Successfully copied face overlay");
                }
            );
            e.AddControl(new MakerButton("Copy body overlay to all outfits", makerCategory, owner))
                .OnClick.AddListener(delegate () {
                    controller.CopyOverlayToAllOutfits(TexType.BodyOver);
                    controller.CopyOverlayToAllOutfits(TexType.BodyUnder);
                    Logger.LogMessage("[Chara Overlays Based On Coordinate] Successfully copied body overlay");
                }
            );
        }
        #endregion
    }

    class Patches {
        private static readonly Dictionary<TexType, byte[]> EyeTexture = new Dictionary<TexType, byte[]>() {
            { TexType.EyeOver, new byte[] { } },
            { TexType.EyeUnder, new byte[] { } }
        };

        [HarmonyPrefix, HarmonyPatch(typeof(CustomTextureCreate), nameof(CustomTextureCreate.RebuildTextureAndSetMaterial))]
        public static void RebuildTexPrefix(CustomTextureCreate __instance, ref bool __state) {
            __state = false;
            CharaOverlaysBasedOnCoordinateController controller = __instance.trfParent?.GetComponent<CharaOverlaysBasedOnCoordinateController>();
            if (null == controller || controller.LoadingLock) return;

            CustomTextureCreate toCompare;
            switch (controller.IrisDisplaySide[(int)controller.CurrentCoordinate.Value]) {
                case 1:
                    toCompare = controller.ChaControl.ctCreateEyeL;
                    break;
                case 2:
                    toCompare = controller.ChaControl.ctCreateEyeR;
                    break;
                default:
                    //Not Display Both
                    return;
            }

            //要保留的那邊return
            if (toCompare == __instance) return;

            __state = true;
            foreach (KeyValuePair<TexType, byte[]> kvp in controller.GetOverlayLoaded(new TexType[] { TexType.EyeOver, TexType.EyeUnder })) {
                EyeTexture[kvp.Key] = kvp.Value;
                controller.OverwriteOverlayWithoutUpdate(kvp.Key, new byte[] { });
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomTextureCreate), nameof(CustomTextureCreate.RebuildTextureAndSetMaterial))]
        public static void RebuildTexPostfix(CustomTextureCreate __instance, ref bool __state) {
            if (!__state) return;

            CharaOverlaysBasedOnCoordinateController controller = __instance.trfParent?.GetComponent<CharaOverlaysBasedOnCoordinateController>();
            if (null != controller) {
                foreach (KeyValuePair<TexType, byte[]> kvp in EyeTexture) {
                    controller.OverwriteOverlayWithoutUpdate(kvp.Key, kvp.Value);
                }
                Extension.Extension.TryGetPluginInstance("KSOX_GUI").Invoke("OnChaFileLoaded");
            }
        }
    }

    internal class CharaOverlaysBasedOnCoordinateController : CharaCustomFunctionController {
        private static readonly ManualLogSource Logger = KK_CharaOverlaysBasedOnCoordinate.Logger;
        private KoiSkinOverlayController KSOXController;
        private KoiSkinOverlayGui KoiSkinOverlayGui;
        internal ChaFileDefine.CoordinateType BackCoordinateType;

        private readonly List<byte[]> Resources = new List<byte[]>();
        internal readonly Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> OverlayTable = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>>();
        internal int[] IrisDisplaySide = new int[] { 0, 0, 0, 0, 0, 0, 0 };
        internal bool LoadingLock = false;

        /// <summary>
        /// 此插件內儲存的Overlay。Get method返回之Dictionary只能讀，寫入需以Set完整寫入
        /// </summary>
        public Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> Overlays {
            get {
                Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> overlays = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
                foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> kvp in OverlayTable) {
                    if (TryGetOverlayByCoordinateType(kvp.Key, out Dictionary<TexType, byte[]> overlay) && null != overlay) {
                        Dictionary<TexType, byte[]> tmp = new Dictionary<TexType, byte[]>();
                        foreach (KeyValuePair<TexType, byte[]> kvp2 in overlay) {
                            tmp.Add(kvp2.Key, (byte[])kvp2.Value?.Clone());
                        }
                        overlay = tmp;
                    } else {
                        overlay = new Dictionary<TexType, byte[]>();
                        foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                            overlay[type] = null;
                        }
                    }
                    overlays[kvp.Key] = new Dictionary<TexType, byte[]>(overlay);
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
                Resources.Add((byte[])b.Clone());
                return Resources.Count - 1;
            }
        }

        /// <summary>
        /// 讀入overlay至插件內
        /// </summary>
        /// <param name="coordinateType">服裝類型</param>
        /// <param name="overlay">要讀入的Overlay</param>
        /// <returns>OverlayTable</returns>
        public Dictionary<TexType, int> SetOverlayByCoordinateType(ChaFileDefine.CoordinateType? coordinateType, Dictionary<TexType, byte[]> overlay) {
            bool notNullFlag = null != coordinateType;
            Dictionary<TexType, int> result = new Dictionary<TexType, int>();
            ChaFileDefine.CoordinateType type = coordinateType ?? ChaFileDefine.CoordinateType.School01;
            foreach (KeyValuePair<TexType, byte[]> kvp in overlay) {
                result[kvp.Key] = InputOverlayTexture(kvp.Value);
                //if (0 != result[kvp.Key] && notNullFlag && KoikatuAPI.GetCurrentGameMode() == GameMode.Maker) Logger.LogDebug($"Input Texture: {type.ToString()}({kvp.Key.ToString()}): {result[kvp.Key]}");
            }
            if (notNullFlag) {
                OverlayTable[type] = result;
                //OverlaysReadyToSave[type] = PrepareOverlayToSave(OverlayTable[type], out _);
            }
            return result;
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

            base.Start();
        }

        #region SaveLoad
        protected override void OnCardBeingSaved(GameMode currentGameMode) {
            if (currentGameMode == GameMode.Maker && !CheckEnableSaving(true)) {
                SavePluginData(true);
                return;
            }
            if (currentGameMode == GameMode.MainGame) {
                return;
            }

            SetOverlayByCoordinateType(BackCoordinateType, GetOverlayLoaded());
            RebuildOverlayTableAndResources();
            SavePluginData();

            Logger.LogDebug("Saved Chara data");
        }
        //保存此插件資料
        public void SavePluginData(bool SaveNothing = false) {
            if (SaveNothing) {
                SetExtendedData(null);
            } else {
                PluginData pd = new PluginData();
                pd.data.Add("AllCharaOverlayTable", PrepareOverlayTableToSave());
                pd.data.Add("AllCharaResources", Resources.Select((x, i) => new { i, x }).ToDictionary(y => y.i, y => y.x));
                pd.data.Add("IrisDisplaySideList", MessagePack.MessagePackSerializer.Serialize<int[]>(IrisDisplaySide));
                pd.version = 3;

                SetExtendedData(pd);
            }
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState) {
            KSOXController = CharacterApi.GetRegisteredBehaviour(typeof(KoiSkinOverlayController)).Instances.Where(x => x.ChaControl == base.ChaControl).Single() as KoiSkinOverlayController;
            BackCoordinateType = CurrentCoordinate.Value;
            //Maker沒有打勾就不載入
            if (!CheckLoad(true)) return;
            LoadingLock = true;

            UpdateOldGUIDSaveData((ChaFile)ChaFileControl);
            OverlayTable.Clear();
            IrisDisplaySide = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            PluginData data = GetExtendedData();
            if (null != data && data.version != 3) {
                UpdateOldVersionSaveData(data);
            } else {
                ReadPluginData(data);
            }
            Logger.LogDebug($"{ChaFileControl.parameter.fullname} Load Extended Data");
            OverwriteOverlay();
            LoadingLock = false;
            UpdateInterface();
        }
        public void ReadPluginData(PluginData data) {
            if (data == null) {
                Logger.LogInfo("No PluginData Existed");
            } else {
                if ((!data.data.TryGetValue("AllCharaOverlayTable", out object tmpOverlayTable) || tmpOverlayTable == null) ||
                     (!data.data.TryGetValue("AllCharaResources", out object tmpResources) || null == tmpResources)) {
                    Logger.LogInfo("Wrong PluginData Existed");
                } else {
                    Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> overlays = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
                    List<byte[]> resourceList = tmpResources.ToDictionary<int, byte[]>().Select(x => x.Value).ToList();
                    foreach (KeyValuePair<ChaFileDefine.CoordinateType, object> kvp in tmpOverlayTable.ToDictionary<ChaFileDefine.CoordinateType, object>()) {
                        Dictionary<TexType, byte[]> coordinate = new Dictionary<TexType, byte[]>();
                        foreach (KeyValuePair<TexType, int> kvp2 in kvp.Value.ToDictionary<TexType, int>()) {
                            coordinate.Add(kvp2.Key, resourceList[kvp2.Value]);
                            if (kvp2.Value != 0) {
                                Logger.LogDebug($"->{kvp.Key.ToString()}: {kvp2.Key.ToString()}, {kvp2.Value}");
                            }
                        }
                        overlays.Add(kvp.Key, coordinate);
                    }

                    Overlays = overlays;

                    if (data.data.TryGetValue("IrisDisplaySideList", out object tmpSide) && tmpSide is byte[] b) {
                        IrisDisplaySide = MessagePack.MessagePackSerializer.Deserialize<int[]>(b);
                    }
                }
            }
            KSOXController.Invoke("OnReload", new object[] { KoikatuAPI.GetCurrentGameMode(), false });
            FillAllEmptyOutfits();
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate) {
            PluginData pd = new PluginData();
            if (!CheckEnableSaving(false)) {
                SetCoordinateExtendedData(coordinate, null);
                return;
            }
            SetOverlayByCoordinateType(BackCoordinateType, GetOverlayLoaded());

            Dictionary<TexType, int> overlayTable = PrepareOverlayToSave(OverlayTable[BackCoordinateType]);
            Dictionary<int, byte[]> resources = new Dictionary<int, byte[]>();

            foreach (KeyValuePair<TexType, int> kvp in overlayTable) {
                if (!resources.ContainsKey(kvp.Value)) {
                    resources[kvp.Value] = Resources[kvp.Value];
                }
            }

            pd.data.Add("CharaOverlayTable", overlayTable);
            pd.data.Add("CharaResources", resources);
            pd.data.Add("IrisDisplaySide", IrisDisplaySide[(int)CurrentCoordinate.Value]);
            pd.version = 3;

            SetCoordinateExtendedData(coordinate, pd);
            Logger.LogDebug("Save Coordinate data");
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState) {
            KSOXController = CharacterApi.GetRegisteredBehaviour(typeof(KoiSkinOverlayController)).Instances.Where(x => x.ChaControl == base.ChaControl).Single() as KoiSkinOverlayController;
            if (!CheckLoad(false)) return;
            LoadingLock = true;

            //IrisDisplaySide[(int)CurrentCoordinate.Value] = 0;
            PluginData data = GetCoordinateExtendedData((ChaFileCoordinate)UpdateOldGUIDSaveData(coordinate));
            if (null != data && data.version != 3) {
                UpdateOldVersionSaveData(data);
            } else {
                if (null == data) {
                    Logger.LogInfo("No PluginData Existed");
                    CurrentOverlay = GetOverlayLoaded();
                } else if ((!data.data.TryGetValue("CharaOverlayTable", out object tmpOverlayTable) || tmpOverlayTable == null) ||
                          (!data.data.TryGetValue("CharaResources", out object tmpResources) || null == tmpResources)) {
                    Logger.LogInfo("No Exist Data found from Coordinate.");
                    CurrentOverlay = GetOverlayLoaded();
                } else {
                    Dictionary<TexType, byte[]> coordinateData = new Dictionary<TexType, byte[]>();
                    Dictionary<int, byte[]> resourceList = tmpResources.ToDictionary<int, byte[]>();
                    foreach (KeyValuePair<TexType, int> kvp in tmpOverlayTable.ToDictionary<TexType, int>()) {
                        coordinateData.Add(kvp.Key, resourceList[kvp.Value]);
                    }

                    CurrentOverlay = coordinateData;

                    if (data.data.TryGetValue("IrisDisplaySide", out object tmpSide) && null != tmpSide && (tmpSide is int tmpIntSide)) {
                        IrisDisplaySide[(int)CurrentCoordinate.Value] = tmpIntSide;
                    }
                }
            }
            foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                if (!OverlayTable[CurrentCoordinate.Value].TryGetValue(type, out _)) {
                    OverlayTable[CurrentCoordinate.Value][type] = 0;
                }
            }
            OverwriteOverlay();
            LoadingLock = false;
            UpdateInterface();
        }
        #endregion

        #region Model
        /// <summary>
        /// 重新建構資源，捨棄沒有用到的資源，不要頻繁呼叫
        /// </summary>
        public void RebuildOverlayTableAndResources() => Overlays = Overlays;

        //取得現在載入的Overlay
        public Dictionary<TexType, byte[]> GetOverlayLoaded(params TexType[] texTypeArray) {
            Dictionary<TexType, byte[]> result = new Dictionary<TexType, byte[]>();
            Dictionary<TexType, OverlayTexture> ori = KSOXController.Overlays.ToDictionary<TexType, OverlayTexture>();
            if (null != ori) {
                if (texTypeArray.Length == 0) {
                    texTypeArray = (TexType[])Enum.GetValues(typeof(TexType));
                }
                foreach (TexType type in texTypeArray) {
                    result.Add(type, ori.TryGetValue(type, out OverlayTexture texture) ? (byte[])texture.Data.Clone() : null);
                }
            } else result = null;

            return result;
        }

        /// <summary>
        /// 依照儲存設定，返回整個整理好的OverlayTable
        /// </summary>
        public Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> PrepareOverlayTableToSave() {
            FillAllEmptyOutfits(KKAPI.Studio.StudioAPI.InsideStudio);
            Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> result = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, int>>();
            foreach (KeyValuePair<ChaFileDefine.CoordinateType, Dictionary<TexType, int>> coordKVP in OverlayTable) {
                Dictionary<TexType, int> tmpCoord = PrepareOverlayToSave(coordKVP.Value);
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
        public Dictionary<TexType, int> PrepareOverlayToSave(Dictionary<TexType, int> coordinateData) {
            Dictionary<TexType, int> result = new Dictionary<TexType, int>();
            Dictionary<TexType, int> overlayLoaded;
            try {
                overlayLoaded = OverlayTable[BackCoordinateType];
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Eyes_Overlay.Value, TexType.EyeOver, TexType.EyeUnder);
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Face_Overlay.Value, TexType.FaceOver, TexType.FaceUnder);
                doMain(KK_CharaOverlaysBasedOnCoordinate.Save_Body_Overlay.Value, TexType.BodyOver, TexType.BodyUnder);
            } catch (KeyNotFoundException) {
                FillAllEmptyOutfits();
                return PrepareOverlayToSave(coordinateData);
            }
            return result;

            void doMain(bool b, TexType t1, TexType t2) {
                if (b || KKAPI.Studio.StudioAPI.InsideStudio) {
                    doMain2(t1);
                    doMain2(t2);
                } else {
                    result.Add(t1, overlayLoaded[t1]);
                    result.Add(t2, overlayLoaded[t2]);
                }
            }
            void doMain2(TexType t) {
                result.Add(t, coordinateData[t]);
            }
        }

        //切換服裝
        private void ChangeCoordinate() {
            Logger.LogDebug("Change Overlay");
            if (null == KSOXController || !KSOXController.Started) { return; }

            if (BackCoordinateType != CurrentCoordinate.Value) {
                LoadingLock = true;
                SetOverlayByCoordinateType(BackCoordinateType, GetOverlayLoaded());
                OverwriteOverlay();
                BackCoordinateType = CurrentCoordinate.Value;
                LoadingLock = false;
                UpdateInterface();
            }
        }

        //複寫Overlay
        /// <summary>
        /// 往KSOX複寫Overlay
        /// </summary>
        public void OverwriteOverlay() {
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

            Logger.LogDebug("OverWrite Overlays");
        }

        internal void OverwriteOverlayWithoutUpdate(TexType texType, byte[] texture) {
            Dictionary<TexType, OverlayTexture> overlay = KSOXController.GetField("_overlays").ToDictionary<TexType, OverlayTexture>();
            if (overlay.ContainsKey(texType)) {
                if (null != texture) {
                    overlay[texType].Data = texture;
                } else {
                    overlay[texType]?.Dispose();
                    overlay.Remove(texType);
                }
            } else {
                if (null != texture) {
                    ((Dictionary<TexType, OverlayTexture>)KSOXController.GetField("_overlays")).Add(new OverlayTexture(texture));
                }
            }
            //Logger.LogDebug($"OverWrite Overlay Without Update {Enum.GetName(typeof(TexType), texType)} : {InputOverlayTexture(texture)}");
        }

        /// <summary>
        /// 存檔前的警告檢查
        /// </summary>
        /// <param name="charaEntry">True為儲存角色，False為儲存衣裝</param>
        /// <returns>依照設定判斷，是否要執行存檔動作</returns>
        private bool CheckEnableSaving(bool charaEntry) {
            if (charaEntry) {
                OverlayTable.TryGetValue(CurrentCoordinate.Value, out Dictionary<TexType, int> currentCoor);
                if (OverlayTable.Where(x => x.Key != CurrentCoordinate.Value).Select(x => x.Value).Where(delegate (Dictionary<TexType, int> x) {
                    bool flag2 = false;
                    if (!KK_CharaOverlaysBasedOnCoordinate.Save_Eyes_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value) {
                        flag2 |= doMain(TexType.EyeOver);
                        flag2 |= doMain(TexType.EyeUnder);
                    }
                    if (!flag2 && (!KK_CharaOverlaysBasedOnCoordinate.Save_Face_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value)) {
                        flag2 |= doMain(TexType.FaceOver);
                        flag2 |= doMain(TexType.FaceUnder);
                    }
                    if (!flag2 && (!KK_CharaOverlaysBasedOnCoordinate.Save_Body_Overlay.Value || !KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value)) {
                        flag2 |= doMain(TexType.BodyOver);
                        flag2 |= doMain(TexType.BodyUnder);
                    }

                    return flag2;

                    bool doMain(TexType type) {
                        return x.ContainsKey(type) && x[type] != 0 && x[type] != currentCoor[type];
                    }
                }).Any()) {
                    Logger.LogWarning("There are overlays on other outfits but the saving setting is not enabled.");
                    ShowMessage("[WARNING] Chara overlays on other outfits are not saved to Chara files.");
                    ShowMessage($"Please check the [{KK_CharaOverlaysBasedOnCoordinate.PLUGIN_NAME}] section in the config.");
                    ShowMessage($"If you really don't know what happened, read the readme of [{KK_CharaOverlaysBasedOnCoordinate.PLUGIN_NAME}].");
                }
                return KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Chara.Value;
            } else {
                if (KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Coordinate.Value) {
                    if (!GetOverlayLoaded().Values.Where(x => null != x).Any()) {
                        Logger.LogWarning("Chara overlay save to coordinate file is enable but no overlay loaded.");
                        ShowMessage("[WARNING] Chara overlay is saving NOTHING to the Coordinate file.");
                        ShowMessage("This will cause the chara overlay to BE CLEARED when loading this coordinate file.");
                        ShowMessage($"If you don't want to do this, check the [{KK_CharaOverlaysBasedOnCoordinate.PLUGIN_NAME}] section in the config.");
                    }
                }
                return KK_CharaOverlaysBasedOnCoordinate.Enable_Saving_To_Coordinate.Value;
            }

            void ShowMessage(string message) {
                if (KKAPI.Studio.StudioAPI.InsideStudio) return;

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

            if (type == TexType.EyeOver || type == TexType.EyeUnder) {
                int side = IrisDisplaySide[(int)CurrentCoordinate.Value];
                IrisDisplaySide = new int[] { side, side, side, side, side, side, side };
                UpdateInterface();
            }
            RebuildOverlayTableAndResources();
        }

        /// <summary>
        /// 填滿空的Outfits
        /// </summary>
        /// <param name="fillWithNull">True填入空白，False填入當前Outfit</param>
        /// <returns>所填入的Overlay內容</returns>
        public Dictionary<TexType, int> FillAllEmptyOutfits(bool fillWithNull = false) {
            Dictionary<TexType, int> fillIn = new Dictionary<TexType, int>();
            if (fillWithNull) {
                foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                    fillIn[type] = 0;
                }
            } else {
                fillIn = SetOverlayByCoordinateType(null, GetOverlayLoaded());
            }

            foreach (ChaFileDefine.CoordinateType type in Enum.GetValues(typeof(ChaFileDefine.CoordinateType))) {
                if (!OverlayTable.TryGetValue(type, out Dictionary<TexType, int> o) || null == o) {
                    OverlayTable[type] = new Dictionary<TexType, int>(fillIn);
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
        private object UpdateOldGUIDSaveData(object input) {
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

        /// <summary>
        /// 更新並讀入舊版本的SaveData
        /// </summary>
        /// <param name="data">舊PluginData</param>
        private void UpdateOldVersionSaveData(PluginData data) {
            if (null != data) {
                try {
                    switch (data.version) {
                        case 0:
                        case 1:
                            if ((data.data.TryGetValue("AllCharaOverlays", out object tmpOverlays) || data.data.TryGetValue("AllIrisOverlays", out tmpOverlays)) && tmpOverlays != null) {
                                Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>> overlays = new Dictionary<ChaFileDefine.CoordinateType, Dictionary<TexType, byte[]>>();
                                foreach (KeyValuePair<ChaFileDefine.CoordinateType, object> kvp in tmpOverlays.ToDictionary<ChaFileDefine.CoordinateType, object>()) {
                                    overlays.Add(kvp.Key, kvp.Value.ToDictionary<TexType, byte[]>());
                                }
                                Overlays = overlays;
                                KSOXController.Invoke("OnReload", new object[] { KoikatuAPI.GetCurrentGameMode(), false });
                                FillAllEmptyOutfits();
                                SavePluginData();
                            } else if ((data.data.TryGetValue("CharaOverlay", out object tmpOverlay) || data.data.TryGetValue("IrisOverlay", out tmpOverlay)) && tmpOverlay != null) {
                                Dictionary<TexType, byte[]> coordinate = tmpOverlay.ToDictionary<TexType, byte[]>();
                                foreach (TexType type in Enum.GetValues(typeof(TexType))) {
                                    if (!coordinate.TryGetValue(type, out _)) {
                                        coordinate[type] = null;
                                    }
                                }
                                CurrentOverlay = coordinate;
                            } else {
                                Logger.LogWarning("Wrong Old PluginData");
                            }
                            break;
                        case 2:
                            if (data.data.TryGetValue("CharaOverlayTable", out object tmpOverlayTable) && tmpOverlayTable != null &&
                                data.data.TryGetValue("CharaResources", out object tmpResources) && null != tmpResources) {
                                Dictionary<TexType, byte[]> coordinateData = new Dictionary<TexType, byte[]>();
                                List<byte[]> resourceList = MessagePack.MessagePackSerializer.Deserialize<List<byte[]>>((byte[])tmpResources);
                                foreach (KeyValuePair<TexType, int> kvp in tmpOverlayTable.ToDictionary<TexType, int>()) {
                                    coordinateData.Add(kvp.Key, resourceList[kvp.Value]);
                                }
                                CurrentOverlay = coordinateData;
                            }

                            if (data.data.TryGetValue("AllCharaOverlayTable", out tmpOverlayTable) && tmpOverlayTable != null &&
                                data.data.TryGetValue("AllCharaResources", out tmpResources) && null != tmpResources) {
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
                                FillAllEmptyOutfits();
                            }
                            break;
                    }
                    Logger.LogInfo($"Read Old PluginData from version {data.version}");
                } catch (Exception e) {
                    Logger.LogError($"Reading Old PluginData FAILED");
                    Logger.LogError($"{e.GetType().ToString()}: {e.Message}");
                    Logger.LogError($"{e.StackTrace}");
                }
            }
        }

        internal void UpdateInterface() {
            if (MakerAPI.InsideMaker &&
                null != KK_CharaOverlaysBasedOnCoordinate.IrisSideRadioBtn &&
                KK_CharaOverlaysBasedOnCoordinate.IrisSideRadioBtn.Value != IrisDisplaySide[(int)CurrentCoordinate.Value]
            ) {
                //改變後會觸發OnValueChange
                KK_CharaOverlaysBasedOnCoordinate.IrisSideRadioBtn.Value = IrisDisplaySide[(int)CurrentCoordinate.Value];
            } else {
                ChangeIrisDisplayside(IrisDisplaySide[(int)CurrentCoordinate.Value]);
            }
        }

        internal void ChangeIrisDisplayside(int side) {
            IrisDisplaySide[(int)CurrentCoordinate.Value] = side;
            Logger.LogDebug("Changed iris display: " + side);
            ChaControl.ChangeSettingEye(true, true, true);
        }
        #endregion
    }
}
