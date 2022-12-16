using System;
using System.Collections.Generic;
using Gameplay.UI.Widgets;
using Gameplay.UI.Console;
using Gameplay.UI.Others.MenuLogic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlasphemousRandomizer;
using BlasphemousRandomizer.Fillers;
using BlasphemousRandomizer.Shufflers;
using BlasphemousRandomizer.Structures;
using BlasphemousRandomizer.Config;
using BlasphemousRandomizer.UI;
using Framework.Managers;
using Gameplay.UI.Others;
using Framework.FrameworkCore;

namespace BlasphemousMultiworld
{
    // Add multiworld commands to console
    [HarmonyPatch(typeof(ConsoleWidget), "InitializeCommands")]
    public class Console_Patch
    {
        public static void Postfix(List<ConsoleCommand> ___commands)
        {
            ___commands.Add(new MultiworldCommand());
        }
    }

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

    // Get all items from the item filler
    [HarmonyPatch(typeof(ItemFiller), "addSpecialItems")]
    public class ItemFiller_Patch
    {
        public static void Postfix(List<Item> items)
        {
            Main.Multiworld.allItems = items;
        }
    }

    // Send location check when giving item
    [HarmonyPatch(typeof(ItemShuffle), "giveItem")]
    public class ItemShuffle_Patch
    {
        public static void Postfix(string locationId)
        {
            Main.Multiworld.sendLocation(locationId);
        }
    }

    // Change randomization methods
    [HarmonyPatch(typeof(Randomizer), "Randomize")]
    public class RandomizerRandomize_Patch
    {
        public static bool Prefix(Randomizer __instance, int ___seed)
        {
            if (Main.Multiworld.connection.connected)
            {
                __instance.itemShuffler.Shuffle(___seed);
                Main.Multiworld.modifyNewItems(__instance.itemShuffler.getNewItems()); // Change to not randomize items first before replacing them
                //__instance.hintShuffler.Shuffle(___seed);
                __instance.enemyShuffler.Shuffle(___seed);
            }
            return false;
        }
    }

    // Set gameStatus when loading or exiting
    [HarmonyPatch(typeof(Randomizer), "SetCurrentPersistentState")]
    public class RandomizerLoad_Patch
    {
        public static void Postfix()
        {
            Main.Multiworld.gameStatus = true;
            Main.Multiworld.processItems();
        }
    }
    [HarmonyPatch(typeof(Randomizer), "onLevelLoaded")]
    public class RandomizerLevelLoad_Patch
    {
        public static void Postfix(Level newLevel)
        {
            if (newLevel.LevelName == "MainMenu")
                Main.Multiworld.gameStatus = false;
        }
    }
    [HarmonyPatch(typeof(Randomizer), "newGame")]
    public class RandomizerNew_Patch
    {
        public static void Postfix()
        {
            Main.Multiworld.gameStatus = true;
            Main.Multiworld.processItems();
        }
    }

    // Don't allow to open a save file unless connected
    [HarmonyPatch(typeof(SelectSaveSlots), "OnAcceptSlots")]
    public class SelectSaveSlots_Patch
    {
        public static bool Prefix()
        {
            if (!Main.Multiworld.connection.connected)
            {
                Main.Randomizer.LogDisplay("Not connected to a multiworld server!");
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(SettingsMenu), "openMenu")]
    public class SettingsMenuOpen_Patch
    {
        public static bool Prefix()
        {
            if (!Main.Multiworld.connection.connected)
            {
                Main.Randomizer.LogDisplay("Not connected to a multiworld server!");
                return false;
            }
            return true;
        }
    }

    // Handle config for settings menu
    [HarmonyPatch(typeof(SettingsMenu), "setConfigSettings")]
    public class SettingsMenuConfig_Patch
    {
        public static void Prefix(ref MainConfig config)
        {
            config = MainConfig.Default();
            config.items.type = 0;
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
}
