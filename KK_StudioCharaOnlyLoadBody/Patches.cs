using BepInEx.Logging;
using Extension;
using Harmony;
using Manager;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;
using UILib;

namespace KK_StudioCharaOnlyLoadBody
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            //harmony.Patch(typeof(MPCharCtrl).GetNestedType("OtherInfo", BindingFlags.Public).GetMethod("UpdateInfo", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(UpdateInfoPostfix), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("InitCharaList", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(InitCharaListPostfix), null), null);

                ////Embed dll
                //AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                //{
                //    String resourceName = typeof(Patches).Namespace + "." +
                //       new AssemblyName(args.Name).Name + ".dll";
                //    Logger.Log(LogLevel.Debug, "[KK_SCOLB] Try to load Assembly dll file: " + resourceName);
                //    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                //    {
                //        Byte[] assemblyData = new Byte[stream.Length];
                //        stream.Read(assemblyData, 0, assemblyData.Length);
                //        return Assembly.Load(assemblyData);
                //    }
                //};
        }

        public static void InitCharaListPostfix(CharaList __instance)
        {
            //((Button)__instaince.GetPrivate("ButtonLoad")).gameObject.

            var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/00_Female/Button Change");
            Button btn = UIUtility.CreateButton("Button Keep Coordinate Change", original.transform.parent, "Keep Coor Change");
            btn.transform.SetRect(original.transform);
            btn.GetComponentInChildren<Text>(true).color = Color.white;
            btn.GetComponentInChildren<Text>(true).font = UILib.UIUtility.defaultFont;
            btn.GetComponent<Image>().color = Color.gray;
            btn.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));
            btn.transform.position += new Vector3(0, -25, 0);

            //var copy = UnityEngine.Object.Instantiate(original.transform, original.transform.parent, true);
            //copy.name = "Button Load Keep Clothes";
            //copy.transform.position += new Vector3(0, -25, 0);

        }

    }
}
