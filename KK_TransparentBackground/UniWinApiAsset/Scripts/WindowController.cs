/**
 * UniWinApi sample
 * 
 * Author: Kirurobo http://twitter.com/kirurobo
 * License: CC0 https://creativecommons.org/publicdomain/zero/1.0/
 */

 /* 20200519 -jim60105
  * Remove drag file & move window related code
  * Add TransparentMaterial
  * Add Koikatu stuff
  */ 

using System;
using System.Collections;
using UnityEngine;

namespace Kirurobo {

    /// <summary>
    /// Set editable the bool property
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class BoolPropertyAttribute : PropertyAttribute { }

    /// <summary>
    /// Set the attribute as readonly
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute { }


    /// <summary>
    /// ウィンドウ操作をとりまとめるクラス
    /// </summary>
    public class WindowController : MonoBehaviour {
        /// <summary>
        /// Window controller
        /// </summary>
        public UniWinApi uniWin;

        public bool blockClickThrough {
            get => _blockClickThrough;
            set {
                _blockClickThrough = value;
                UpdateClickThrough(!value);
            }
        }
        private bool _blockClickThrough = false;

        /// <summary>
        /// 操作を透過する状態か
        /// </summary>
        public bool isClickThrough {
            get { return _isClickThrough; }
        }
        private bool _isClickThrough = true;

        /// <summary>
        /// Is this window transparent
        /// </summary>
        public bool isTransparent {
            get { return _isTransparent; }
            set { SetTransparent(value); }
        }
        [SerializeField, BoolProperty, Tooltip("Check to set transparent on startup")]
        private bool _isTransparent = false;

        /// <summary>
        /// Is this window minimized
        /// </summary>
        public bool isTopmost {
            get { return ((uniWin == null) ? _isTopmost : _isTopmost = uniWin.IsTopmost); }
            set { SetTopmost(value); }
        }
        [SerializeField, BoolProperty, Tooltip("Check to set topmost on startup")]
        private bool _isTopmost = false;

        /// <summary>
        /// Is this window maximized
        /// </summary>
        public bool isMaximized {
            get { return ((uniWin == null) ? _isMaximized : _isMaximized = uniWin.IsMaximized); }
            set { SetMaximized(value); }
        }
        [SerializeField, BoolProperty, Tooltip("Check to set maximized on startup")]
        private bool _isMaximized = false;

        /// <summary>
        /// Is this window minimized
        /// </summary>
        public bool isMinimized {
            get { return ((uniWin == null) ? _isMinimized : _isMinimized = uniWin.IsMinimized); }
            set { SetMinimized(value); }
        }

        public Material TransparentMaterial;

        [SerializeField, BoolProperty, Tooltip("Check to set minimized on startup")]
        private bool _isMinimized = false;

        // カメラの背景をアルファゼロの黒に置き換えるため、本来の背景を保存しておく変数
        private CameraClearFlags originalCameraClearFlags;
        private Color originalCameraBackground;
        private int originalCameraCullingmask;

        /// <summary>
        /// Is the mouse pointer on an opaque pixel
        /// </summary>
        [SerializeField, Tooltip("Is the mouse pointer on an opaque pixel? (Read only)")]
        private bool onOpaquePixel = true;

        /// <summary>
        /// The cut off threshold of alpha value.
        /// </summary>
        private float opaqueThreshold = 0.1f;

        /// <summary>
        /// Pixel color under the mouse pointer. (Read only)
        /// </summary>
        [ReadOnly, Tooltip("Pixel color under the mouse pointer. (Read only)")]
        public Color pickedColor;

        /// <summary>
        /// 現在対象としているウィンドウが自分自身らしいと確認できたらtrueとする
        /// </summary>
        private bool isWindowChecked = false;

        /// <summary>
        /// カメラのインスタンス
        /// </summary>
        private Camera currentCamera;

        /// <summary>
        /// タッチがBeganとなったものを受け渡すためのリスト
        /// PickColorCoroutine()実行のタイミングではどうもtouch.phaseがうまくとれないようなのでこれで渡してみる
        /// </summary>
        private Touch? firstTouch = null;

        /// <summary>
        /// ウィンドウ状態が変化したときに発生するイベント
        /// </summary>
        public event OnStateChangedDelegate OnStateChanged;
        public delegate void OnStateChangedDelegate();

        /// <summary>
        /// 表示されたテクスチャ
        /// </summary>
        private Texture2D colorPickerTexture = null;

        // Use this for initialization
        void Awake() {
            Input.simulateMouseWithTouches = false;

            if (!currentCamera) {
                // メインカメラを探す
                currentCamera = Camera.main;

                // もしメインカメラが見つからなければ、Findで探す
                if (!currentCamera) {
                    currentCamera = FindObjectOfType<Camera>();
                }
            }

            StoreOriginalCameraSetting();

            // マウス下描画色抽出用テクスチャを準備
            colorPickerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

            // ウィンドウ制御用のインスタンス作成
            uniWin = new UniWinApi();

            // 自分のウィンドウを取得
            FindMyWindow();
        }

        void Start() {
            // マウスカーソル直下の色を取得するコルーチンを開始
            StartCoroutine(PickColorCoroutine());
        }

        void OnDestroy() {
            if (uniWin != null) {
                uniWin.Dispose();
            }
        }

        void OnGUI() {
            float buttonWidth = 140f;
            float buttonHeight = 40f;
            float margin = 20f;
            if (
                GUI.Button(
                    new Rect(
                        Screen.width - buttonWidth - margin,
                        Screen.height - buttonHeight - margin,
                        buttonWidth,
                        buttonHeight),
                    "Toggle transparency"
                    )
                ) {
                // 透過の ON/OFF ボタン
                isTransparent ^= true;
                isTopmost ^= true;
            }
        }

        // Update is called once per frame
        void Update() {
            // 自ウィンドウ取得状態が不確かなら探しなおす
            //  マウス押下が取れるのはすなわちフォーカスがあるとき
            //if (Input.anyKey) {
            //    UpdateWindow();
            //}

            // キー、マウス操作の下ウィンドウへの透過状態を更新
            UpdateClickThrough();

            // ウィンドウ枠が復活している場合があるので監視するため、呼ぶ
            if (uniWin != null) {
                uniWin.Update();
            }
        }

        void OnRenderImage(RenderTexture from, RenderTexture to) {
            if (_isTransparent && null != TransparentMaterial) {
                Graphics.Blit(from, to, TransparentMaterial);
            } else {
                Graphics.Blit(from, to);
            }
        }

        /// <summary>
        /// ウィンドウ状態が変わったときに呼ぶイベントを処理
        /// </summary>
        private void StateChangedEvent() {
            if (OnStateChanged != null) {
                OnStateChanged();
            }
        }

        /// <summary>
        /// 画素の色を基に操作受付を切り替える
        /// </summary>
        public void UpdateClickThrough(bool? clickThrough = null) {
            // Force set Click through
            if (null != clickThrough) {
                if (uniWin != null) uniWin.EnableClickThrough((bool)clickThrough);
                _isClickThrough = (bool)clickThrough;
                return;
            }

            // マウスカーソル非表示状態ならば透明画素上と同扱い
            bool opaque = (onOpaquePixel && !UniWinApi.GetCursorVisible());

            if (_isClickThrough) {
                if (opaque) {
                    if (uniWin != null) uniWin.EnableClickThrough(false);
                    _isClickThrough = false;
                }
            } else {
                if (isTransparent && !opaque && !blockClickThrough) {
                    if (uniWin != null) uniWin.EnableClickThrough(true);
                    _isClickThrough = true;
                }
            }
        }

        public void StoreOriginalCameraSetting() {
            // カメラの元の背景を記憶
            if (currentCamera) {
                originalCameraClearFlags = currentCamera.clearFlags;
                originalCameraBackground = currentCamera.backgroundColor;
                originalCameraCullingmask = currentCamera.cullingMask;
            }
        }

        /// <summary>
        /// OnPostRenderではGUI描画前になってしまうため、コルーチンを用意
        /// </summary>
        /// <returns></returns>
        private IEnumerator PickColorCoroutine() {
            while (Application.isPlaying) {
                yield return new WaitForEndOfFrame();
                UpdateOnOpaquePixel();
            }
            yield return null;
        }

        /// <summary>
        /// マウス下の画素があるかどうかを確認
        /// </summary>
        /// <param name="cam"></param>
        private void UpdateOnOpaquePixel() {
            Vector2 mousePos;
            mousePos = Input.mousePosition;

            //// コルーチン & WaitForEndOfFrame ではなく、OnPostRenderで呼ぶならば、MSAAによって上下反転しないといけない？
            //if (QualitySettings.antiAliasing > 1) mousePos.y = camRect.height - mousePos.y;

            // タッチ開始点が指定されれば、それを調べる
            if (firstTouch != null) {
                Touch touch = (Touch)firstTouch;
                Vector2 pos = touch.position;

                firstTouch = null;

                if (GetOnOpaquePixel(pos)) {
                    onOpaquePixel = true;
                    //activeFingerId = touch.fingerId;
                    return;
                }
            }

            // マウス座標を調べる
            if (GetOnOpaquePixel(mousePos)) {
                //Debug.Log("Mouse " + mousePos);
                onOpaquePixel = true;
                return;
            } else {
                onOpaquePixel = false;
            }
        }

        /// <summary>
        /// 指定座標の画素が透明か否かを返す
        /// </summary>
        /// <param name="mousePos">座標[px]。必ず描画範囲内であること。</param>
        /// <returns></returns>
        private bool GetOnOpaquePixel(Vector2 mousePos) {
            // 画面外であれば透明と同様
            if (
                mousePos.x < 0 || mousePos.x >= Screen.width
                || mousePos.y < 0 || mousePos.y >= Screen.height
                ) {
                return false;
            }

            // 透過状態でなければ、範囲内なら不透過扱いとする
            if (!_isTransparent) return true;

            // 指定座標の描画結果を見て判断
            try {
                // Reference http://tsubakit1.hateblo.jp/entry/20131203/1386000440
                colorPickerTexture.ReadPixels(new Rect(mousePos, Vector2.one), 0, 0);
                Color color = colorPickerTexture.GetPixel(0, 0);
                pickedColor = color;
                return (color.a >= opaqueThreshold);  // αがしきい値以上ならば不透過とする
            } catch (System.Exception ex) {
                Debug.LogError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 自分のウィンドウハンドルを見つける
        /// </summary>
        private void FindMyWindow() {
            // ウィンドウが確かではないとしておく
            isWindowChecked = false;

            // 現在このウィンドウがアクティブでなければ、取得はやめておく
            if (!Application.isFocused) return;

            // 今アクティブなウィンドウを取得
            UniWinApi.WindowHandle window = UniWinApi.FindWindow();
            if (window == null) return;

            // 見つかったウィンドウを利用開始
            uniWin.SetWindow(window);

            // 初期状態を反映
            SetTopmost(_isTopmost);
            SetMaximized(_isMaximized);
            SetMinimized(_isMinimized);
            SetTransparent(_isTransparent);
        }

        /// <summary>
        /// 自分のウィンドウハンドルが不確かならば探しなおす
        /// </summary>
        private void UpdateWindow() {
            if (uniWin == null) return;

            // もしウィンドウハンドル取得に失敗していたら再取得
            if (!uniWin.IsActive) {
                //Debug.Log("Window is not active");
                FindMyWindow();
            } else if (!isWindowChecked) {
                // 自分自身のウィンドウか未確認の場合

                // 今アクティブなウィンドウが自分自身かをチェック
                if (uniWin.CheckActiveWindow()) {
                    isWindowChecked = true; // どうやら正しくウィンドウをつかめているよう
                } else {
                    // ウィンドウが違っているようなので、もう一度アクティブウィンドウを取得
                    uniWin.Reset();
                    uniWin.Dispose();
                    uniWin = new UniWinApi();
                    FindMyWindow();
                }
            }
        }

        /// <summary>
        /// ウィンドウへのフォーカスが変化したときに呼ばれる
        /// </summary>
        /// <param name="focus"></param>
        private void OnApplicationFocus(bool focus) {
            //Debug.Log("Focus:" + focus);
            if (focus) {
                UpdateWindow();
            }
        }

        /// <summary>
        /// ウィンドウ透過状態になった際、自動的に背景を透明単色に変更する
        /// </summary>
        /// <param name="isTransparent"></param>
        void SetCamera(bool isTransparent) {
            if (null == currentCamera) return;

            if (isTransparent) {
                currentCamera.clearFlags = CameraClearFlags.SolidColor;
                currentCamera.backgroundColor = Color.clear;

                //H scene會在HSceneProc.Update()->HSceneProc.SetConfig()重寫Camera.main.backgroundColor
                //這EtcData是重寫的來源
                Manager.Config.EtcData.BackColor = Color.clear;

                //Only display chara layer
                //為了把地圖等東西不顯示，若有其它什麼沒顯示出來要修改這裡
                currentCamera.cullingMask = 1 << LayerMask.NameToLayer("Chara");
            } else {
                currentCamera.clearFlags = originalCameraClearFlags;
                currentCamera.backgroundColor = originalCameraBackground;

                Manager.Config.EtcData.BackColor = originalCameraBackground;
                currentCamera.cullingMask = originalCameraCullingmask;
            }

            try {
                //These only work in Maker
                GameObject.Find("BackGroundCamera").GetComponent<Camera>().enabled = !isTransparent;
                Camera.main.gameObject.GetComponent<UnityStandardAssets.ImageEffects.BloomAndFlares>().enabled = !isTransparent;
            } catch (NullReferenceException) { }
        }

        /// <summary>
        /// 透明化状態を切替
        /// </summary>
        /// <param name="transparent"></param>
        public void SetTransparent(bool transparent) {
            if (transparent && !_isTransparent) StoreOriginalCameraSetting();

            _isTransparent = transparent;
            SetCamera(transparent);

            //隱藏Map
            //因為某些Map物件是在CharaLayer，所以除了cullingMask以外也要做這個
            var go = GameObject.Find("/Map");
            if (null != go) {
                //Go through children
                foreach(Transform t in go.transform) {
                    t.gameObject.SetActive(!transparent);
                }
            }

            if (uniWin != null) {
                uniWin.EnableTransparent(transparent);
            }
            UpdateClickThrough();
            StateChangedEvent();
        }

        /// <summary>
        /// 最大化を切替
        /// </summary>
        public void SetMaximized(bool maximized) {
            //if (_isMaximized == maximized) return;
            if (uniWin == null) {
                _isMaximized = maximized;
            } else {

                if (maximized) {
                    uniWin.Maximize();
                } else if (uniWin.IsMaximized) {
                    uniWin.Restore();
                }
                _isMaximized = uniWin.IsMaximized;
            }
            StateChangedEvent();
        }

        /// <summary>
        /// 最小化を切替
        /// </summary>
        public void SetMinimized(bool minimized) {
            //if (_isMinimized == minimized) return;
            if (uniWin == null) {
                _isMinimized = minimized;
            } else {
                if (minimized) {
                    uniWin.Minimize();
                } else if (uniWin.IsMinimized) {
                    uniWin.Restore();
                }
                _isMinimized = uniWin.IsMinimized;
            }
            StateChangedEvent();
        }

        /// <summary>
        /// 最前面を切替
        /// </summary>
        /// <param name="topmost"></param>
        public void SetTopmost(bool topmost) {
            //if (_isTopmost == topmost) return;
            if (uniWin == null) return;

            uniWin.EnableTopmost(topmost);
            _isTopmost = uniWin.IsTopmost;
            StateChangedEvent();
        }

        /// <summary>
        /// 終了時にはウィンドウプロシージャを戻す処理が必要
        /// </summary>
        void OnApplicationQuit() {
            if (Application.isPlaying) {
                if (uniWin != null) {
                    uniWin.Dispose();
                }
            }
        }

        /// <summary>
        /// 自分のウィンドウにフォーカスを与える
        /// </summary>
        public void Focus() {
            if (uniWin != null) {
                uniWin.SetFocus();
            }
        }
    }
}
