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
        public static void Postfix(GameObject ___settingsMenu, bool ___menuActive)
        {
            if (___settingsMenu != null && ___menuActive)
            {
                Text multiworldText = Main.Multiworld.MultiworldStatusText;
                if (Main.Multiworld.APManager.Connected)
                {
                    if (!Main.Multiworld.HasRequiredMods)
                    {
                        multiworldText.color = Color.red;
                        multiworldText.text = Main.Multiworld.Localize("seterr");
                    }
                    else
                    {
                        multiworldText.color = Color.green;
                        multiworldText.text = Main.Multiworld.Localize("setcon");
                    }
                }
                else
                {
                    multiworldText.color = Color.yellow;
                    multiworldText.text = Main.Multiworld.Localize("setnon");
                }
            }
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
    [HarmonyPatch(typeof(SettingsMenu), "beginGame")]
    public class SettingsMenuBegin_Patch
    {
        public static bool Prefix()
        {
            return !Main.Multiworld.APManager.Connected || Main.Multiworld.HasRequiredMods;
        }
    }
}
