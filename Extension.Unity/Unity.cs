using System;
using System.Collections;
using UnityEngine;

namespace Extension
{
    internal static class Unity
    {
        /// <summary>
        /// Excute this coroutine synchronously.
        /// </summary>
        /// <remarks>https://www.feelouttheform.net/unity3d-coroutine-synchronous/</remarks>
        public static void WaitCoroutine(this IEnumerator @this)
        {
            while (@this.MoveNext())
            {
                if (@this.Current != null)
                {
                    IEnumerator num;
                    try
                    {
                        num = (IEnumerator)@this.Current;
                    }
                    catch (InvalidCastException)
                    {
                        if (@this.Current.GetType() == typeof(WaitForSeconds))
                            Debug.LogWarning("Skipped call to WaitForSeconds. Use WaitForSecondsRealtime instead.");
                        return;  // Skip WaitForSeconds, WaitForEndOfFrame and WaitForFixedUpdate
                    }
                    num.WaitCoroutine();
                }
            }
        }
    }
}
