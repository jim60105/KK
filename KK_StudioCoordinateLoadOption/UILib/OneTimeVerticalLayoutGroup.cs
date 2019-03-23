using System;
using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
	// Token: 0x02000026 RID: 38
	internal class OneTimeVerticalLayoutGroup : VerticalLayoutGroup
	{
		// Token: 0x0600011B RID: 283 RVA: 0x0000DEB0 File Offset: 0x0000C0B0
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

		// Token: 0x0600011C RID: 284 RVA: 0x0000DEE0 File Offset: 0x0000C0E0
		protected override void OnDisable()
		{
		}

		// Token: 0x0600011D RID: 285 RVA: 0x0000DEE4 File Offset: 0x0000C0E4
		public void UpdateLayout()
		{
			base.enabled = true;
		}
	}
}
