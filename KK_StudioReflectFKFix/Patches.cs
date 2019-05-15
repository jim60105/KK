using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;

using Extension;

using Harmony;

using Studio;

using UnityEngine.UI;

using Logger = BepInEx.Logger;

namespace KK_StudioReflectFKFix
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(MPCharCtrl).GetMethod("Awake", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(AwakePostfix), null), null);
            //harmony.Patch(typeof(MPCharCtrl).GetMethod("Awake", AccessTools.all), null, null, new HarmonyMethod(typeof(Patches), nameof(AwakeTranspiler), null));
        }

        private static void AwakePostfix(MPCharCtrl __instance)
        {
            ((Button)__instance.GetPrivate("ikInfo").GetPrivate("buttonReflectFK")).onClick.RemoveAllListeners();
            ((Button)__instance.GetPrivate("ikInfo").GetPrivate("buttonReflectFK")).onClick.AddListener(delegate ()
            {
                //__instance.CopyBoneFK((OIBoneInfo.BoneGroup)353);
                typeof(MPCharCtrl).InvokeMember("CopyBoneFK", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, __instance, new object[] { (OIBoneInfo.BoneGroup)1 });
            });
            Logger.Log(LogLevel.Debug, "[KK_SRFF] Copy Function Rewrite");
        }

        //private static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = new List<CodeInstruction>(instructions);
        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        Logger.Log(LogLevel.Debug, $"[KK_SRFF] {codes[i].opcode.ToString()}");
        //        if (codes[i].opcode == OpCodes.Ldc_I4)
        //        {
        //            codes[i].operand = "1";
        //            break;
        //        }
        //    }
        //    Logger.Log(LogLevel.Debug, "[KK_SRFF] Copy Function Rewrite");
        //    return codes.AsEnumerable();
        //}

    }
}
