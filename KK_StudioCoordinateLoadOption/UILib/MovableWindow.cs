using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UILib
{
	// Token: 0x02000024 RID: 36
	internal class MovableWindow : UIBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler, IPointerUpHandler
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600010B RID: 267 RVA: 0x0000DB64 File Offset: 0x0000BD64
		// (remove) Token: 0x0600010C RID: 268 RVA: 0x0000DBA0 File Offset: 0x0000BDA0
		public event Action<PointerEventData> onPointerDown;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x0600010D RID: 269 RVA: 0x0000DBDC File Offset: 0x0000BDDC
		// (remove) Token: 0x0600010E RID: 270 RVA: 0x0000DC18 File Offset: 0x0000BE18
		public event Action<PointerEventData> onDrag;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x0600010F RID: 271 RVA: 0x0000DC54 File Offset: 0x0000BE54
		// (remove) Token: 0x06000110 RID: 272 RVA: 0x0000DC90 File Offset: 0x0000BE90
		public event Action<PointerEventData> onPointerUp;

		// Token: 0x06000111 RID: 273 RVA: 0x0000DCCC File Offset: 0x0000BECC
		protected override void Awake()
		{
			base.Awake();
			this._cameraControl = UnityEngine.Object.FindObjectOfType<BaseCameraControl>();
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000DCE0 File Offset: 0x0000BEE0
		public void OnPointerDown(PointerEventData eventData)
		{
			if (this.preventCameraControl && this._cameraControl)
			{
				this._noControlFunctionCached = this._cameraControl.NoCtrlCondition;
				this._cameraControl.NoCtrlCondition = (() => true);
			}
			this._pointerDownCalled = true;
			this._cachedDragPosition = this.toDrag.position;
			this._cachedMousePosition = Input.mousePosition;
			Action<PointerEventData> action = this.onPointerDown;
			if (action == null)
			{
				return;
			}
			action(eventData);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000DD8C File Offset: 0x0000BF8C
		public void OnDrag(PointerEventData eventData)
		{
			if (!this._pointerDownCalled)
			{
				return;
			}
			this.toDrag.position = this._cachedDragPosition + ((Vector2)Input.mousePosition - this._cachedMousePosition);
			Action<PointerEventData> action = this.onDrag;
			if (action == null)
			{
				return;
			}
			action(eventData);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000DDF0 File Offset: 0x0000BFF0
		public void OnPointerUp(PointerEventData eventData)
		{
			if (!this._pointerDownCalled)
			{
				return;
			}
			if (this.preventCameraControl && this._cameraControl)
			{
				this._cameraControl.NoCtrlCondition = this._noControlFunctionCached;
			}
			this._pointerDownCalled = false;
			Action<PointerEventData> action = this.onPointerUp;
			if (action == null)
			{
				return;
			}
			action(eventData);
		}

		// Token: 0x040000C9 RID: 201
		private Vector2 _cachedDragPosition;

		// Token: 0x040000CA RID: 202
		private Vector2 _cachedMousePosition;

		// Token: 0x040000CB RID: 203
		private bool _pointerDownCalled;

		// Token: 0x040000CC RID: 204
		private BaseCameraControl _cameraControl;

		// Token: 0x040000CD RID: 205
		private BaseCameraControl.NoCtrlFunc _noControlFunctionCached;

		// Token: 0x040000D1 RID: 209
		public RectTransform toDrag;

		// Token: 0x040000D2 RID: 210
		public bool preventCameraControl;
	}
}
