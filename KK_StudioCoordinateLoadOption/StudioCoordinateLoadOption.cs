using System;
using System.Collections.Generic;
using System.Reflection;
using ChaCustom;
using Illusion.Game;
using MessagePack;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UniRx;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace KK_StudioCoordinateLoadOption
{
	// Token: 0x02000003 RID: 3
	public class StudioCoordinateLoadOption : MonoBehaviour
	{
		// Token: 0x06000004 RID: 4 RVA: 0x00002094 File Offset: 0x00000294
		public void Awake()
        {
            Console.WriteLine("****My Awak");
            SceneManager.sceneLoaded += this.OnSceneLoad;
        }

        // Token: 0x06000002 RID: 2 RVA: 0x00002068 File Offset: 0x00000268
        public void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            this.studioScene = UnityEngine.Object.FindObjectOfType<StudioScene>();
            bool flag = this.studioScene == null;
            if (flag)
            {
                base.enabled = false;
            }
            else
            {
                base.enabled = true;
                base.StartCoroutine(this.AddBtnEvent());
            }
        }

        // Token: 0x06000006 RID: 6 RVA: 0x000020DE File Offset: 0x000002DE
        public IEnumerator AddBtnEvent()
		{
            if (this.tglWears != null)
			{
                yield break;
			}
			Button btnCoordeLoadLoad = Singleton<CustomFileWindow>.Instance.btnCoordeLoadLoad;
			btnCoordeLoadLoad.onClick.RemoveListener(new UnityAction(this.OnCoordeLoadLoadClick));
			btnCoordeLoadLoad.onClick.AddListener(new UnityAction(this.OnCoordeLoadLoadClick));
			yield return null;
			if (this.idisposable_btnCoordeLoadLoad != null)
			{
				this.idisposable_btnCoordeLoadLoad.Dispose();
			}
			this.idisposable_btnCoordeLoadLoad = btnCoordeLoadLoad.OnClickAsObservable().Subscribe(delegate(Unit _)
			{
				this.OnCoordinateBtnClick();
			});
			GameObject gameObject = UnityEngine.Object.FindObjectOfType<CustomBase>().gameObject;
			GameObject gameObject2 = gameObject.transform.Find("FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad/Select").gameObject;
			Vector2 sizeDelta = gameObject2.GetComponent<RectTransform>().sizeDelta;
			gameObject2.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + 32f);
			GameObject gameObject3 = gameObject2.transform.Find("tglItem01").gameObject;
			gameObject3.GetComponent<Toggle>().onValueChanged.AddListener(new UnityAction<bool>(this.OnValueChenged_Clothes));
			GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(gameObject3, gameObject2.transform);
			GameObject gameObject5 = UnityEngine.Object.Instantiate<GameObject>(gameObject3, gameObject2.transform);
			GameObject gameObject6 = UnityEngine.Object.Instantiate<GameObject>(gameObject3, gameObject2.transform);
			GameObject gameObject7 = UnityEngine.Object.Instantiate<GameObject>(gameObject3, gameObject2.transform);
			GameObject gameObject8 = UnityEngine.Object.Instantiate<GameObject>(gameObject3, gameObject2.transform);
			GameObject gameObject9 = UnityEngine.Object.Instantiate<GameObject>(gameObject3, gameObject2.transform);
			gameObject4.name = "tglWears";
			gameObject5.name = "tglUnderWears";
			gameObject6.name = "tglGloves";
			gameObject7.name = "tglPanst";
			gameObject8.name = "tglSocks";
			gameObject9.name = "tglShoes";
			gameObject4.GetComponentInChildren<TextMeshProUGUI>().text = "服上下";
			gameObject5.GetComponentInChildren<TextMeshProUGUI>().text = "下着";
			gameObject6.GetComponentInChildren<TextMeshProUGUI>().text = "手袋";
			gameObject7.GetComponentInChildren<TextMeshProUGUI>().text = "パンスト";
			gameObject8.GetComponentInChildren<TextMeshProUGUI>().text = "靴下";
			gameObject9.GetComponentInChildren<TextMeshProUGUI>().text = "靴";
			RectTransform component = gameObject4.GetComponent<RectTransform>();
			RectTransform component2 = gameObject5.GetComponent<RectTransform>();
			RectTransform component3 = gameObject6.GetComponent<RectTransform>();
			RectTransform component4 = gameObject7.GetComponent<RectTransform>();
			RectTransform component5 = gameObject8.GetComponent<RectTransform>();
			RectTransform component6 = gameObject9.GetComponent<RectTransform>();
			component.sizeDelta = new Vector2(component.sizeDelta.x + 40f, component.sizeDelta.y);
			component2.sizeDelta = new Vector2(component2.sizeDelta.x + 20f, component2.sizeDelta.y);
			component3.sizeDelta = new Vector2(component3.sizeDelta.x + 20f, component3.sizeDelta.y);
			component4.sizeDelta = new Vector2(component4.sizeDelta.x + 60f, component4.sizeDelta.y);
			component5.sizeDelta = new Vector2(component5.sizeDelta.x + 20f, component5.sizeDelta.y);
			component.anchoredPosition += new Vector2(0f, -30f);
			component2.anchoredPosition += new Vector2(100f, -30f);
			component3.anchoredPosition += new Vector2(180f, -30f);
			component4.anchoredPosition += new Vector2(260f, -30f);
			component5.anchoredPosition += new Vector2(380f, -30f);
			component6.anchoredPosition += new Vector2(460f, -30f);
			gameObject4.GetComponentInChildren<Image>().raycastTarget = true;
			gameObject5.GetComponentInChildren<Image>().raycastTarget = true;
			gameObject6.GetComponentInChildren<Image>().raycastTarget = true;
			gameObject7.GetComponentInChildren<Image>().raycastTarget = true;
			gameObject8.GetComponentInChildren<Image>().raycastTarget = true;
			gameObject9.GetComponentInChildren<Image>().raycastTarget = true;
			this.tglWears = gameObject4.GetComponent<Toggle>();
			this.tglUnderWears = gameObject5.GetComponent<Toggle>();
			this.tglGloves = gameObject6.GetComponent<Toggle>();
			this.tglPanst = gameObject7.GetComponent<Toggle>();
			this.tglSocks = gameObject8.GetComponent<Toggle>();
			this.tglShoes = gameObject9.GetComponent<Toggle>();
			Toggle[] tgls = new Toggle[]
			{
				this.tglWears,
				this.tglUnderWears,
				this.tglGloves,
				this.tglPanst,
				this.tglSocks,
				this.tglShoes
			};
			GameObject gameObject10 = UnityEngine.Object.Instantiate<GameObject>(gameObject.transform.Find("FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad/btnLoad").gameObject, gameObject2.transform, true);
			gameObject10.GetComponentInChildren<TextMeshProUGUI>().text = "全選択";
			RectTransform component7 = gameObject10.GetComponent<RectTransform>();
			component7.sizeDelta = component.sizeDelta;
			component7.anchorMin = component.anchorMin;
			component7.anchorMax = component.anchorMax;
			component7.pivot = component.pivot;
			component7.anchoredPosition = component.anchoredPosition + new Vector2(520f, 0f);
			this.btnSelectAll = gameObject10.GetComponent<Button>();
			this.btnSelectAll.onClick.RemoveAllListeners();
			this.btnSelectAll.onClick.AddListener(delegate()
			{
				bool flag = false;
				for (int i = 0; i < tgls.Length; i++)
				{
					if (!tgls[i].isOn)
					{
						flag = true;
					}
					tgls[i].isOn = true;
				}
				if (!flag)
				{
					for (int j = 0; j < tgls.Length; j++)
					{
						tgls[j].isOn = false;
					}
				}
			});
			yield break;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000020F0 File Offset: 0x000002F0
		private void OnValueChenged_Clothes(bool on)
		{
			if (this.tglWears == null)
			{
				return;
			}
			this.tglWears.interactable = on;
			this.tglUnderWears.interactable = on;
			this.tglGloves.interactable = on;
			this.tglPanst.interactable = on;
			this.tglSocks.interactable = on;
			this.tglShoes.interactable = on;
			this.btnSelectAll.interactable = on;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002160 File Offset: 0x00000360
		private void OnCoordeLoadLoadClick()
		{
			CustomFileListCtrl fieldValue = StudioCoordinateLoadOption.Utl.GetFieldValue<CustomCoordinateFile, CustomFileListCtrl>(Singleton<CustomCoordinateFile>.Instance, "listCtrl");
			this.lstFileInfoBackUp = StudioCoordinateLoadOption.Utl.GetFieldValue<CustomFileListCtrl, List<CustomFileInfo>>(fieldValue, "lstFileInfo");
			StudioCoordinateLoadOption.Utl.SetFieldValue<CustomFileListCtrl>(fieldValue, "lstFileInfo", new List<CustomFileInfo>());
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000021A0 File Offset: 0x000003A0
		public void OnCoordinateBtnClick()
		{
			CustomFileListCtrl fieldValue = StudioCoordinateLoadOption.Utl.GetFieldValue<CustomCoordinateFile, CustomFileListCtrl>(Singleton<CustomCoordinateFile>.Instance, "listCtrl");
			StudioCoordinateLoadOption.Utl.SetFieldValue<CustomFileListCtrl>(fieldValue, "lstFileInfo", this.lstFileInfoBackUp);
			ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
			ChaFileClothes clothes = chaCtrl.nowCoordinate.clothes;
			if (clothes.parts.Length != 9)
			{
				return;
			}
			byte[][] array = new byte[9][];
			for (int i = 0; i < 9; i++)
			{
				array[i] = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(clothes.parts[i]);
			}
			int[] array2 = new int[3];
			for (int j = 0; j < 3; j++)
			{
				array2[j] = clothes.subPartsId[j];
			}
			CustomFileWindow instance = Singleton<CustomFileWindow>.Instance;
			CustomFileInfoComponent selectTopItem = fieldValue.GetSelectTopItem();
			if (null != selectTopItem)
			{
				Utils.Sound.Play(SystemSE.ok_s);
				string fullPath = selectTopItem.info.FullPath;
				bool flag = instance.tglCoordeLoadClothes && instance.tglCoordeLoadClothes.isOn;
				bool flag2 = instance.tglCoordeLoadAcs && instance.tglCoordeLoadAcs.isOn;
				byte[] bytes = MessagePackSerializer.Serialize<ChaFileClothes>(chaCtrl.nowCoordinate.clothes);
				byte[] bytes2 = MessagePackSerializer.Serialize<ChaFileAccessory>(chaCtrl.nowCoordinate.accessory);
				chaCtrl.nowCoordinate.LoadFile(fullPath);
				if (!flag)
				{
					chaCtrl.nowCoordinate.clothes = MessagePackSerializer.Deserialize<ChaFileClothes>(bytes);
				}
				else
				{
					if (!this.tglWears.isOn)
					{
						chaCtrl.nowCoordinate.clothes.parts[0] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[0]);
						chaCtrl.nowCoordinate.clothes.parts[1] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[1]);
						for (int k = 0; k < 3; k++)
						{
							chaCtrl.nowCoordinate.clothes.subPartsId[k] = array2[k];
						}
					}
					if (!this.tglUnderWears.isOn)
					{
						chaCtrl.nowCoordinate.clothes.parts[2] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[2]);
						chaCtrl.nowCoordinate.clothes.parts[3] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[3]);
					}
					if (!this.tglGloves.isOn)
					{
						chaCtrl.nowCoordinate.clothes.parts[4] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[4]);
					}
					if (!this.tglPanst.isOn)
					{
						chaCtrl.nowCoordinate.clothes.parts[5] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[5]);
					}
					if (!this.tglSocks.isOn)
					{
						chaCtrl.nowCoordinate.clothes.parts[6] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[6]);
					}
					if (!this.tglShoes.isOn)
					{
						chaCtrl.nowCoordinate.clothes.parts[7] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[7]);
						chaCtrl.nowCoordinate.clothes.parts[8] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(array[8]);
					}
				}
				if (!flag2)
				{
					chaCtrl.nowCoordinate.accessory = MessagePackSerializer.Deserialize<ChaFileAccessory>(bytes2);
				}
				chaCtrl.Reload(false, true, true, true);
				chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.chaFile.status.coordinateType);
				Singleton<CustomBase>.Instance.updateCustomUI = true;
				Singleton<CustomHistory>.Instance.Add5(chaCtrl, new Func<bool, bool, bool, bool, bool>(chaCtrl.Reload), false, true, true, true);
			}
		}
        
		// Token: 0x04000005 RID: 5
		private List<CustomFileInfo> lstFileInfoBackUp;

		// Token: 0x04000006 RID: 6
		private IDisposable idisposable_btnCoordeLoadLoad;

		// Token: 0x04000007 RID: 7
		private Toggle tglWears;

		// Token: 0x04000008 RID: 8
		private Toggle tglUnderWears;

		// Token: 0x04000009 RID: 9
		private Toggle tglGloves;

		// Token: 0x0400000A RID: 10
		private Toggle tglPanst;

		// Token: 0x0400000B RID: 11
		private Toggle tglSocks;

		// Token: 0x0400000C RID: 12
		private Toggle tglShoes;

		// Token: 0x0400000D RID: 13
		private Button btnSelectAll;

        private Button _copyTransform;

        private Button _pasteTransform;

        private Button _resetTransform;

        private StudioScene studioScene;

        // Token: 0x02000004 RID: 4
        internal static class Utl
		{
			// Token: 0x0600000C RID: 12 RVA: 0x000024C8 File Offset: 0x000006C8
			internal static FieldInfo GetFieldInfo<T>(string name)
			{
				BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
				return typeof(T).GetField(name, bindingAttr);
			}

			// Token: 0x0600000D RID: 13 RVA: 0x000024EC File Offset: 0x000006EC
			internal static TResult GetFieldValue<T, TResult>(T inst, string name)
			{
				if (inst == null)
				{
					return default(TResult);
				}
				FieldInfo fieldInfo = StudioCoordinateLoadOption.Utl.GetFieldInfo<T>(name);
				if (fieldInfo == null)
				{
					return default(TResult);
				}
				return (TResult)((object)fieldInfo.GetValue(inst));
			}

			// Token: 0x0600000E RID: 14 RVA: 0x00002530 File Offset: 0x00000730
			public static void SetFieldValue<T>(object inst, string name, object val)
			{
				FieldInfo fieldInfo = StudioCoordinateLoadOption.Utl.GetFieldInfo<T>(name);
				if (fieldInfo != null)
				{
					fieldInfo.SetValue(inst, val);
				}
			}
		}
	}
}
