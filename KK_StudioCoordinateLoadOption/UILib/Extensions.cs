using System;
using System.Collections;
using UnityEngine;

namespace UILib
{
	// Token: 0x02000023 RID: 35
	internal static class Extensions
	{
		// Token: 0x06000109 RID: 265 RVA: 0x0000DB3C File Offset: 0x0000BD3C
		internal static void ExecuteDelayed(this MonoBehaviour self, Action action, int waitCount = 1)
		{
			self.StartCoroutine(Extensions.ExecuteDelayed_Routine(action, waitCount));
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000DB4C File Offset: 0x0000BD4C
		private static IEnumerator ExecuteDelayed_Routine(Action action, int waitCount)
		{
			int num;
			for (int i = 0; i < waitCount; i = num)
			{
				yield return null;
				num = i + 1;
			}
			action();
			yield break;
		}
	}
}
