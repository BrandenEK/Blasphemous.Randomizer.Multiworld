using HarmonyLib;
using System.Collections.Generic;
using Gameplay.UI.Widgets;
using Gameplay.UI.Console;
using Gameplay.UI.Others.MenuLogic;
using BlasphemousRandomizer.UI;

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
}
