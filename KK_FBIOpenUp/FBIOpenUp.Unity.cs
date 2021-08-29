using Extension;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FBIOpenUp {
    internal static class UnityStuff {
        private static readonly BepInEx.Logging.ManualLogSource Logger = FBIOpenUp.Logger;
        /// <summary>
        /// 按下按鈕計時器
        /// </summary>
        private static float btnClickTimer = 0;
        /// <summary>
        /// 按下按鈕?
        /// </summary>
        private static bool downState = false;
        /// <summary>
        /// 動畫步驟
        /// </summary>
        private static int step = 0;

        internal class ShiftPicture {
            internal Image image;
            internal RawImage video;
            internal float smoothTime = 0.5f;
            internal Vector3 velocity = Vector3.zero;
            internal Vector3 targetPosition = Vector3.zero;
            internal Type type;

            public enum Type {
                picture,
                video
            }
            internal float Width {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.sprite.rect.width;
                        case Type.video:
                            return video.texture.width;
                    }
                    return 0;
                }
            }
            internal float Height {
                get {
                    switch (type) {
                        case Type.picture:
                            return image.sprite.rect.height;
                        case Type.video:
                            return video.texture.height;
                    }
                    return 0;
                }
            }
            internal Transform Transform {
                get {
                    switch (type) {
                        case Type.picture:
                            return image?.transform;
                        case Type.video:
                            return video?.transform;
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// 切換紅色書包圖標顯示
        /// </summary>
        private static void ChangeRedBagBtn(GameObject redBagBtn) {
            if (FBIOpenUp._isenabled) {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                Logger.LogInfo("Enable Plugin");
            } else {
                redBagBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
                Logger.LogInfo("Disable Plugin");
            }
        }

        /// <summary>
        /// 建立紅色書包圖標
        /// </summary>
        /// <param name="gameMode">遊戲模式</param>
        /// <param name="param">我知道這不安全，但這很方便σ`∀´)σ</param>
        internal static void DrawRedBagBtn(FBIOpenUp.GameMode gameMode, object param = null) {
            GameObject original, parent;
            Vector2 offsetMin, offsetMax;
            FBIOpenUp.nowGameMode = gameMode;
            switch (FBIOpenUp.nowGameMode) {
                case FBIOpenUp.GameMode.Studio:
                    CharaList charaList = param as CharaList;
                    original = GameObject.Find($"StudioScene/Canvas Main Menu/01_Add/{charaList.name}/Button Change");
                    parent = original.transform.parent.gameObject;
                    offsetMin = new Vector2(-120, -270);
                    offsetMax = new Vector2(-40, -190);
                    break;
                case FBIOpenUp.GameMode.Maker:
                    original = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/01_BodyTop/tglShape/ShapeTop/Scroll View/Viewport/Content/grpBtn/btnS");
                    parent = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCoordinateType").gameObject;
                    offsetMin = new Vector2(849.64f, -120f);
                    offsetMax = new Vector2(914.64f, -55f);
                    break;
                case FBIOpenUp.GameMode.MainGame:
                    original = GameObject.Find("ActionScene/UI/ActionMenuCanvas/ModeAnimation/State");
                    parent = original.transform.parent.gameObject;
                    offsetMin = new Vector2(0, -80);
                    offsetMax = new Vector2(80, 0);
                    break;
                case FBIOpenUp.GameMode.FreeH:
                    original = GameObject.Find("Canvas/SubMenu/ClothCategory/ClothFemale/DressCategory/ClothChange");
                    int i = (int)param;
                    if (i == 1) {
                        parent = GameObject.Find("Canvas/SubMenu/ClothCategory");
                    } else {
                        parent = GameObject.Find("Canvas/SubMenu/SecondDressCategory");
                    }

                    offsetMin = new Vector2(5, -198);
                    offsetMax = new Vector2(107, -96);
                    break;
                default:
                    return;
            }
            GameObject redBagBtn = UnityEngine.Object.Instantiate(original, parent.transform);
            redBagBtn.name = "redBagBtn";
            redBagBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), offsetMin, offsetMax);

            redBagBtn.GetComponent<Button>().spriteState = new SpriteState();
            redBagBtn.GetComponent<Image>().sprite = ImageHelper.LoadNewSprite("FBIOpenUp.Resources.redBag.png", 100, 100);
            redBagBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            for (int i = 0; i < redBagBtn.GetComponent<Button>().onClick.GetPersistentEventCount(); i++) {
                redBagBtn.GetComponent<Button>().onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }
            if (redBagBtn.transform.Find("Text (TMP)")?.gameObject is GameObject go) {
                GameObject.Destroy(go);
            }
            if (redBagBtn.transform.Find("textBtn")?.gameObject is GameObject go2) {
                GameObject.Destroy(go2);
            }
            redBagBtn.GetComponent<Button>().interactable = true;

            //因為要handle長按，監聽PointerDown、PointerUp Event
            //在Update()裡面有對Timer累加
            redBagBtn.AddComponent<EventTrigger>();
            EventTrigger trigger = redBagBtn.gameObject.GetComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerDown,
                callback = new EventTrigger.TriggerEvent()
            };
            pointerDown.callback.AddListener((baseEventData) => {
                btnClickTimer = 0;
                downState = true;
            });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerUp,
                callback = new EventTrigger.TriggerEvent()
            };
            pointerUp.callback.AddListener((baseEventData) => {
                downState = false;
                float clickDeltaTime = btnClickTimer;
                btnClickTimer = 0;
                if (step != 0) return;
                switch (FBIOpenUp.nowGameMode) {
                    case FBIOpenUp.GameMode.FreeH:
                        //不分長短按
                        FBIOpenUp.SetEnabled();
                        if (FBIOpenUp._isenabled) {
                            step = 10;
                        } else {
                            if (FBIOpenUp.Effect_on_ABMX.Value) {
                                step = 2;
                            } else {
                                step = 20;
                            }
                        }
                        ChangeWithoutShiftPicture = true;
                        StepLightAndPlay();
                        //開ABMX時不變更狀態
                        if (FBIOpenUp.Effect_on_ABMX.Value) {
                            FBIOpenUp.SetEnabled();
                        }
                        ChangeRedBagBtn(redBagBtn);
                        break;
                    case FBIOpenUp.GameMode.Maker:
                        FBIOpenUp.SetEnabled();
                        if (FBIOpenUp._isenabled) {
                            DrawSlidePic(10);
                        } else {
                            if (FBIOpenUp.Effect_on_ABMX.Value) {
                                DrawSlidePic(2);
                            } else {
                                DrawSlidePic(20);
                            }
                        }
                        //Maker開ABMX時、啟用長按時不變更狀態
                        if (FBIOpenUp.Effect_on_ABMX.Value || (clickDeltaTime >= 1f && FBIOpenUp._isenabled)) {
                            FBIOpenUp.SetEnabled();
                        }
                        ChangeRedBagBtn(redBagBtn);
                        break;
                    case FBIOpenUp.GameMode.MainGame:
                        //主遊戲不分長短按
                        FBIOpenUp.SetEnabled();
                        if (FBIOpenUp._isenabled) {
                            DrawSlidePic(10);
                        } else {
                            if (FBIOpenUp.Effect_on_ABMX.Value) {
                                DrawSlidePic(2);
                            } else {
                                DrawSlidePic(20);
                            }
                        }
                        if (FBIOpenUp.Effect_on_ABMX.Value) {
                            FBIOpenUp.SetEnabled();
                        }
                        ChangeRedBagBtn(redBagBtn);
                        break;
                    case FBIOpenUp.GameMode.Studio:
                        //Studio長按時調整狀態而不變人
                        FBIOpenUp.SetEnabled();
                        if (clickDeltaTime <= 1f) {
                            if (FBIOpenUp._isenabled) {
                                DrawSlidePic(10);
                            } else {
                                if (FBIOpenUp.Effect_on_ABMX.Value) {
                                    DrawSlidePic(2);
                                } else {
                                    DrawSlidePic(20);
                                }
                            }
                            ChangeRedBagBtn(redBagBtn);
                        } else {
                            if (FBIOpenUp._isenabled) {
                                DrawSlidePic(1);
                            } else {
                                DrawSlidePic(2);
                            }
                            ChangeRedBagBtn(redBagBtn);
                        }
                        break;
                }
            });
            trigger.triggers.Add(pointerUp);
            ChangeRedBagBtn(redBagBtn);
        }

        private static float videoTimer = 0;
        private static ShiftPicture shiftPicture;
        /// <summary>
        /// 繪製轉場圖片
        /// </summary>
        /// <param name="_step">繪製完後要進入的腳本位置</param>
        /// <param name="sceneName">Scene名稱</param>
        private static void DrawSlidePic(int _step) {
            GameObject parent;
            switch (FBIOpenUp.nowGameMode) {
                case FBIOpenUp.GameMode.Studio:
                    parent = GameObject.Find("StudioScene/Canvas Main Menu");
                    break;
                case FBIOpenUp.GameMode.MainGame:
                    parent = GameObject.Find("ActionScene/UI/ActionMenuCanvas/ModeAnimation");
                    break;
                case FBIOpenUp.GameMode.Maker:
                    parent = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCoordinateType/redBagBtn");
                    break;
                //FreeH死都不成功，放棄
                //case KK_FBIOpenUp.GameMode.FreeH:
                //    parent = GameObject.Find("CommonSpace");
                //    break;
                default:
                    parent = GameObject.FindObjectsOfType<GameObject>()[0]; //Not tested
                    break;
            }
            GameObject gameObject = new GameObject();
            gameObject.transform.SetParent(parent.transform, false);
            gameObject.SetActive(false);
            if (null != shiftPicture) {
                GameObject.Destroy(shiftPicture.Transform.parent.gameObject);
                shiftPicture.image = null;
                shiftPicture.video = null;
                shiftPicture = null;
            }
            shiftPicture = new ShiftPicture();

            //如果影片不存在，用熊吉代替
            bool noVideoFallback = _step == 20 && null == FBIOpenUp.videoPath;
            if (noVideoFallback) {
                _step = 2;
            }

            switch (_step) {
                case 1:
                    //小學生真是太棒了
                    shiftPicture.type = ShiftPicture.Type.picture;
                    shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, ImageHelper.LoadNewSprite("FBIOpenUp.Resources.saikodaze.jpg", 800, 657));
                    shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f * 800 / 657, Screen.height / 1.5f);
                    Right2Center();
                    break;
                case 2:
                    //熊吉逮捕
                    shiftPicture.type = ShiftPicture.Type.picture;
                    shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, ImageHelper.LoadNewSprite("FBIOpenUp.Resources.Kumakichi.jpg", 640, 480));
                    shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f * 640 / 480, Screen.height / 1.5f);
                    Left2Center();
                    break;
                case 10:
                    //幼女退光線
                    shiftPicture.type = ShiftPicture.Type.picture;
                    shiftPicture.image = UIUtility.CreateImage("", gameObject.transform, ImageHelper.LoadNewSprite("FBIOpenUp.Resources.beam.png", 700, 700));
                    shiftPicture.image.rectTransform.sizeDelta = new Vector2(Screen.height / 1.25f, Screen.height / 1.25f);
                    Right2Center();
                    break;
                case 20:
                    //FBI Open Up影片
                    shiftPicture.type = ShiftPicture.Type.video;

                    shiftPicture.video = UIUtility.CreateRawImage("", gameObject.transform);
                    shiftPicture.video.rectTransform.sizeDelta = new Vector2(Screen.height / 1.5f, Screen.height / 1.5f);

                    UnityEngine.Video.VideoPlayer videoPlayer = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                    AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                    videoPlayer.playOnAwake = false;
                    audioSource.playOnAwake = false;
                    videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.APIOnly;

                    //videoPlayer.url= "../UserData/audio/FBI.mp4";
                    videoPlayer.url = FBIOpenUp.videoPath;

                    //Set Audio Output to AudioSource
                    videoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;

                    //Assign the Audio from Video to AudioSource to be played
                    videoPlayer.EnableAudioTrack(0, true);
                    videoPlayer.SetTargetAudioSource(0, audioSource);

                    Logger.LogDebug($"{videoPlayer.url}");
                    videoPlayer.isLooping = true;

                    //先把他移到螢幕外啟用，否則未啟用無法Prepare，而直接啟用會出現白色畫面
                    shiftPicture.Transform.position = new Vector3(-2 * Screen.width, Screen.height / 2);
                    gameObject.SetActive(true);

                    videoPlayer.Prepare();
                    videoPlayer.prepareCompleted += (source) => {
                        ///TODO 這實際上沒有辦法真的catch到錯誤，待修
                        if (videoPlayer.texture == null) {
                            Logger.LogError("Video not found");
                            GameObject.Destroy(shiftPicture.Transform.parent.gameObject);
                            shiftPicture.video = null;
                            shiftPicture = null;
                            _step = 0;
                            return;
                        }

                        shiftPicture.video.texture = videoPlayer.texture;
                        videoTimer = 2;
                        videoPlayer.Play();
                        audioSource.Play();

                        //影片太大聲QQ
                        audioSource.volume = FBIOpenUp.videoVolume;

                        Left2Center();
                    };
                    break;
            }

            //如果影片不存在，用熊吉代替
            if (noVideoFallback) {
                _step = 20;
            }

            step = _step;
            Logger.LogDebug("Draw Slide Pic");

            void Right2Center() {
                //Right To Center
                shiftPicture.Transform.position = new Vector3(Screen.width + shiftPicture.Width / 2, Screen.height / 2);
                shiftPicture.targetPosition = new Vector3(Screen.width / 2, Screen.height / 2);
                gameObject.SetActive(true);
            }
            void Left2Center() {
                //Left To Center
                shiftPicture.Transform.position = new Vector3(-1 * (Screen.width + shiftPicture.Width / 2), Screen.height / 2);
                shiftPicture.targetPosition = new Vector3(Screen.width / 2, Screen.height / 2);
                gameObject.SetActive(true);
            }
        }

        private static float intensityFrom = 1f;
        private static float intensityTo = 5f;
        private static bool intensityState = false;
        private static CameraLightCtrl.LightInfo studioLightInfo;
        private static object studioLightCalc;
        private static UnityEngine.Light makerLight;
        private static UnityEngine.Light freeHLight;
        /// <summary>
        /// 調整角色燈光，製造爆亮轉場
        /// </summary>
        /// <param name="goLighter">True轉亮；False轉暗</param>
        private static void ToggleFlashLight(bool goLighter) {
            intensityState = true;
            switch (FBIOpenUp.nowGameMode) {
                case FBIOpenUp.GameMode.FreeH:
                    Light light = Hooks.hSceneProc.lightCamera;
                    if (null != light) {
                        freeHLight = light;
                    } else {
                        Logger.LogError("Get Camera Light FAILED");
                        FBIOpenUp.nowGameMode = FBIOpenUp.GameMode.MainGame;
                        goto default;
                    }
                    if (goLighter) {
                        intensityFrom = freeHLight.intensity;
                        intensityTo = 5f;
                    } else {
                        intensityTo = intensityFrom;
                        intensityFrom = freeHLight.intensity;
                    }
                    break;
                case FBIOpenUp.GameMode.Maker:
                    Light light1 = ((UnityEngine.GameObject)Singleton<ChaCustom.CustomControl>.Instance.cmpDrawCtrl.GetField("objLight")).GetComponent<UnityEngine.Light>();
                    if (null != light1) {
                        makerLight = light1;
                    } else {
                        Logger.LogError("Get Camera Light FAILED");
                        goto default;
                    }
                    if (goLighter) {
                        intensityFrom = makerLight.intensity;
                        intensityTo = 5f;
                    } else {
                        intensityTo = intensityFrom;
                        intensityFrom = makerLight.intensity;
                    }
                    break;
                case FBIOpenUp.GameMode.Studio:
                    if (null == studioLightInfo || null == studioLightCalc) {
                        studioLightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");
                        studioLightInfo = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
                        if (null == studioLightInfo || null == studioLightCalc) {
                            Logger.LogError("Get Camera Light FAILED");
                            goto default;
                        }
                    }
                    if (goLighter) {
                        intensityFrom = studioLightInfo.intensity;
                        intensityTo = 5f;
                    } else {
                        intensityTo = intensityFrom;
                        intensityFrom = studioLightInfo.intensity;
                    }
                    break;
                default:
                    ///TODO MainGame角色光未完成
                    intensityState = false;
                    return;
            }
        }

        //加亮動畫的計數器
        private static int reflectCount = 0;
        private static bool ChangeWithoutShiftPicture = false;
        internal static void Update() {
            //長按計時
            if (downState) {
                btnClickTimer += Time.deltaTime;
            }

            if (ChangeWithoutShiftPicture) {
                StepLightAndPlay();
            } else if (videoTimer > 0) {
                //影片delay兩秒再做轉場動畫，讓FBIOpenUp影片先在外頭撥放兩秒
                videoTimer -= Time.deltaTime;
            } else if ((null != shiftPicture && null != shiftPicture.Transform)) {
                shiftPicture.Transform.position = Vector3.SmoothDamp(shiftPicture.Transform.position, shiftPicture.targetPosition, ref shiftPicture.velocity, shiftPicture.smoothTime);
                //Logger.LogDebug($"Velocity:{shiftPicture.velocity} ; Image.position:{shiftPicture.Transform.position}");
                if ((shiftPicture.Transform.position - shiftPicture.targetPosition).sqrMagnitude < 1f) {
                    StepLightAndPlay();
                }
            }
        }

        private static void StepLightAndPlay() {
            if (intensityState && reflectCount < 60) {
                switch (FBIOpenUp.nowGameMode) {
                    case FBIOpenUp.GameMode.Studio:
                        studioLightInfo.intensity += (intensityTo - intensityFrom) / 60;
                        studioLightCalc.Invoke("Reflect");
                        break;
                    case FBIOpenUp.GameMode.FreeH:
                        freeHLight.intensity += (intensityTo - intensityFrom) / 60;
                        break;
                    case FBIOpenUp.GameMode.Maker:
                        makerLight.intensity += (intensityTo - intensityFrom) / 60;
                        break;
                }
                reflectCount++;
            } else {
                Logger.LogDebug($"At Step: {step}");
                switch (step) {
                    case 0:
                        //消滅圖片
                        if (null != shiftPicture.Transform.parent.gameObject) {
                            GameObject.Destroy(shiftPicture.Transform.parent.gameObject);
                            shiftPicture.image = null;
                            shiftPicture.video = null;
                            shiftPicture = null;
                        }
                        break;
                    case 1:
                        //由中間移動到左邊
                        shiftPicture.targetPosition = new Vector3(0 - (shiftPicture.Width / 2), Screen.height / 2);
                        stepSet(0);
                        break;
                    case 2:
                        //由中間移動到右邊
                        shiftPicture.targetPosition = new Vector3(Screen.width + shiftPicture.Width / 2, Screen.height / 2);
                        stepSet(0);
                        break;
                    case 10:
                        //將角色全部替換
                        //加亮角色光
                        reflectCount = 0;
                        ToggleFlashLight(true);
                        stepAdd();
                        break;
                    case 11:
                        intensityState = false;
                        Patches.ChangeAllCharacters(false);
                        reflectCount = 0;
                        ToggleFlashLight(false);
                        stepAdd();
                        break;
                    case 12:
                        intensityState = false;
                        if (!ChangeWithoutShiftPicture) {
                            stepSet(1);
                        } else {
                            ChangeWithoutShiftPicture = false;
                            stepSet(0);
                        }
                        break;
                    case 20:
                        //將角色換回
                        //加亮角色光
                        reflectCount = 0;
                        ToggleFlashLight(true);
                        stepAdd();
                        break;
                    case 21:
                        intensityState = false;
                        Patches.ChangeAllCharacters(true);
                        reflectCount = 0;
                        ToggleFlashLight(false);
                        stepAdd();
                        break;
                    case 22:
                        intensityState = false;
                        if (!ChangeWithoutShiftPicture) {
                            stepSet(2);
                        } else {
                            ChangeWithoutShiftPicture = false;
                            stepSet(0);
                        }
                        break;
                }
                void stepAdd() {
                    step++;
                }

                void stepSet(int st) {
                    step = st;
                }
            }
        }
    }
}
