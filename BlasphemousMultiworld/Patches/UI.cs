using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlasphemousRandomizer.Config;
using BlasphemousRandomizer.UI;
using Framework.Managers;
using Gameplay.UI.Widgets;
using Gameplay.UI.Others;
using Tools.Playmaker2.Action;

namespace BlasphemousMultiworld.Patches
{
    // Add multiworld version to main menu
    [HarmonyPatch(typeof(VersionNumber), "Start")]
    public class VersionNumberPatch
    {
        public static void Postfix(VersionNumber __instance)
        {
            Text version = __instance.GetComponent<Text>();
            if (version.text.Contains("v."))
                version.text = "";
            version.text += "Multiworld v" + PluginInfo.PLUGIN_VERSION + "\n";
        }
    }

    // Allow console commands on the main menu
    [HarmonyPatch(typeof(KeepFocus), "Update")]
    public class KeepFocus_Patch
    {
        public static bool Prefix()
        {
            return ConsoleWidget.Instance == null || !ConsoleWidget.Instance.IsEnabled();
        }
    }
    [HarmonyPatch(typeof(ConsoleWidget), "SetEnabled")]
    public class ConsoleWidgetDisable_Patch
    {
        public static void Postfix(bool enabled)
        {
            if (!enabled && Core.LevelManager.currentLevel.LevelName == "MainMenu")
            {
                Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>();
                foreach (Button b in buttons)
                {
                    if (b.name == "Continue")
                    {
                        EventSystem.current.SetSelectedGameObject(b.gameObject);
                        return;
                    }
                }
            }
        }
    }

    // Handle config for settings menu
    [HarmonyPatch(typeof(SettingsMenu), "setConfigSettings")]
    public class SettingsMenuConfig_Patch
    {
        public static void Prefix(ref MainConfig config)
        {
            Main.Multiworld.modifyGameConfig(config);
        }
    }
    [HarmonyPatch(typeof(SettingsMenu), "update")]
    public class SettingsMenuUpdate_Patch
    {
        public static void Postfix(ref Text ___descriptionText, GameObject ___settingsMenu, bool ___menuActive)
        {
            if (___settingsMenu != null && ___menuActive)
                ___descriptionText.text = "Configuration settings have been determined by Multiworld";
        }
    }
    [HarmonyPatch(typeof(SettingsMenu), "processKeyInput")]
    public class SettingsMenuKeyInput_Patch
    {
        public static bool Prefix()
        {
            return false;
        }
    }
    [HarmonyPatch(typeof(SettingsElement), "onClick")]
    public class SettingsElement_Patch
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    // Send goal completion on specific cutscenes
    [HarmonyPatch(typeof(CutscenePlay), "OnEnter")]
    public class CutscenePlay_Patch
    {
        public static void Postfix(CutscenePlay __instance)
        {
            if (__instance.cutscene != null)
            {
                string name = __instance.cutscene.name;
                if (name == "CTS10-EndingA")
                    Main.Multiworld.sendGoal(1);
                else if (name == "CTS09-EndingB")
                    Main.Multiworld.sendGoal(0);
                else if (name == "CTS301-EndingC")
                    Main.Multiworld.sendGoal(2);
            }
        }
    }

}
