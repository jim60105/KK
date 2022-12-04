using BepInEx.Logging;

namespace CoordinateLoadOption
{
    public class Helper
    {
        private static readonly ManualLogSource Logger = CoordinateLoadOption.Logger;

        /// <summary>
        /// 傳入ID和Type查詢名稱
        /// </summary>
        /// <param name="id">查詢ID</param>
        /// <param name="type">查詢Type</param>
        /// <returns>名稱</returns>
        public static string GetNameFromIDAndType(int id, ChaListDefine.CategoryNo type)
        {
            ChaListControl chaListControl = Manager.Character.chaListCtrl;
            Logger.LogDebug($"Find Accessory id / type: {id} / {type}");

            string name = "";
            if (type == (ChaListDefine.CategoryNo)120)
            {
                name = StringResources.StringResourcesManager.GetString("empty");
            }
            if (null == name || "" == name)
            {
                name = chaListControl.GetListInfo(type, id)?.Name;
            }
            if (null == name || "" == name)
            {
                name = StringResources.StringResourcesManager.GetString("unreconized");
            }

            return name;
        }

        public static void PrintAccStatus(ChaFileAccessory.PartsInfo[] chaCtrlAccParts, string flag = "")
        {
#if DEBUG
            for (int i = 0; i < chaCtrlAccParts.Length; i++)
            {
                Logger.LogDebug($"*{flag}* Acc{i} / Part: {(ChaListDefine.CategoryNo)chaCtrlAccParts[i].type} / ID: {chaCtrlAccParts[i].id}");
            }
#endif
        }

        public static void PrintClothStatus(ChaFileClothes chaFileClothes, string flag = "")
        {
#if DEBUG
            for (int i = 0; i < chaFileClothes.parts.Length; i++)
            {
                Logger.LogDebug($"*{flag}* Cloth{i} / ID: {chaFileClothes.parts[i].id}");
                if (i == 0)
                {
                    Logger.LogDebug($"*{flag}* ClothSub0 / ID: {chaFileClothes.subPartsId[0]}");
                    Logger.LogDebug($"*{flag}* ClothSub1 / ID: {chaFileClothes.subPartsId[1]}");
                    Logger.LogDebug($"*{flag}* ClothSub2 / ID: {chaFileClothes.subPartsId[2]}");
                }
            }
#endif
        }
    }
}
