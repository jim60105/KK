using BepInEx.Logging;
using Extension;
using Harmony;
using Illusion.Game;
using MessagePack;
using Studio;
using System;
using System.Collections;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption
{
    internal class Patches
    {
        delegate void callback();
        private static CharaFileSort charaFileSort;
        private static MPCharCtrl mpCharCtrl;
        private static int[] clothesIdBackup = null;
        private static ChaFileClothes.PartsInfo.ColorInfo[][] clothesColorBackup = null;
        private static int[] subClothesIdBackup = null;
        private static ChaFileAccessory.PartsInfo[] accessoriesPartsBackup = null;
        private static Toggle[] toggleList = null;
        private static bool _skipFlag = false;

        public static readonly string[] MainClothesNames =
        {
            "ct_clothesTop",
            "ct_clothesBot",
            "ct_bra",
            "ct_shorts",
            "ct_gloves",
            "ct_panst",
            "ct_socks",
            "ct_shoes_inner",
            "ct_shoes_outer"
        };
        public static readonly string[] SubClothesNames =
        {
            "ct_top_parts_A",
            "ct_top_parts_B",
            "ct_top_parts_C"
        };

        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(MPCharCtrl).GetMethod("OnClickRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(Patches), nameof(OnClickRootPostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(Patches), nameof(InitPostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("OnClickLoad", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnClickLoadPrefix), null), new HarmonyMethod(typeof(Patches), nameof(OnClickLoadPostfix), null), null);

            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist)
            {
                MoreAccessories_Support.InitPatch(harmony);
            }
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Patch Insert Complete");
        }

        private static void InitPostfix(object __instance)
        {
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Init Patch");
            Array ClothesKindArray = Enum.GetValues(typeof(ChaFileDefine.ClothesKind));

            //Draw Panel and ButtonAll
            charaFileSort = (CharaFileSort)__instance.GetPrivate("fileSort");
            Image panel = UILib.UIUtility.CreatePanel("TooglePanel", charaFileSort.root.parent.parent.parent);
            Button btnAll = UILib.UIUtility.CreateButton("BtnAll", panel.transform, "all");
            btnAll.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll.GetComponent<Image>().color = Color.gray;
            btnAll.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 25f + 20f * ClothesKindArray.Length), new Vector2(-5f, 50f + 20f * (ClothesKindArray.Length)));
            btnAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Toggles
            Toggle[] tgls = new Toggle[ClothesKindArray.Length + 1];
            for (int i = 0; i < ClothesKindArray.Length; i++)
            {
                tgls[i] = UILib.UIUtility.CreateToggle(ClothesKindArray.GetValue(i).ToString(), panel.transform, ClothesKindArray.GetValue(i).ToString());
                tgls[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tgls[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls[i].transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 20f * (ClothesKindArray.Length - i)), new Vector2(5f, 25f + 20f * (ClothesKindArray.Length - i)));
                tgls[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));
            }

            tgls[ClothesKindArray.Length] = UILib.UIUtility.CreateToggle("ToggleAccessories", panel.transform, "accessories");
            tgls[ClothesKindArray.Length].GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
            tgls[ClothesKindArray.Length].GetComponentInChildren<Text>(true).color = Color.white;
            tgls[ClothesKindArray.Length].transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 0f), new Vector2(5f, 25f));
            tgls[ClothesKindArray.Length].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));

            panel.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(407f, 285f - ClothesKindArray.Length * 20), new Vector2(150f, 340f));
            panel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

            btnAll.onClick.RemoveAllListeners();
            btnAll.onClick.AddListener(delegate ()
            {
                bool flag = false;
                for (int i = 0; i < tgls.Length; i++)
                {
                    if (!tgls[i].isOn)
                    {
                        flag = true;
                    }
                    tgls[i].isOn = true;
                }
                if (!flag)
                {
                    for (int j = 0; j < tgls.Length; j++)
                    {
                        tgls[j].isOn = false;
                    }
                }
            });
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Draw Panel Finish");
        }

        public static void OnClickRootPostfix(MPCharCtrl __instance, int _idx)
        {
            if (_idx > 0 && __instance != null)
            {
                mpCharCtrl = __instance;
            }
        }

        public static bool OnClickLoadPrefix()
        {
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Onclick Patch Start");
            _skipFlag = false;

            if (null == charaFileSort)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Get charaFileSort FAILED in postfix");
                _skipFlag = true;
                return true;
            }
            if (charaFileSort == null || charaFileSort.select < 0)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Get filesort ERROR");
                _skipFlag = true;
                return true;
            }
            toggleList = charaFileSort.root.parent.parent.parent.GetComponentsInChildren<Toggle>();
            if (toggleList.Length < 0)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Getting ToggleList FAILED");
                _skipFlag = true;
                return true;
            }
            bool flag = true;
            bool flag2 = false;
            foreach (Toggle tgl in toggleList)
            {
                flag &= tgl.isOn;
                flag2 |= tgl.isOn;
            }
            if (flag)
            {
                Logger.Log(LogLevel.Info, "[KK_SCLO] Toggle all true, skip roll back");
                _skipFlag = true;
                toggleList = null;
                return true;
            }
            if (!flag2)
            {
                Logger.Log(LogLevel.Info, "[KK_SCLO] No Toogle selected, skip loading coordinate");
                _skipFlag = true;
                toggleList = null;
                return false;
            }
            BackupCoordinateData();
            return true;
        }

        public static void BackupCoordinateData()
        {
            ChaControl chaCtrl = mpCharCtrl.ociChar.charInfo;
            if (chaCtrl == null)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Get chaCtrl FAILED");
                _skipFlag = true;
                return;
            }
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Get ChaCtrl");
            ChaFileClothes clothes = chaCtrl.nowCoordinate.clothes;
            ChaFileAccessory accessories = chaCtrl.nowCoordinate.accessory;

            //Backup
            clothesIdBackup = new int[clothes.parts.Length];
            clothesColorBackup = new ChaFileClothes.PartsInfo.ColorInfo[clothes.parts.Length][];
            subClothesIdBackup = new int[clothes.subPartsId.Length];
            accessoriesPartsBackup = new ChaFileAccessory.PartsInfo[accessories.parts.Length];
            for (int i = 0; i < clothes.parts.Length; i++)
            {
                clothesIdBackup[i] = clothes.parts[i].id;
                clothesColorBackup[i] = clothes.parts[i].colorInfo;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original: " + MainClothesNames[i] + "/ ID: " + clothes.parts[i].id);
            }
            for (int j = 0; j < clothes.subPartsId.Length; j++)
            {
                subClothesIdBackup[j] = clothes.subPartsId[j];
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original: " + SubClothesNames[j] + "/ ID: " + clothes.subPartsId[j]);
            }
            for (int i = 0; i < accessories.parts.Length; i++)
            {
                accessoriesPartsBackup[i] = accessories.parts[i];

                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original Accessory: " + accessories.parts[i].id);
            }
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original coordinate SUCCESS");

            //BackupKCOX
            if (KK_StudioCoordinateLoadOption._isKCOXExist)
            {
                KCOX_Support.BackupKCOXData(chaCtrl, clothes);
            }

            //BackupABMX
            if (KK_StudioCoordinateLoadOption._isABMXExist)
            {
                ABMX_Support.BackupABMXData(chaCtrl);
            }

            //BackupMoreAccessories
            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist)
            {
                MoreAccessories_Support.CopyMoreAccessoriesData(chaCtrl.chaFile);
            }

            //Get whole clothes and whole accessories
            //byte[] bytes = MessagePackSerializer.Serialize<ChaFileClothes>(chaCtrl.nowCoordinate.clothes);
            //byte[] originalAccBytes = MessagePackSerializer.Serialize<ChaFileAccessory>(chaCtrl.nowCoordinate.accessory);

            //Change clothes part
            //clothesIdBackup[kind] = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(clothes.parts[kind]);
            //chaCtrl.nowCoordinate.clothes.parts[kind] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(clothesIdBackup[kind]);

            //Load function
            //chaCtrl.nowCoordinate.LoadFile(fullPath);
            //Logger.Log(LogLevel.Debug,"[KK_SCLO] Loaded new clothes SUCCESS");
        }

        //Rollback
        public static void OnClickLoadPostfix()
        {
            if (_skipFlag)
            {
                CleanBackup();
                return;
            }

            if (null == accessoriesPartsBackup || null == clothesIdBackup || null == subClothesIdBackup)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Get Backup FAILED");
                CleanBackup();
                return;
            }

            ChaControl chaCtrl = mpCharCtrl.ociChar.charInfo;
            if (chaCtrl == null)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Get chaCtrl FAILED");
                CleanBackup();
                return;
            }
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Starting to roll back origin clothes");

            int doCount = 0;
            foreach (Toggle tgl in toggleList)
            {
                chaCtrl.StartCoroutine(doChange(tgl, () =>
                {
                    doCount++;
                    //Finish Rollback all of the clothes
                    if (doCount == toggleList.Length)
                    {
                        CleanBackup();
                    }
                }));
            }

            IEnumerator doChange(Toggle tgl, callback Callback)
            {
                yield return null;
                if (!tgl.isOn)
                {
                    /*
                    ChaFileDefine.ClothesKind
                    public enum ClothesKind
                    {
                        top,
                        bot,
                        bra,
                        shorts,
                        gloves,
                        panst,
                        socks,
                        shoes_inner,
                        shoes_outer
                    }
                    */

                    //Rollback accessories
                    if (String.Equals(tgl.GetComponentInChildren<Text>(true).text, "accessories"))
                    {
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Roll back:" + tgl.GetComponentInChildren<Text>(true).text);
                        //Rollback MoreAccessories first
                        if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist)
                        {
                            MoreAccessories_Support.RollbackMoreAccessoriesData(chaCtrl.chaFile);
                        }
                        //Rollback normal accessories
                        chaCtrl.nowCoordinate.accessory = new ChaFileAccessory();
                        for (int i = 0; i < accessoriesPartsBackup.Length; i++)
                        {
                            //chaCtrl.nowCoordinate.accessory.parts[i] = accessoriesPartsBackup[i];
                            var tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(accessoriesPartsBackup[i]);
                            chaCtrl.nowCoordinate.accessory.parts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
                            chaCtrl.ChangeAccessory(i, accessoriesPartsBackup[i].type, accessoriesPartsBackup[i].id, accessoriesPartsBackup[i].parentKey, true);
                        }
                        chaCtrl.Reload(false, true, true, true);
                        chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
                        Callback?.Invoke();
                        yield break;
                    }

                    //Discard unknown toggle
                    object tmpToggleType = null;
                    try
                    {
                        tmpToggleType = Enum.Parse(typeof(ChaFileDefine.ClothesKind), tgl.GetComponentInChildren<Text>(true).text);
                    }
                    catch (NullReferenceException)
                    {
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] Discard Unknown Toggle:" + tgl.GetComponentInChildren<Text>(true).text);
                        Callback?.Invoke();
                        yield break;
                    }

                    int kind = Convert.ToInt32(tmpToggleType);
                    //Roll back clothes
                    chaCtrl.ChangeClothes(kind, clothesIdBackup[kind], subClothesIdBackup[0], subClothesIdBackup[1], subClothesIdBackup[2], true);
                    chaCtrl.nowCoordinate.clothes.parts[kind].colorInfo = clothesColorBackup[kind];
                    Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Roll back:" + tgl.GetComponentInChildren<Text>(true).text + " / ID: " + clothesIdBackup[kind]);
                    //Rollback KCOX
                    if (KK_StudioCoordinateLoadOption._isKCOXExist && clothesIdBackup[kind] != 0)
                    {
                        chaCtrl.Reload(false, true, true, true);
                        chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
                        KCOX_Support.RollbackOverlay(true, kind);
                    }
                    chaCtrl.Reload(false, true, true, true);
                    chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);

                    switch (tmpToggleType)
                    {
                        case ChaFileDefine.ClothesKind.top: 
                            //Rollback SubCloth KCOX
                            if (KK_StudioCoordinateLoadOption._isKCOXExist)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    KCOX_Support.RollbackOverlay(false, j);
                                }
                            }
                            break;
                        case ChaFileDefine.ClothesKind.bot:
                            //Rollback ABMX if bot select
                            if (KK_StudioCoordinateLoadOption._isABMXExist)
                            {
                                ABMX_Support.RollbackABMXBone(chaCtrl);
                            }
                            break;
                        default:
                            break;
                    }
                }
                Callback?.Invoke();
                yield break;
            }
        }

        private static void CleanBackup()
        {
            clothesIdBackup = null;
            accessoriesPartsBackup = null;
            subClothesIdBackup = null;
            if (KK_StudioCoordinateLoadOption._isKCOXExist) KCOX_Support.CleanKCOXBackup();
            if (KK_StudioCoordinateLoadOption._isABMXExist) ABMX_Support.CleanABMXBackup();
            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) MoreAccessories_Support.CleanMoreAccBackup();
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Finish");
            Utils.Sound.Play(SystemSE.ok_s);
        }
    }
}
