using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using UnityEngine;

namespace KK_CoordinateLoadOption.StringResources {
    internal static class StringResourcesManager {
        public static CultureInfo UICulture { get; private set; }

        /// <summary>
        /// 設定CurrentUICulture
        /// </summary>
        /// <param name="culture">Culture Name (Ex: "en-US")，傳入Null則設定為系統語言</param>
        /// <returns>以Culture Name建立的CultureInfo</returns>
        internal static CultureInfo SetUICulture(string culture = null) {
            try {
                return null == culture
                    ? (UICulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(x => x.EnglishName.Equals(Application.systemLanguage.ToString())))
                    : (UICulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(culture));
            } catch (Exception) {
                KK_CoordinateLoadOption.Logger.LogInfo($"Language not found. Keep {UICulture.Name}");
                return Thread.CurrentThread.CurrentUICulture = UICulture;
            }
        }

        /// <summary>
        /// 取得資源內字串
        /// </summary>
        /// <param name="str">字串名稱</param>
        /// <param name="lang">Culture Name (Ex:"en-US")，使用SetUICulture設定Global值</param>
        /// <returns></returns>
        internal static string GetString(string str, string lang = "") {
            if (lang == "") {
                if (null == UICulture) {
                    SetUICulture();
                }
                lang = UICulture.Name;
            }

            if (string.IsNullOrEmpty(str))
                KK_CoordinateLoadOption.Logger.LogInfo("Empty language query string");

            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream;
            stream = asm.GetManifestResourceStream($"{asm.GetName().Name}.StringResources.StringResources.{lang}.resources");

            if (null == stream && lang.IndexOf("-") > 0) {
                lang = lang.Substring(0, lang.IndexOf("-"));
                stream = asm.GetManifestResourceStream($"{asm.GetName().Name}.StringResources.StringResources.{lang}.resources");
            }

            // resource not found, revert to default resource
            if (null == stream) {
                stream = asm.GetManifestResourceStream($"{asm.GetName().Name}.StringResources.StringResources.resources");
                if (lang != "en") {
                    KK_CoordinateLoadOption.Logger.LogDebug($"Language {lang} not found! Load English for default.");
                }
            }

            if (null != stream) {
                ResourceReader reader = new ResourceReader(stream);
                IDictionaryEnumerator en = reader.GetEnumerator();
                while (en.MoveNext()) {
                    if (en.Key.Equals(str)) {
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
