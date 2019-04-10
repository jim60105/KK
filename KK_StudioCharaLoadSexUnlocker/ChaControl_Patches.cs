using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;

namespace KK_StudioCharaLoadSexUnlocker
{
    class ChaControl_Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(ChaControl).GetMethod("Reload", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(ChaControl_Patches), nameof(ReloadPostfix), null), null);
        }

        //I get these codes from Uncensor Selector
        //Thanks the authors from Discord Koikatsu! Group

        //Reload character body
        private static void ReloadPostfix(object __instance)
        {
            ChaControl chaControl = (ChaControl)__instance;
            while (chaControl.objBody == null)
            {
                return;
            }

            //Copy Bones
            string Asset = (chaControl.sex == 0 ? "p_cm_body_00" : "p_cf_body_00");
            if (chaControl.hiPoly == false)
                Asset += "_low";
            GameObject uncensorCopy = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", Asset, true);
            foreach (var mesh in chaControl.objBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (mesh.name == "o_body_a")
                {
                    SkinnedMeshRenderer src = uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name);
                    SkinnedMeshRenderer dst = mesh;

                    if (src == null || dst == null)
                        return;

                    dst.sharedMesh = src.sharedMesh;

                    List<Transform> newBones = new List<Transform>();
                    foreach (Transform t in src.bones)
                        newBones.Add(Array.Find(dst.bones, c => c?.name == t?.name));
                    dst.bones = newBones.ToArray();
                }
            }
            UnityEngine.Object.Destroy(uncensorCopy);

            Traverse.Create(chaControl).Method("UpdateSiru", new object[] { true }).GetValue();

            //Update Bust
            chaControl.updateBustSize = true;
            if (chaControl.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
            {
                bustNormal.Release();
            }
            bustNormal = new BustNormal();
            bustNormal.Init(chaControl.objBody, "chara/oo_base.unity3d", "p_cf_body_00_Nml", string.Empty);
            chaControl.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
        }
    }
}
