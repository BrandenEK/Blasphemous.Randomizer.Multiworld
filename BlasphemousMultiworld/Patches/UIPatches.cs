using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using BlasphemousRandomizer;
using BlasphemousRandomizer.Settings;

namespace BlasphemousMultiworld.Patches
{
    // Handle config for settings menu
    [HarmonyPatch(typeof(SettingsMenu), "setConfigSettings")]
    public class SettingsMenuConfig_Patch
    {
        public static void Prefix(ref Config config)
        {
            if (Main.Multiworld.APManager.Connected)
                config = Main.Multiworld.MultiworldSettings.Config;
        }
    }
    [HarmonyPatch(typeof(SettingsMenu), "update")]
    public class SettingsMenuUpdate_Patch
    {
        public static void Postfix(ref Text ___descriptionText, GameObject ___settingsMenu, bool ___menuActive)
        {
            if (Main.Multiworld.APManager.Connected && ___settingsMenu != null && ___menuActive)
                ___descriptionText.text = Main.Multiworld.Localize("config");
        }
    }
    [HarmonyPatch(typeof(SettingsMenu), "processKeyInput")]
    public class SettingsMenuKeyInput_Patch
    {
        public static bool Prefix()
        {
            return !Main.Multiworld.APManager.Connected;
        }
    }
    [HarmonyPatch(typeof(SettingsElement), "onClick")]
    public class SettingsElement_Patch
    {
        public static bool Prefix()
        {
            return !Main.Multiworld.APManager.Connected;
        }
    }
}
