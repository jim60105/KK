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
using ChaCustom;
using CoordinateLoadOption.OtherPlugin;
using CoordinateLoadOption.OtherPlugin.CharaCustomFunctionController;
using Extension;
using HarmonyLib;
using Studio;
using System;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;

namespace CoordinateLoadOption
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("KoikatsuSunshine")]
    [BepInProcess("CharaStudio")]
    [BepInDependency("KCOX", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("KKABMX.Core", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.materialeditor", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.hairaccessorycustomizer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.jim60105.kk.charaoverlaysbasedoncoordinate", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("marco.FolderBrowser", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("KK_ClothesLoadOption")]
    [BepInIncompatibility("com.jim60105.kk.studiocoordinateloadoption")]
    public class CoordinateLoadOption : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Coordinate Load Option";
        internal const string GUID = "com.jim60105.kks.coordinateloadoption";
        internal const string PLUGIN_VERSION = "22.12.04.3";
        internal const string PLUGIN_RELEASE_VERSION = "1.4.7";

        public static bool insideStudio = Application.productName == "CharaStudio";

        Harmony harmonyInstance;
        internal static new ManualLogSource Logger;

        public static bool _isKCOXExist = false;
        public static bool _isABMXExist = false;
        public static bool _isMoreAccessoriesExist = false;
        public static bool _isMaterialEditorExist = false;
        public static bool _isHairAccessoryCustomizerExist = false;

        internal const int FORCECLEANCOUNT = 100;    //tmpChara清理倒數
        public enum ClothesKind
        {
            top = 0,
            bot = 1,
            bra = 2,
            shorts = 3,
            gloves = 4,
            panst = 5,
            socks = 6,
            shoes_inner = 7,
            shoes_outer = 8,
            accessories = 9 /*注意這個*/
        }

        public static string[] pluginBoundAccessories =
        {
            // Pre-filled
            "madevil.kk.ass",
            "madevil.kk.mr",
            "madevil.kk.ca",
            "BonerStateSync",
            "BendUrAcc",
            "madevil.kk.AAAPK"
        };

        internal static Vector3 defaultPanelPosition = Vector3.zero;
        public static ConfigEntry<Vector3> Maker_Panel_Position { get; private set; }
        public static ConfigEntry<Vector3> Studio_Panel_Position { get; private set; }
        public static ConfigEntry<string> Plugin_Bound_Accessories { get; private set; }

        public void Awake()
        {
            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            UIUtility.Init();
            harmonyInstance = Harmony.CreateAndPatchAll(typeof(Patches));

            //Studio
            Type CostumeInfoType = typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic);
            harmonyInstance.Patch(CostumeInfoType.GetMethod("Init", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix)));
            harmonyInstance.Patch(CostumeInfoType.GetMethod("OnClickLoad", AccessTools.all),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPrefix)),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPostfix)));
            harmonyInstance.Patch(CostumeInfoType.GetMethod("OnSelect", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnSelectPostfix)));

            //Maker
            harmonyInstance.Patch(typeof(CustomCoordinateFile).GetMethod("FileWindowSetting", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix)));
            harmonyInstance.Patch(typeof(CustomCoordinateFile).GetMethod("OnChangeSelect", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnSelectPostfix)));
        }

        public void Start()
        {
            _isKCOXExist = new KCOX(null).LoadAssembly();
            _isABMXExist = new ABMX(null).LoadAssembly();
            _isMoreAccessoriesExist = MoreAccessories.LoadAssembly();
            _isMaterialEditorExist = new MaterialEditor(null).LoadAssembly();
            _isHairAccessoryCustomizerExist = new HairAccessoryCustomizer(null).LoadAssembly();

            //Patch other plugins at Start()
            if (_isHairAccessoryCustomizerExist)
                HairAccessoryCustomizer.Patch(harmonyInstance);

            if(_isMoreAccessoriesExist)
                MoreAccessories.PatchMoreAcc(harmonyInstance);

            FolderBrowser.PatchFolderBrowser(harmonyInstance);

            StringResources.StringResourcesManager.SetUICulture();

            Plugin_Bound_Accessories = Config.Bind<string>("Settings", "Plugin that bound accessories options", "", new ConfigDescription("Edit this only when any plugin maker tells you to do so. Fill in the GUIDs, and seperate them with comma(,), example: 'this.guid.A,some.guid.B,another.guid.C'"));
            pluginBoundAccessories = pluginBoundAccessories.Concat(Plugin_Bound_Accessories.Value.Split(','))
                                                           .Where(p => null != p
                                                                       && !string.IsNullOrEmpty(p))
                                                           .Distinct()
                                                           .ToArray();

            if (insideStudio)
            {
                Studio_Panel_Position = Config.Bind<Vector3>("Settings", "Studio Panel Position", defaultPanelPosition);
                Studio_Panel_Position.SettingChanged += _MovePanel;
            }
            else
            {
                Maker_Panel_Position = Config.Bind<Vector3>("Settings", "Maker Panel Position", defaultPanelPosition);
                Maker_Panel_Position.SettingChanged += _MovePanel;
            }

            void _MovePanel(object sender, EventArgs e)
            {
                if (sender is ConfigEntry<Vector3> s)
                {
                    Patches.panel.transform.localPosition = s.Value;
                    if (s.Value == Vector3.zero && defaultPanelPosition != Vector3.zero) s.Value = defaultPanelPosition;
                }
            };
        }

        public void Update() => CoordinateLoad.Update();
    }
}
