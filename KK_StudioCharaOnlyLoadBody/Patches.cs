using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Extension;
using Harmony;
using MessagePack;
using Studio;

using UILib;

using UnityEngine;
using UnityEngine.UI;

using Logger = BepInEx.Logger;

namespace KK_StudioCharaOnlyLoadBody
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(CharaList).GetMethod("InitCharaList", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(InitCharaListPostfix), null), null);
            harmony.Patch(typeof(ChaFile).GetMethod("SetCoordinateBytes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(Patches), nameof(SetCoordinateBytesPostfix), null), null);

            //Change the interactable of the button
            harmony.Patch(typeof(CharaList).GetMethod("OnDelete", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDeselect", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelect", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelectChara", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSort", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
        }

        private static GameObject[] btn = new GameObject[2];
        public static void InitCharaListPostfix(CharaList __instance)
        {
            var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/"+__instance.name+"/Button Change");
            int i = (String.Equals(__instance.name, "00_Female") ? 1 : 0);
            btn[i] = UnityEngine.Object.Instantiate(original,original.transform.parent);
            btn[i].name = "Button Keep Coordinate Change";
            btn[i].transform.position += new Vector3(0, -25, 0);
            btn[i].transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(179, -401), new Vector2(390, -379.5f));
            btn[i].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaOnlyLoadBody.Resources.buttonChange.png", 183, 18);

            btn[i].GetComponent<Button>().onClick.RemoveAllListeners();
            btn[i].GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            btn[i].GetComponent<Button>().onClick.AddListener(()=> { BackupCoordinates(__instance.name); });
            SetKeepCoorButtonInteractable(__instance);
        }

        private static void SetKeepCoorButtonInteractable(CharaList __instance)
        {
            int i = (String.Equals(__instance.name, "00_Female") ? 1 : 0);
            btn[i].GetComponent<Button>().interactable = ((Button)__instance.GetPrivate("buttonChange")).interactable;
        }

        public static List<byte[]> backupCoordinates;
        public static void BackupCoordinates(string charaListName)
        {
            TreeNodeObject selectNode = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode;
            if (selectNode != null && Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNode, out ObjectCtrlInfo objectCtrlInfo))
            {
                ChaControl chaCtrl = (objectCtrlInfo as OCIChar).charInfo;
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Get ChaCtrl");

                backupCoordinates = new List<byte[]>();
                foreach(var cor in chaCtrl.chaFile.coordinate)
                {
                    backupCoordinates.Add(cor.SaveBytes());
                }
                if (null != backupCoordinates && backupCoordinates.Count!=0)
                {
                    Logger.Log(LogLevel.Debug, "[KK_SCOLB] Backup Coordinates Finish");
                    var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/" + charaListName + "/Button Change");
                    original.GetComponent<Button>().onClick.Invoke();
                }
                else
                {
                    Logger.Log(LogLevel.Error, "[KK_SCOLB] Backup Coordinates FAILED");
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SCOLB] Get chaCtrl FAILED");
            }
        }

        public static void SetCoordinateBytesPostfix(ChaFile __instance)
        {
            if (null == backupCoordinates || backupCoordinates.Count==0)
            {
                return ;
            }
            if (__instance.coordinate.Length != backupCoordinates.Count)
            {
                Logger.Log(LogLevel.Error, "[KK_SCOLB] BackupData Not Correct!");
                Logger.Log(LogLevel.Error, "[KK_SCOLB] Load original Coordinates!");
                return ;
            }
            bool success = true;
            for(int i = 0;i<__instance.coordinate.Length;i++)
            {
                success &= __instance.coordinate[i].LoadBytes(backupCoordinates[i], ChaFileDefine.ChaFileCoordinateVersion);
            }
            if (success)
            {
                Logger.Log(LogLevel.Info, "[KK_SCOLB] Rollback Coordinates Finish");
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SCOLB] Rollback Coordinates FAILED");
            }
            backupCoordinates = null;
            return ;
        }
    }
}
