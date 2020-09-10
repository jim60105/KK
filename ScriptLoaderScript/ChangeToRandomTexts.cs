// #name Change to Random texts
// #author jim60105 
// #desc For Fun~~
// v20.09.10.0
// v1.0.0

using System;
using HarmonyLib;
using TMPro;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class ChangeToRandomTexts {
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
        instance = HarmonyLib.Harmony.CreateAndPatchAll(typeof(ChangeToRandomTexts));
    }

    public static void Unload() {
        instance?.UnpatchAll(instance?.Id);
        instance = null;
    }

    //TMPro
    [HarmonyPrefix, HarmonyPatch(typeof(TMPro.TextMeshPro), "set_text")]
    public static void set_textPrefix(ref string __0) => __0 = GetRandomChar(__0);

    [HarmonyPostfix, HarmonyPatch(typeof(TMPro.TextMeshPro), "Awake")]
    public static void AwakePostfix(TMPro.TextMeshPro __instance) => doAwake(__instance);

    [HarmonyPrefix, HarmonyPatch(typeof(TMPro.TextMeshProUGUI), "set_text")]
    public static void set_textPrefix2(ref string __0) => __0 = GetRandomChar(__0);

    [HarmonyPostfix, HarmonyPatch(typeof(TMPro.TextMeshProUGUI), "Awake")]
    public static void AwakePostfix2(TMPro.TextMeshProUGUI __instance) => doAwake(__instance);

    private static void doAwake(TMPro.TMP_Text __instance) {
        try {
            __instance.text = GetRandomChar(
                (string)typeof(TMPro.TMP_Text)
                .GetField("m_text", AccessTools.all)
                .GetValue(__instance as TMPro.TMP_Text));
        } catch (ArgumentException) { }
    }

    //ADV MessageBox
    [HarmonyPrefix, HarmonyPatch(typeof(TextController), "Set")]
    public static void SetPrefix(ref string __1) => __1 = GetRandomChar(__1);

    private static Random random = new Random();
    private static string GetRandomChar(string str) {
        char[] chars = str?.ToCharArray();
        if (null == chars) return "";

        for (int i = 0; i < chars.Length; i++) {
            chars[i] = randomChar[random.Next(randomChar.Length)];
        }
        return new string(chars);
    }
}