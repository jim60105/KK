using System;
using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
	// Token: 0x02000025 RID: 37
	internal class OneTimeHorizontalLayoutGroup : HorizontalLayoutGroup
	{
		// Token: 0x06000116 RID: 278 RVA: 0x0000DE5C File Offset: 0x0000C05C
		protected override void OnEnable()
		{
			base.OnEnable();
			if (!Application.isEditor || Application.isPlaying)
			{
				this.ExecuteDelayed(delegate
				{
					base.enabled = false;
				}, 3);
			}
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000DE8C File Offset: 0x0000C08C
		protected override void OnDisable()
		{
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000DE90 File Offset: 0x0000C090
		public void UpdateLayout()
		{
			base.enabled = true;
		}
	}
}
