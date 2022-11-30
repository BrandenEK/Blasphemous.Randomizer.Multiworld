using System;
using System.Collections.Generic;
using Gameplay.UI.Widgets;
using Gameplay.UI.Console;
using HarmonyLib;
using UnityEngine.UI;
using BlasphemousRandomizer.Fillers;
using BlasphemousRandomizer.Structures;

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

    // Get all items from the item filler
    [HarmonyPatch(typeof(ItemFiller), "addSpecialItmes")]
    public class ItemFiller_Patch
    {
        public static void Postfix(List<Item> items)
        {
            Main.Multiworld.allItems = items;
        }
    }
}
