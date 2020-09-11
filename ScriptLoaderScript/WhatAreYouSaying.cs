// #name りしれ供さ小 (What are you saying?)
// #author jim60105 
// #desc 看不見的話就是藝術
// v20.09.10.1
// v1.0.0
// https://meme.fandom.com/zh-tw/wiki/%E3%82%8A%E3%81%97%E3%82%8C%E4%BE%9B%E3%81%95%E5%B0%8F

using System;
using HarmonyLib;
using TMPro;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class WhatAreYouSaying{
    // Modify this array to change the display texts
    private static readonly char[] randomChar = new char[] {
        '♂',
        '♀',
        '▲',
        '▼',
        '●',
        '★',
        '☆',
        //'✔',  //The following does not work in Maker and Studio, but works in MainGame.
        //'✘',
        //'▶',
        //'◀',
        //'☀',
        //'☃',
        //'☂',
        //'☁',
        //'❄',
        //'✿',
        //'❁',
        //'❀',
        //'✦',
        //'♘',
        //'♞',
        //'↯',
        //'☈',
        //'☇',
        //'⤴',
        //'⤵',
        //'⇗',
        //'↩',
        //'↭',
        //'⇝',
        //'⇜',
    };

    static HarmonyLib.Harmony instance;
    public static void Main() {
        instance = HarmonyLib.Harmony.CreateAndPatchAll(typeof(WhatAreYouSaying));
    }

    public static void Unload() {
        instance?.UnpatchAll(instance?.Id);
        instance = null;
    }

    #region TMPro (Mainly for Maker & Studio)
    [HarmonyPrefix, HarmonyPatch(typeof(TMPro.TextMeshPro), "set_text")]
    public static void Set_textPrefix(ref string __0) => __0 = GetRandomString(__0);

    [HarmonyPostfix, HarmonyPatch(typeof(TMPro.TextMeshPro), "Awake")]
    public static void AwakePostfix(TMPro.TextMeshPro __instance) => doAwake(__instance);

    [HarmonyPrefix, HarmonyPatch(typeof(TMPro.TextMeshProUGUI), "set_text")]
    public static void Set_textPrefix2(ref string __0) => __0 = GetRandomString(__0);

    [HarmonyPostfix, HarmonyPatch(typeof(TMPro.TextMeshProUGUI), "Awake")]
    public static void AwakePostfix2(TMPro.TextMeshProUGUI __instance) => doAwake(__instance);

    private static void doAwake(TMPro.TMP_Text __instance) {
        try {
            __instance.text = GetRandomString(
                (string)typeof(TMPro.TMP_Text)
                .GetField("m_text", AccessTools.all)
                .GetValue(__instance as TMPro.TMP_Text));
        } catch (ArgumentException) { }
    }
    #endregion

    #region ADV MessageBox (For MainGame)
    [HarmonyPrefix, HarmonyPatch(typeof(TextController), "Set")]
    public static void SetPrefix(ref string __1) => __1 = GetRandomString(__1);
    #endregion

    private static readonly System.Random random = new System.Random();
    private static string GetRandomString(string str) {
        char[] chars = str?.ToCharArray();
        if (null == chars) return "";

        for (int i = 0; i < chars.Length; i++) {
            chars[i] = randomChar[random.Next(randomChar.Length)];
        }
        return new string(chars);
    }
}