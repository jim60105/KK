using CoordinateLoadOption.OtherPlugin;
using CoordinateLoadOption.OtherPlugin.CharaCustomFunctionController;
using Extension;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoordinateLoadOption
{
    using CLO = CoordinateLoadOption;
    partial class CoordinateLoad
    {
        public static void ChangeAccessories(ChaControl sourceChaCtrl, ChaFileAccessory.PartsInfo[] sourceParts, ChaControl targetChaCtrl, ref ChaFileAccessory.PartsInfo[] targetParts)
        {
            Queue<int> accQueue = new Queue<int>();
            int accCount = !CLO._isMoreAccessoriesExist
                            ? 20
                            : addAccModeFlag
                                ? Math.Max(targetParts.Length, targetParts.Count(p => p.type != 120) + Patches.tgls2.Count(p => p.isOn))
                                : Math.Max(sourceParts.Length, targetParts.Length);
            ChaFileAccessory.PartsInfo[] tmpArr = new ChaFileAccessory.PartsInfo[accCount];
            targetParts.CopyTo(tmpArr, 0);
            tmpArr = tmpArr.Select(p => p ?? new ChaFileAccessory.PartsInfo()).ToArray();

            bool isAllFalseFlag = true;
            foreach (bool b in Patches.tgls2.Select(x => x.isOn).ToArray())
            {
                isAllFalseFlag &= !b;
            }
            if (isAllFalseFlag)
            {
                Logger.LogDebug("Load Accessories All False");
                Logger.LogDebug("Load Accessories Finish");
                return;
            }
            Logger.LogDebug($"Acc Count : {Patches.tgls2.Length}");

            MaterialEditor me = new MaterialEditor(targetChaCtrl);

            for (int i = 0; i < tmpArr.Length && i < Patches.tgls2.Length; i++)
            {
                // 飾品綁定模式，一律Change
                if (Patches.boundAcc)
                {
                    DoChangeAccessory(i, i);
                    continue;
                }

                // 如果沒勾選就不Change
                if (!(bool)Patches.tgls2[i]?.isOn)
                    continue;

                // 增加模式
                if (addAccModeFlag)
                {
                    // 空欄
                    if (tmpArr[i].type == 120)
                    {
                        DoChangeAccessory(i, i);
                    }
                    else
                    {
                        EnQueue(i, sourceParts[i], tmpArr[i], accQueue);
                    }
                    continue;
                }

                // 如果是髮飾品，且啟用鎖定髮飾品則queue
                if (Patches.lockHairAcc && IsHairAccessory(targetChaCtrl, i))
                {
                    EnQueue(i, sourceParts[i], tmpArr[i], accQueue);
                    continue;
                }

                DoChangeAccessory(i, i);
            }

            //遍歷空欄dequeue accQueue
            for (int j = 0; j < tmpArr.Length && accQueue.Count > 0; j++)
            {
                if (tmpArr[j].type == 120)
                {
                    int slot = accQueue.Dequeue();
                    DoChangeAccessory(slot, j);
                    Logger.LogDebug($"->DeQueue: Acc{j} / Part: {(ChaListDefine.CategoryNo)tmpArr[j].type} / ID: {tmpArr[j].id}");
                } //else continue;
            }

            targetParts = tmpArr;

            //accQueue內容物太多，報告後捨棄
            while (accQueue.Count > 0)
            {
                int slot = accQueue.Dequeue();
                Logger.LogMessage("Accessories slot is not enough! Discard " + Helper.GetNameFromIDAndType(sourceParts[slot].id, (ChaListDefine.CategoryNo)sourceParts[slot].type));
            }

            void EnQueue(int i, ChaFileAccessory.PartsInfo sourcePartsInfo, ChaFileAccessory.PartsInfo targetPartsInfo, Queue<int> queue)
            {
                if (sourcePartsInfo?.type == 120)
                {
                    Logger.LogDebug($"->Lock: Acc{i} / Part: {(ChaListDefine.CategoryNo)targetPartsInfo.type} / ID: {targetPartsInfo.id}");
                    Logger.LogDebug($"->Pass: Acc{i} / Part: {(ChaListDefine.CategoryNo)sourcePartsInfo.type} / ID: {sourcePartsInfo.id}");
                }
                else
                {
                    Logger.LogDebug($"->Lock: Acc{i} / Part: {(ChaListDefine.CategoryNo)targetPartsInfo.type} / ID: {targetPartsInfo.id}");
                    Logger.LogDebug($"->EnQueue: Acc{i} / Part: {(ChaListDefine.CategoryNo)sourcePartsInfo.type} / ID: {sourcePartsInfo.id}");
                    queue.Enqueue(i);
                }
            }

            /// <summary>
            /// 換飾品
            /// </summary>
            /// <param name="sourceSlot">來源slot</param>
            /// <param name="targetSlot">目標slot</param>
            void DoChangeAccessory(int sourceSlot, int targetSlot)
            {
                //來源目標都空著就跳過
                if (sourceParts[sourceSlot].type == 120 && tmpArr[targetSlot].type == 120)
                {
                    Logger.LogDebug($"->BothEmpty: SourceAcc{sourceSlot}, TargetAcc{targetSlot}");
                    return;
                }

                if (me.isExist)
                {
                    me.RemoveMaterialEditorData(targetSlot, targetChaCtrl.GetAccessoryComponent(targetSlot)?.gameObject, MaterialEditor.ObjectType.Accessory);
                }

                byte[] tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(sourceParts[sourceSlot]);
                tmpArr[targetSlot] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);

                if (hairacc.isExist)
                {
                    hairacc.CopyHairAcc(sourceChaCtrl, sourceSlot, targetChaCtrl, targetSlot);
                }

                if (me.isExist)
                {
                    me.CopyMaterialEditorData(sourceChaCtrl, sourceSlot, targetSlot, sourceChaCtrl.GetAccessoryComponent(sourceSlot)?.gameObject, MaterialEditor.ObjectType.Accessory);
                }
                Logger.LogDebug($"->Changed: Acc{targetSlot} / Part: {(ChaListDefine.CategoryNo)tmpArr[targetSlot].type} / ID: {tmpArr[targetSlot].id}");
            }
        }

        public static void ClearAccessories(ChaControl chaCtrl)
        {
            for (int i = 0; i < chaCtrl.nowCoordinate.accessory.parts.Length; i++)
            {
                if (!Patches.boundAcc
                    && Patches.lockHairAcc
                    && IsHairAccessory(chaCtrl, i))
                {
                    Logger.LogDebug($"Keep HairAcc{i}: {chaCtrl.nowCoordinate.accessory.parts[i].id}");
                    continue;
                }

                chaCtrl.nowCoordinate.accessory.parts[i] = new ChaFileAccessory.PartsInfo();
            }

            if (CLO._isMoreAccessoriesExist)
                MoreAccessories.Update();

            chaCtrl.ChangeAccessory(true);

            //chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
            byte[] data = chaCtrl.nowCoordinate.SaveBytes();
            chaCtrl.chaFile.coordinate[chaCtrl.chaFile.status.coordinateType].LoadBytes(data, chaCtrl.nowCoordinate.loadVersion);

            chaCtrl.ChangeCoordinateTypeAndReload(false);
        }

        /// <summary>
        /// 檢查是否為頭髮飾品
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static bool IsHairAccessory(ChaControl chaCtrl, int index)
            => null != chaCtrl.GetAccessoryComponent(index)?.gameObject.GetComponent<ChaCustomHairComponent>();
    }
}
