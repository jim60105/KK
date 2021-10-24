using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using UnityEngine;

namespace CoordinateLoadOption.StringResources
{
    internal static class StringResourcesManager
    {
        public static CultureInfo UICulture { get; private set; } = CultureInfo.CreateSpecificCulture("en-US");

        /// <summary>
        /// 設定CurrentUICulture, Because of CultureFix, Thread.CurrentThread.CurrentUICulture can no longer be used as a basis for language display.
        /// </summary>
        /// <param name="culture">Culture Name (Ex: "en-US")，傳入Null則設定為系統語言</param>
        /// <returns></returns>
        internal static CultureInfo SetUICulture(string culture = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    return UICulture = CultureInfo.CreateSpecificCulture(culture);
                }
            }
            catch (CultureNotFoundException)
            {
                Extension.Logger.LogWarning("Unsuccessfully established the specified culture:" + culture);
            }

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseTraditional:
                case SystemLanguage.ChineseSimplified:
                    return UICulture = CultureInfo.CreateSpecificCulture("zh");
                case SystemLanguage.Japanese:
                    return UICulture = CultureInfo.CreateSpecificCulture("ja");
                default:
                    return UICulture = CultureInfo.CreateSpecificCulture("en");
            }
        }

        /// <summary>
        /// 取得資源內字串
        /// </summary>
        /// <param name="str">字串名稱</param>
        /// <param name="lang">Culture Name (Ex:"en-US")，使用SetUICulture設定Global值</param>
        /// <returns></returns>
        internal static string GetString(string str, string lang = "")
        {
            if (lang == "")
            {
                if (null == UICulture)
                {
                    UICulture = SetUICulture();
                }
                lang = UICulture.Name;
            }

            if (string.IsNullOrEmpty(str))
                Extension.Logger.LogDebug("Empty language query string");

            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream;
            stream = asm.GetManifestResourceStream($"CoordinateLoadOption.StringResources.StringResources.{lang}.resources");

            if (null == stream && lang.IndexOf("-") > 0)
            {
                lang = lang.Substring(0, lang.IndexOf("-"));
                stream = asm.GetManifestResourceStream($"CoordinateLoadOption.StringResources.StringResources.{lang}.resources");
            }

            // resource not found, revert to default resource
            if (null == stream)
            {
                stream = asm.GetManifestResourceStream($"CoordinateLoadOption.StringResources.StringResources.resources");
                if (lang != "en")
                {
                    Extension.Logger.LogDebug($"Language {lang} not found! Load English for default.");
                }
            }

            if (null != stream)
            {
                ResourceReader reader = new ResourceReader(stream);
                IDictionaryEnumerator en = reader.GetEnumerator();
                while (en.MoveNext())
                {
                    if (en.Key.Equals(str))
                    {
                        return en.Value.ToString();
                    }
                }
            }

            // string not translated, revert to default resource
            //return Properties.Resources.ResourceManager.GetString(str);
            return null;
        }
    }
}
