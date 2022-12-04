using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using CoordinateLoadOption.OtherPlugin;
using CoordinateLoadOption.OtherPlugin.CharaCustomFunctionController;
using ExtensibleSaveFormat;
using Extension;
using HarmonyLib;
using MessagePack;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoordinateLoadOption
{
    using CLO = CoordinateLoadOption;

    class Patches
    {
        private static readonly ManualLogSource Logger = CLO.Logger;
        private static CustomFileWindow CustomFileWindow;
        internal static string coordinatePath;

        public static string[] ClothesKindName;

        internal static Toggle[] tgls;
        internal static Toggle[] tgls2 = new Toggle[0]; //使用時再初始化
        internal static Image panel;
        private static Image panel2;
        private static RectTransform toggleGroup;
        internal static bool lockHairAcc = false;
        internal static bool[] charaOverlay = new bool[] { true, true, true };  //順序: Iris、Face、Body
        internal static bool readABMX = true;
        internal static bool boundAcc = false;

        public static void InitPostfix(object __instance)
        {
            //如果啟動機翻就一律顯示英文，否則機翻會毀了我的文字
            if (null != KoikatuHelper.TryGetPluginInstance("gravydevsupreme.xunity.autotranslator"))
            {
                string XUAConfigPath = Path.Combine(Paths.ConfigPath, "AutoTranslatorConfig.ini");
                if (File.ReadLines(XUAConfigPath).Any(l => l.ToLower().Contains("enableugui=true")))
                {
                    _ = StringResources.StringResourcesManager.SetUICulture("en-US");
                    Logger.LogInfo("Found XUnityAutoTranslator Enabled, load English UI");
                }
            }

            ClothesKindName = new string[]{
                StringResources.StringResourcesManager.GetString("ClothesKind_top"),
                StringResources.StringResourcesManager.GetString("ClothesKind_bot"),
                StringResources.StringResourcesManager.GetString("ClothesKind_bra"),
                StringResources.StringResourcesManager.GetString("ClothesKind_shorts"),
                StringResources.StringResourcesManager.GetString("ClothesKind_gloves"),
                StringResources.StringResourcesManager.GetString("ClothesKind_panst"),
                StringResources.StringResourcesManager.GetString("ClothesKind_socks"),
                StringResources.StringResourcesManager.GetString("ClothesKind_shoes_inner"),
                StringResources.StringResourcesManager.GetString("ClothesKind_shoes_outer"),
                StringResources.StringResourcesManager.GetString("ClothesKind_accessories")
            };

            DrawUI(__instance);

            //Block Maker Load Button
            if (!CLO.insideStudio)
            {
                Button btnLoad = (__instance.GetField("fileWindow") as CustomFileWindow).btnCoordeLoadLoad;
                btnLoad.onClick.RemoveAllListeners();
                btnLoad.onClick.AddListener(OnClickLoadPostfix);
            }
        }

        #region UI
        private static void DrawUI(object __instance)
        {
            Array ClothesKindArray = Enum.GetValues(typeof(CLO.ClothesKind));

            Transform PanelParent = null;
            if (CLO.insideStudio)
            {
                PanelParent = (__instance.GetField("fileSort") as CharaFileSort).root.parent.parent.parent;
            }
            else
            {
                //Maker
                PanelParent = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad").transform;
            }

            //Show Selection Btn
            Image coordianteLoadPanel = UIUtility.CreatePanel("CoordinateLoadPanel", PanelParent);
            coordianteLoadPanel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);
            Button btnCoordinateLoadOption = UIUtility.CreateButton("CoordinateLoadBtn", coordianteLoadPanel.transform, StringResources.StringResourcesManager.GetString("showSelection") + (CoordinateLoadOption.insideStudio ? "" : ">>"));
            btnCoordinateLoadOption.GetComponentInChildren<Text>(true).color = Color.white;
            btnCoordinateLoadOption.GetComponent<Image>().color = Color.gray;
            btnCoordinateLoadOption.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -35f), new Vector2(-5f, -5f));
            btnCoordinateLoadOption.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Panel
            panel = UIUtility.CreatePanel("CoordinateTooglePanel", PanelParent);
            panel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

            //Draw ButtonAll
            Button btnAll = UIUtility.CreateButton("BtnAll", panel.transform, "All");
            btnAll.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll.GetComponent<Image>().color = Color.gray;
            btnAll.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -30), new Vector2(-5f, -5f));
            btnAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Toggles
            tgls = new Toggle[ClothesKindArray.Length];
            for (int i = 0; i < ClothesKindArray.Length - 1; i++)
            {
                tgls[i] = UIUtility.CreateToggle(ClothesKindArray.GetValue(i).ToString(), panel.transform, ClothesKindName.GetValue(i).ToString());
                tgls[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleLeft;
                tgls[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls[i].transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -60f - 25f * i), new Vector2(-5f, -35f - 25f * i));
                tgls[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            }

            //分隔線
            Image line = UIUtility.CreateImage("line", panel.transform);
            line.color = Color.gray;
            line.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -262.5f), new Vector2(-5f, -261f));

            //AccToggle
            tgls[9] = UIUtility.CreateToggle(ClothesKindArray.GetValue(9).ToString(), panel.transform, ClothesKindName.GetValue(9).ToString());
            tgls[9].GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleLeft;
            tgls[9].GetComponentInChildren<Text>(true).color = Color.white;
            tgls[9].transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -292.5f), new Vector2(-5f, -267.5f));
            tgls[9].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            tgls[9].onValueChanged.AddListener((x) =>
            {
                if (tgls2.Length != 0 && null != panel2)
                {
                    panel2.gameObject.SetActive(x);
                }
            });
            List<Toggle> toggleList = tgls.ToList();

            //清空飾品btn
            Button btnClearAcc = UIUtility.CreateButton("BtnClearAcc", panel.transform, StringResources.StringResourcesManager.GetString("clearAccWord"));
            btnClearAcc.GetComponentInChildren<Text>(true).color = Color.white;
            btnClearAcc.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperCenter;
            btnClearAcc.GetComponent<Image>().color = Color.gray;
            btnClearAcc.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -317.5f), new Vector2(-5f, -292.5f));
            btnClearAcc.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            float baseY = -322;

            if (CLO._isABMXExist)
            {
                //分隔線
                Image line3 = UIUtility.CreateImage("line", panel.transform);
                line3.color = Color.gray;
                line3.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, baseY - 2f), new Vector2(-5f, baseY - 0.5f));

                //Chara Overlay toggle
                Toggle tglReadABMX = UIUtility.CreateToggle("TglReadABMX", panel.transform, StringResources.StringResourcesManager.GetString("readABMX"));
                tglReadABMX.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglReadABMX.GetComponentInChildren<Text>(true).color = Color.white;
                tglReadABMX.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, baseY - 32f), new Vector2(-5f, baseY - 6.5f));
                tglReadABMX.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                baseY -= 32f;

                tglReadABMX.onValueChanged.AddListener((x) =>
                {
                    readABMX = x;
                });
                tglReadABMX.isOn = true;
                toggleList.Add(tglReadABMX);
            }

            //Draw accessories panel
            panel2 = UIUtility.CreatePanel("AccessoriesTooglePanel", panel.transform);
            panel2.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

            Button btnAll2 = UIUtility.CreateButton("BtnAll2", panel2.transform, "All");
            btnAll2.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll2.GetComponent<Image>().color = Color.gray;
            btnAll2.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -30), new Vector2(-5f, -5f));
            btnAll2.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //反選髮飾品Btn
            Button btnReverseHairAcc = UIUtility.CreateButton("BtnReverseHairAcc", panel2.transform, StringResources.StringResourcesManager.GetString("reverseHairAcc"));
            btnReverseHairAcc.GetComponentInChildren<Text>(true).color = Color.white;
            btnReverseHairAcc.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperCenter;
            btnReverseHairAcc.GetComponent<Image>().color = Color.gray;
            btnReverseHairAcc.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -55f), new Vector2(-5f, -30f));
            btnReverseHairAcc.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));
            if (!CLO._isHairAccessoryCustomizerExist) btnReverseHairAcc.interactable = false;

            //飾品載入模式btn
            Button btnChangeAccLoadMode = UIUtility.CreateButton("BtnChangeAccLoadMode", panel2.transform, "AccModeBtn");
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).color = Color.white;
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleCenter;
            btnChangeAccLoadMode.GetComponent<Image>().color = Color.gray;
            btnChangeAccLoadMode.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -80), new Vector2(-5f, -55f));
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));
            panel2.gameObject.SetActive(false);

            //Lock頭髮飾品toggle
            Toggle tglHair = UIUtility.CreateToggle("lockHairAcc", panel2.transform, StringResources.StringResourcesManager.GetString("lockHairAcc"));
            tglHair.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
            tglHair.GetComponentInChildren<Text>(true).color = Color.yellow;
            tglHair.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -110f), new Vector2(-5f, -85f));
            tglHair.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            tglHair.isOn = lockHairAcc;
            tglHair.onValueChanged.AddListener((x) =>
            {
                lockHairAcc = x;
            });

            //滾動元件
            Scroller();

            //選項盤位置
            PanelPosition();

            panel.gameObject.SetActive(false);

            //拖曳event
            DragEvent();

            //Button邏輯
            ButtonLogics();

            //Logger.LogDebug("Draw UI Finish");

            void ButtonLogics()
            {

                btnCoordinateLoadOption.onClick.RemoveAllListeners();
                btnCoordinateLoadOption.onClick.AddListener(() =>
                {
                    if (null != panel)
                    {
                        bool active = !panel.IsActive();
                        panel.gameObject.SetActive(active);

                        //Maker: 修改服裝選擇器下方的勾選列
                        if (!CLO.insideStudio)
                        {
                            GameObject stackLoadPanel = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad/Select");
                            stackLoadPanel.GetComponentsInChildren<Toggle>().ToList().ForEach(tgl => tgl.isOn = true);  //全勾上tmpChara才能完整載入
                            stackLoadPanel.SetActive(!active);

                            //在CoordinateLoad OnEnable時改變ListArea長度
                            GameObject go = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad");
                            if (null == go.GetComponent(typeof(OnEnableListener)))
                            {
                                OnEnableListener onEnableListener = (OnEnableListener)go.AddComponent(typeof(OnEnableListener));
                                onEnableListener.OnEnableEvent += delegate
                                {
                                    GameObject listView = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/ListArea");
                                    listView.transform.SetRect(Vector2.up, Vector2.up, new Vector2(4, Patches.panel.IsActive() ? -880 : -834), new Vector2(772, -72));
                                    //Logger.LogDebug("OnEnable: " + Patches.panel.IsActive());
                                };
                            }
                            go.SetActive(false);
                            go.SetActive(true);
                        }
                    }
                });

                btnAll.onClick.RemoveAllListeners();
                btnAll.onClick.AddListener(() =>
                {
                    if (toggleList.All(x => x.isOn == true))
                    {
                        foreach (Toggle x in toggleList) { x.isOn = false; }
                    }
                    else
                    {
                        foreach (Toggle x in toggleList) { x.isOn = true; }
                    }
                });
                btnAll2.onClick.RemoveAllListeners();
                btnAll2.onClick.AddListener(() =>
                {
                    if (tgls2.All(x => x.isOn == true))
                    {
                        foreach (Toggle x in tgls2) { x.isOn = false; }
                    }
                    else
                    {
                        foreach (Toggle x in tgls2) { x.isOn = true; }
                    }
                });
                btnClearAcc.onClick.RemoveAllListeners();
                btnClearAcc.onClick.AddListener(() =>
                {
                    if (CLO.insideStudio)
                    {
                        IEnumerable<OCIChar> array = Singleton<GuideObjectManager>.Instance.selectObjectKey
                            .Select(p => (OCIChar)Studio.Studio.GetCtrlInfo(p))
                            .Where(p => null != p);
                        foreach (OCIChar ocichar in array)
                        {
                            CoordinateLoad.ClearAccessories(ocichar.charInfo);
                        }
                    }
                    else
                    {
                        CoordinateLoad.ClearAccessories(Singleton<CustomBase>.Instance.chaCtrl);
                        Singleton<CustomBase>.Instance.updateCustomUI = true;
                    }

                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                    Logger.LogDebug("Clear accessories Finish");
                });
                btnReverseHairAcc.onClick.RemoveAllListeners();
                if (CLO._isHairAccessoryCustomizerExist)
                {
                    btnReverseHairAcc.onClick.AddListener(() =>
                    {
                        CoordinateLoad.MakeTmpChara((_) =>
                        {
                            CoordinateLoad.tmpChaCtrl.StopAllCoroutines();
                            for (int i = 0; i < CoordinateLoad.tmpChaCtrl.nowCoordinate.accessory.parts.Length; i++)
                            {
                                if (i < tgls2.Length)
                                {
                                    if (CoordinateLoad.IsHairAccessory(CoordinateLoad.tmpChaCtrl, i))
                                    {
                                        tgls2[i].isOn = !tgls2[i].isOn;
                                        Logger.LogDebug($"Reverse Hair Acc.: {i}");
                                    }
                                }
                            }
                            Manager.Character.DeleteChara(CoordinateLoad.tmpChaCtrl);
                            CoordinateLoad.tmpChaCtrl = null;
                            Logger.LogDebug($"Delete Temp Chara");
                            Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                            Logger.LogDebug("Reverse Hair Acc. toggles.");
                        });
                    });
                }

                btnChangeAccLoadMode.onClick.RemoveAllListeners();
                btnChangeAccLoadMode.onClick.AddListener(() =>
                {
                    CoordinateLoad.addAccModeFlag = !CoordinateLoad.addAccModeFlag;
                    btnChangeAccLoadMode.GetComponentInChildren<Text>().text =
                        CoordinateLoad.addAccModeFlag
                            ? StringResources.StringResourcesManager.GetString("addMode")
                            : StringResources.StringResourcesManager.GetString("replaceMode");
                    Logger.LogDebug("Set add accessories mode to " + (CoordinateLoad.addAccModeFlag ? "add" : "replace") + " mode");
                });
                CoordinateLoad.addAccModeFlag = true;
                btnChangeAccLoadMode.onClick.Invoke();
            }

            void DragEvent()
            {
                Vector2 mouse = Vector2.zero;
                EventTrigger trigger = panel.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
                entry.callback.AddListener((data) =>
                {
                    mouse = new Vector2(Input.mousePosition.x - panel.transform.localPosition.x, Input.mousePosition.y - panel.transform.localPosition.y);
                });
                EventTrigger.Entry entry2 = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                entry2.callback.AddListener((data) =>
                {
                    if (CLO.insideStudio)
                    {
                        CLO.Studio_Panel_Position.Value = new Vector3(Input.mousePosition.x - mouse.x, Input.mousePosition.y - mouse.y, 0);
                    }
                    else
                    {
                        CLO.Maker_Panel_Position.Value = new Vector3(Input.mousePosition.x - mouse.x, Input.mousePosition.y - mouse.y, 0);
                    }
                });
                trigger.triggers.Add(entry);
                trigger.triggers.Add(entry2);
            }

            void PanelPosition()
            {
                if (CLO.insideStudio)
                {
                    coordianteLoadPanel.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(170f, -45f), new Vector2(0, -345f));
                    panel.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(170f, (baseY - 7.5f) - 40), new Vector2(-52, -345 - 40));
                    _SetPanel(CLO.Studio_Panel_Position);
                    panel2.transform.SetRect(Vector2.one, Vector2.one, new Vector2(1f, -612.5f), new Vector2(200f, 0f));
                }
                else
                {
                    coordianteLoadPanel.transform.SetRect(Vector2.zero, Vector2.zero, new Vector2(596, 1), new Vector2(766, 41));
                    panel.transform.SetRect(Vector2.zero, Vector2.zero, new Vector2(777, -8), new Vector2(947, -7.5f - (baseY - 2.5f)));
                    _SetPanel(CLO.Maker_Panel_Position);
                    panel2.transform.SetRect(Vector2.right, Vector2.right, new Vector2(1, 0), new Vector2(200f, 612.5f));
                }
                void _SetPanel(ConfigEntry<Vector3> c)
                {
                    CLO.defaultPanelPosition = panel.transform.localPosition;
                    if (c.Value == Vector3.zero) c.Value = CLO.defaultPanelPosition;
                    panel.transform.localPosition = c.Value;
                }
            }

            void Scroller()
            {
                ScrollRect scrollRect = UIUtility.CreateScrollView("scroll", panel2.transform);
                toggleGroup = scrollRect.content;
                scrollRect.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(0f, 5f), new Vector2(0, -110f));
                scrollRect.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
                foreach (Image img in scrollRect.verticalScrollbar.GetComponentsInChildren<Image>())
                {
                    img.color = Color.Lerp(img.color, Color.black, 0.6f);
                }
                (scrollRect.verticalScrollbar.transform as RectTransform).offsetMin = new Vector2(-16f, 0);
                scrollRect.scrollSensitivity = 30;
            }
        }
        #endregion

        //選擇衣裝
        internal static void OnSelectPostfix(object __instance)
        {
            if (null == panel2)
            {
                return;
            }
            if (CLO.insideStudio)
            {
                coordinatePath = (__instance.GetField("fileSort") as CharaFileSort)?.selectPath ?? "";
            }
            else
            {
                CustomFileWindow = (CustomFileWindow)__instance.GetField("fileWindow");
                object customFileInfoComponent = __instance.GetField("listCtrl")?.Invoke("GetSelectTopItem");

                //OnDeslect in Maker
                if (null == customFileInfoComponent) return;

                if (null != customFileInfoComponent.GetType().GetField("info"))
                {
                    coordinatePath = customFileInfoComponent.GetField("info").GetField("FullPath") as string ?? "";
                }
                else
                {
                    //KKP
                    coordinatePath = customFileInfoComponent.GetProperty("info").GetProperty("FullPath") as string ?? "";
                }
            }
            Logger.LogDebug($"Coordinate Path: {coordinatePath}");
            if (string.IsNullOrEmpty(coordinatePath)) return;

            //取得飾品清單
            ChaFileCoordinate tmpChaFileCoordinate = new ChaFileCoordinate();
            tmpChaFileCoordinate.LoadFile(coordinatePath);

            List<string> accNames = new List<string>();

            accNames.AddRange(tmpChaFileCoordinate.accessory.parts.Select(x => Helper.GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)));

            // MoreAcc v2.0弄了個同時存新舊資料的兼容
            // 如果已載入飾品數量<20，那就當做是舊存檔，讀舊數據
            if (CLO._isMoreAccessoriesExist && tmpChaFileCoordinate.accessory.parts.Length <= 20)
            {
                accNames.AddRange(MoreAccessories.LoadOldMoreAccData(tmpChaFileCoordinate));
            }

            // 檢查選中的服裝是否有要綁定飾品的插件資料
            boundAcc = false;
            foreach (string guid in CLO.pluginBoundAccessories)
            {
                if (null != ExtendedSave.GetExtendedDataById(tmpChaFileCoordinate, guid))
                {
                    boundAcc = true;
                    Logger.LogWarning($"The accessories option is disabled due to the plugin data ({guid}) found on selected coordinate ({tmpChaFileCoordinate.coordinateName})");
                    break;
                }
            }

            // 更新ChangeMode button禁用/啟用狀態
            if (boundAcc) CoordinateLoad.addAccModeFlag = false;
            Button btnChangeAccLoadMode = panel2.transform.Find("BtnChangeAccLoadMode").GetComponent<Button>();
            btnChangeAccLoadMode.interactable = !boundAcc;
            btnChangeAccLoadMode.GetComponentInChildren<Text>().text =
                CoordinateLoad.addAccModeFlag
                    ? StringResources.StringResourcesManager.GetString("addMode")
                    : StringResources.StringResourcesManager.GetString("replaceMode");

            if (boundAcc) lockHairAcc = false;
            Toggle tglHair = panel2.transform.Find("lockHairAcc").GetComponent<Toggle>();
            tglHair.interactable = !boundAcc;
            tglHair.isOn = lockHairAcc;

            foreach (Toggle tgl in toggleGroup.gameObject.GetComponentsInChildren<Toggle>())
            {
                GameObject.Destroy(tgl.gameObject);
            }
            List<Toggle> tmpTgls = new List<Toggle>();
            foreach (string accName in accNames)
            {
                Toggle toggle = UIUtility.CreateToggle(Enum.GetValues(typeof(CLO.ClothesKind)).GetValue(9).ToString(), toggleGroup.transform, accName);
                toggle.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                toggle.GetComponentInChildren<Text>(true).color = Color.white;
                toggle.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -25f * (tmpTgls.Count + 1)), new Vector2(0f, -25f * tmpTgls.Count));
                toggle.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
                toggle.isOn = true;
                toggle.interactable = !boundAcc;
                tmpTgls.Add(toggle);
            }
            tgls2 = tmpTgls.ToArray();
            toggleGroup.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(0, -(25f * (accNames.Count - 20))), new Vector2(0, 0));

            panel2.gameObject.SetActive(tgls[(int)CLO.ClothesKind.accessories].isOn);
            //Logger.LogDebug("Onselect");
        }

        [HarmonyPriority(Priority.First)]
        internal static bool OnClickLoadPrefix() => false;

        //Load衣裝時觸發
        internal static void OnClickLoadPostfix()
        {
            if (CoordinateLoad.oCICharQueue.Count != 0) return;

            Logger.LogDebug("Studio Coordinate Load Option Start");

            //bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;

            List<bool> toggleBoolList = tgls.Select(x => x.isOn).ToList();
            toggleBoolList.AddRange(charaOverlay);
            toggleBoolList.Add(readABMX);

            foreach (bool b in toggleBoolList)
            {
                //isAllTrueFlag &= b;
                isAllFalseFlag &= !b;
            }

            ////飾品細項只反應到AllTrue檢查
            //foreach (Toggle tgl in tgls2) {
            //    isAllTrueFlag &= tgl.isOn;
            //}

            //全沒選
            if (panel.IsActive() && isAllFalseFlag)
            {
                Logger.LogInfo("No Toggle selected, skip loading coordinate");
                Logger.LogDebug("Studio Coordinate Load Option Finish");
                return;
            }

            if (CLO.insideStudio)
            {
                IEnumerable<OCIChar> array = GuideObjectManager.Instance.selectObjectKey
                    .Select(p => (OCIChar)Studio.Studio.GetCtrlInfo(p))
                    .Where(p => null != p).OfType<OCIChar>();
                if (array.Count() == 0)
                {
                    //沒選中人
                    Logger.LogMessage("No available characters selected");
                    Logger.LogDebug("Studio Coordinate Load Option Finish");
                }
                else if (!panel.IsActive()/* || (isAllTrueFlag && !lockHairAcc && !addAccModeFlag)*/)
                {
                    //未展開Panel
                    Logger.LogInfo("Use original game function");
                    foreach (OCIChar ocichar in array)
                    {
                        ocichar.LoadClothesFile(coordinatePath);
                    }
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                }
                else
                {
                    //建立tmpChara並等待載入完成，然後再呼叫換衣
                    CoordinateLoad.totalAmount = array.Count();
                    CoordinateLoad.oCICharQueue = new Queue<OCIChar>(array);
                    CoordinateLoad.finishedCount = 0;
                    CoordinateLoad.MakeTmpChara(CoordinateLoad.ChangeCoordinate);
                }
            }
            else
            {
                if (!panel.IsActive()/* || (isAllTrueFlag && !lockHairAcc && !addAccModeFlag)*/)
                {
                    //未展開Panel
                    Logger.LogInfo("Use original game function");
                    ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
                    bool flag = CustomFileWindow.tglCoordeLoadClothes && CustomFileWindow.tglCoordeLoadClothes.isOn;
                    bool flag2 = CustomFileWindow.tglCoordeLoadAcs && CustomFileWindow.tglCoordeLoadAcs.isOn;
                    byte[] bytes = MessagePackSerializer.Serialize<ChaFileClothes>(chaCtrl.nowCoordinate.clothes);
                    byte[] bytes2 = MessagePackSerializer.Serialize<ChaFileAccessory>(chaCtrl.nowCoordinate.accessory);
                    chaCtrl.nowCoordinate.LoadFile(coordinatePath);
                    if (!flag)
                    {
                        chaCtrl.nowCoordinate.clothes = MessagePackSerializer.Deserialize<ChaFileClothes>(bytes);
                    }
                    if (!flag2)
                    {
                        chaCtrl.nowCoordinate.accessory = MessagePackSerializer.Deserialize<ChaFileAccessory>(bytes2);
                    }

                    if (CLO._isABMXExist)
                    {
                        //強制重整，修正ABMX在讀取衣裝後，不會正常反應
                        new ABMX(chaCtrl).SetExtDataFromController();
                    }

                    chaCtrl.Reload(false, true, true, true);
                    byte[] data = chaCtrl.nowCoordinate.SaveBytes();
                    chaCtrl.chaFile.coordinate[chaCtrl.chaFile.status.coordinateType].LoadBytes(data, chaCtrl.nowCoordinate.loadVersion);
                    Singleton<CustomBase>.Instance.updateCustomUI = true;
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                }
                else
                {
                    //建立tmpChara並等待載入完成，然後再呼叫換衣
                    CoordinateLoad.MakeTmpChara(CoordinateLoad.ChangeCoordinate);
                }
            }
        }
    }

    //用於MakerUI之OnEnable回呼
    class OnEnableListener : MonoBehaviour
    {
        public delegate void EventHandler();
        public event EventHandler OnEnableEvent;

        public void OnEnable() => this.StartCoroutine(OnEnableCoroutine());

        //確保能在最後執行
        private IEnumerator OnEnableCoroutine()
        {
            yield return null;
            OnEnableEvent?.Invoke();
        }
    }
}
