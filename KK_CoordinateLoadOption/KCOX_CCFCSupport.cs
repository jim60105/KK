using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extension;
using UnityEngine;

namespace KK_CoordinateLoadOption {
    class KCOX_CCFCSupport : CCFCSupport {
        public override string GUID => "KCOX";
        public override string ControllerName => "KoiClothesOverlayController";
        public override string CCFCName => "KCOX";

        internal static string[] MaskKind = { "BodyMask", "InnerMask", "BraMask" };
        internal static readonly string[] MainClothesKind = {
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
        internal static readonly string[] SubClothesKind = {
            "ct_top_parts_A",
            "ct_top_parts_B",
            "ct_top_parts_C"
        };

        internal new Dictionary<string, object> SourceBackup { get => base.SourceBackup.ToDictionary<string, object>(); set => base.SourceBackup = value; }
        //internal new Dictionary<string, object> TargetBackup { get => base.TargetBackup.ToDictionary<string, object>(); set => base.TargetBackup = value; }

        public KCOX_CCFCSupport(ChaControl chaCtrl) : base(chaCtrl)
            => isExist = KK_CoordinateLoadOption._isKCOXExist;

        public override bool LoadAssembly() => LoadAssembly(out _, new Version(5, 2));

        /// <summary>
        /// 由ChaControl Controller取得ExtData
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <param name="dict">Output KCOX Data Backup</param>
        /// <returns>KCOX Controller</returns>
        public override object GetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour controller = GetController(chaCtrl);

            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (null == controller) {
                Logger.LogDebug($"No KCOX Controller found on {chaCtrl.fileParam.fullname}");
            } else {
                int cnt = 0;
                foreach (string mainName in MainClothesKind) {
                    cnt += GetOverlay(ref dict, mainName) ? 1 : 0;
                }
                foreach (string subName in SubClothesKind) {
                    cnt += GetOverlay(ref dict, subName) ? 1 : 0;
                }
                foreach (string maskKind in MaskKind) {
                    cnt += GetOverlay(ref dict, maskKind) ? 1 : 0;
                }
                Logger.LogDebug("Get Overlay/Mask Total: " + cnt);
            }
            return dict;

            bool GetOverlay(ref Dictionary<string, object> dictionary, string name) {
                dictionary[name] = controller.Invoke("GetOverlayTex", new object[] { name, false });

                if (null == dictionary[name]) {
                    //Logger.LogDebug(name + " not found");
                    return false;
                } else {
                    Logger.LogDebug("->Get Overlay/Mask: " + name);
                    return true;
                }
            }
        }

        /// <summary>
        /// 拷貝KCOX資料
        /// </summary>
        /// <param name="sourceChaCtrl">拷貝來源</param>
        /// <param name="kind">Kind Name的Array位置</param>
        /// <param name="main">True:MainKind, False:SubKind, Null:MaskKind</param>
        public void CopyKCOXData(ChaControl sourceChaCtrl, int kind, bool? main = true) {
            if (sourceChaCtrl != SourceChaCtrl || DefaultChaCtrl != TargetChaCtrl) {
                if (!GetControllerAndBackupData(sourceChaCtrl, DefaultChaCtrl)) {
                    Logger.LogError("Skip on KCOX Controller not found.");
                    ClearBackup();
                    return;
                }
            }

            string name = "";
            switch (main) {
                case true:
                    name = MainClothesKind[kind];
                    switch (kind) {
                        case 0:
                            //換上衣時處理sub和BodyMask、InnerMask
                            for (int i = 0; i < 3; i++) {
                                CopyKCOXData(sourceChaCtrl, i, false);
                            }
                            for (int i = 0; i < 2; i++) {
                                CopyKCOXData(sourceChaCtrl, i, null);
                            }
                            break;
                        case 2:
                            //換胸罩時處理BraMask
                            CopyKCOXData(sourceChaCtrl, 2, null);
                            break;
                    }
                    break;
                case false:
                    name = SubClothesKind[kind];
                    break;
                case null:
                    name = MaskKind[kind];
                    break;
            }

            bool exist = SourceBackup.TryGetValue(name, out object tex);

            ChaFileDefine.CoordinateType coordinateType = (ChaFileDefine.CoordinateType)TargetChaCtrl.fileStatus.coordinateType;
            Dictionary<ChaFileDefine.CoordinateType, object> _allOverlayTextures = TargetController.GetField("_allOverlayTextures").ToDictionary<ChaFileDefine.CoordinateType, object>();
            if (TargetController.GetProperty("CurrentOverlayTextures").ToDictionary<string, object>().TryGetValue(name, out object clothesTexData)) {
                if (!exist || tex == null || (bool)tex.Invoke("IsEmpty")) {
                    clothesTexData.Invoke("Clear");
                    clothesTexData.SetField("Override", false);
                    _allOverlayTextures[coordinateType].Invoke("Remove", new object[] { name });
                    Logger.LogDebug($"->Clear Overlay/Mask: {name}");
                } else {
                    clothesTexData.SetProperty("Texture", tex.GetProperty("Texture"));
                    if (null != tex.GetField("Override")) {
                        clothesTexData.SetField("Override", tex.GetField("Override"));
                    }
                    Logger.LogDebug($"->Replace Overlay/Mask: {name}");
                }
            } else {
                if (exist && tex != null && !(bool)tex.Invoke("IsEmpty")) {
                    _allOverlayTextures[coordinateType].Invoke("Add", new object[] { name, tex });
                    Logger.LogDebug($"->Add Overlay/Mask: {name}");
                }
            }
        }

        /// <summary>
        /// 重新整理Overlay，Mask
        /// </summary>
        public IEnumerator Update() => Update(DefaultChaCtrl);
        public IEnumerator Update(ChaControl chaCtrl) {
            yield return null;
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("RefreshAllTextures", new object[] { false });
        }

        public override bool CheckControllerPrepared(ChaControl chaCtrl)
         => base.CheckControllerPrepared(
             chaCtrl,
             (controller) => null != controller.GetProperty("CurrentOverlayTextures"));
    }
}
