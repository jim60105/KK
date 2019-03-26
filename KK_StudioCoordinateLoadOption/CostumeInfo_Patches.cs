using System;
using System.Reflection;
using Harmony;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Illusion.Game;
using MessagePack;

namespace KK_CostumeInfo_Patches
{
	internal class CostumeInfo_Patches
	{
		internal static void InitPatch(HarmonyInstance harmony)
		{
            harmony.Patch(typeof(MPCharCtrl).GetMethod("OnClickRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(CostumeInfo_Patches), "OnClickRootPostfix", null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(CostumeInfo_Patches), "InitPostfix", null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("OnClickLoad", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(CostumeInfo_Patches), "OnClickLoadPrefix", null), null, null);
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Patch Insert Complete.");
		}

        public static CharaFileSort charaFileSort;
		private static void InitPostfix(object __instance)
		{
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Init Patch");
			charaFileSort = (CharaFileSort)__instance.GetPrivate("fileSort");
			Transform parent = charaFileSort.root.parent;

            Type ClothesKind = typeof(ChaFileDefine.ClothesKind);
            Array ClothesKindArray = Enum.GetValues(ClothesKind);

            Image panel = UILib.UIUtility.CreatePanel("TooglePanel", parent.parent.parent);
            Button btnAll = UILib.UIUtility.CreateButton("BtnAll", panel.transform, "all");
            btnAll.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll.GetComponent<Image>().color = Color.gray;
            btnAll.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 25f+20f*ClothesKindArray.Length), new Vector2(-5f, 50f+20f*(ClothesKindArray.Length)));
            btnAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            Toggle[] tgls = new Toggle[ClothesKindArray.Length+1];
            for (int i = 0; i < ClothesKindArray.Length; i++) { 
                tgls[i] = UILib.UIUtility.CreateToggle(ClothesKindArray.GetValue(i).ToString(), panel.transform, ClothesKindArray.GetValue(i).ToString());
                tgls[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tgls[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls[i].transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 20f*(ClothesKindArray.Length-i)), new Vector2(5f, 25f+20f*(ClothesKindArray.Length-i)));
                tgls[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));
            }

            tgls[ClothesKindArray.Length] = UILib.UIUtility.CreateToggle("ToggleAccessories", panel.transform, "accessories");
            tgls[ClothesKindArray.Length].GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
            tgls[ClothesKindArray.Length].GetComponentInChildren<Text>(true).color = Color.white;
            tgls[ClothesKindArray.Length].transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 0f), new Vector2(5f, 25f));
            tgls[ClothesKindArray.Length].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 1f), new Vector2(-5f, -2f));

			panel.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(407f, 285f-ClothesKindArray.Length*20), new Vector2(150f, 340f));
            panel.GetComponent<Image>().color = new Color32(80,80,80,220);

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
        }

        private static MPCharCtrl mpCharCtrl;
        public static void OnClickRootPostfix(MPCharCtrl __instance, int _idx)
        {
            if (_idx > 0 && __instance != null)
            {
                mpCharCtrl = __instance;
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Get mpCharCtrl");
            }
        }

        public static bool OnClickLoadPrefix()
        {
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Onclick Patch Start");
            if (charaFileSort == null)
            {
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Error,"[KK_SCLO] Get charaFileSort FAILED.");
            }
            CharaFileSort fileSort = charaFileSort;
            OCIChar ociChar = (OCIChar)mpCharCtrl.ociChar;

            Transform parent = fileSort.root.parent;
            Toggle[] toggleList;
            toggleList = parent.parent.parent.GetComponentsInChildren<Toggle>();

            bool flag = true;
            foreach(Toggle tgl in toggleList)
            {
                if (false == tgl.isOn)
                {
                    flag = false;
                }
            }
            if (flag)
            {
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info,"[KK_SCLO] Toggle all true, use default game function.");
                toggleList = new Toggle[0];
                return true;
            }

			ChaControl chaCtrl = ociChar.charInfo;
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Get CharaCtrl");

			ChaFileClothes clothes = chaCtrl.nowCoordinate.clothes;
			ChaFileAccessory accessories = chaCtrl.nowCoordinate.accessory;
			byte[][] arrayClothes = new byte[clothes.parts.Length][];
			for (int i = 0; i < clothes.parts.Length; i++)
			{
				arrayClothes[i] = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(clothes.parts[i]);
			}
			byte[][] arrayAcc = new byte[accessories.parts.Length][];
            for (int i = 0; i < accessories.parts.Length; i++)
            {
                arrayAcc[i] = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(accessories.parts[i]);
            }
			int[] arraySubClothes = new int[clothes.subPartsId.Length];
			for (int j = 0; j < clothes.subPartsId.Length; j++)
			{
				arraySubClothes[j] = clothes.subPartsId[j];
			}

            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] FileSort select:"+fileSort.select);
			if (fileSort.select>=0)
			{
				string fullPath = fileSort.selectPath;
                bool getToggleListSuccess = toggleList.Length > 0;
				byte[] bytes = MessagePackSerializer.Serialize<ChaFileClothes>(chaCtrl.nowCoordinate.clothes);
				byte[] bytes2 = MessagePackSerializer.Serialize<ChaFileAccessory>(chaCtrl.nowCoordinate.accessory);
				chaCtrl.nowCoordinate.LoadFile(fullPath);
                if (!getToggleListSuccess)
                {
                    chaCtrl.nowCoordinate.clothes = MessagePackSerializer.Deserialize<ChaFileClothes>(bytes);
                    chaCtrl.nowCoordinate.accessory = MessagePackSerializer.Deserialize<ChaFileAccessory>(bytes2);
                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Error,"[KK_SCLO] Getting ToggleList FAILED");
                }
                else
                {
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Loaded new clothes SUCCESS.");
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Starting to roll back origin clothes.");
                    foreach (Toggle tgl in toggleList)
                    {
                        if (!tgl.isOn)
                        {
                            switch (tgl.GetComponentInChildren<Text>(true).text)
                            {
                                case "top":
                                    chaCtrl.nowCoordinate.clothes.parts[0] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[0]);
                                    for (int k = 0; k < 3; k++)
                                    {
                                        chaCtrl.nowCoordinate.clothes.subPartsId[k] = arraySubClothes[k];
                                    }
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "bot":
                                    chaCtrl.nowCoordinate.clothes.parts[1] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[1]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "bra":
                                    chaCtrl.nowCoordinate.clothes.parts[2] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[2]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "shorts":
                                    chaCtrl.nowCoordinate.clothes.parts[3] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[3]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "gloves":
                                    chaCtrl.nowCoordinate.clothes.parts[4] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[4]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "panst":
                                    chaCtrl.nowCoordinate.clothes.parts[5] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[5]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "socks":
                                    chaCtrl.nowCoordinate.clothes.parts[6] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[6]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "shoes_inner":
                                    chaCtrl.nowCoordinate.clothes.parts[7] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[7]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "shoes_outer":
                                    chaCtrl.nowCoordinate.clothes.parts[8] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(arrayClothes[8]);
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                case "accessories":
                                    for (int i = 0; i < arrayAcc.Length; i++)
                                    {
                                        chaCtrl.nowCoordinate.accessory.parts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(arrayAcc[i]);
                                    }
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Keep:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                                default:
                                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO]  Discard Unknown Toggle:" + tgl.GetComponentInChildren<Text>(true).text);
                                    break;
                            }
                        }
                    }
                }

                chaCtrl.Reload(false, true, true, true);
                chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.chaFile.status.coordinateType);

				Utils.Sound.Play(SystemSE.ok_s);

                toggleList = new Toggle[0];
            }
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug,"[KK_SCLO] Finish.");
            return false;
        }
    }
}
