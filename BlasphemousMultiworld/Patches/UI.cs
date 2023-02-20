using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using BlasphemousRandomizer.Config;
using BlasphemousRandomizer.UI;
using Gameplay.UI.Others.MenuLogic;

namespace BlasphemousMultiworld.Patches
{
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

    // Send receive notification data to the item receiver
    [HarmonyPatch(typeof(PopupAchievementWidget), "Awake")]
    public class PopupAchievementWidget_Patch
    {
        public static void Postfix(RectTransform ___PopUp)
        {
            Main.Multiworld.itemReceiver.backgroundSprite = ___PopUp.GetComponent<Image>().sprite;
            Main.Multiworld.itemReceiver.textFont = ___PopUp.GetChild(1).GetComponent<Text>().font;
        }
    }
    [HarmonyPatch(typeof(NewInventory_GridItem), "Awake")]
    public class InvGridItem_Patch
    {
        public static void Postfix(Sprite ___backEquipped)
        {
            Main.Multiworld.itemReceiver.boxSprite = ___backEquipped;
        }
    }
}
