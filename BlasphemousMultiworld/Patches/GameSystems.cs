using HarmonyLib;
using System.Collections.Generic;
using Gameplay.UI.Widgets;
using Gameplay.UI.Console;
using Gameplay.UI.Others.MenuLogic;
using BlasphemousRandomizer.UI;
using Framework.Managers;
using UnityEngine.UI;

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

    // Show whether a save file was started in multiworld
    [HarmonyPatch(typeof(SelectSaveSlots), "SetAllData")]
    public class SelectSaveSlotsData_Patch
    {
        public static void Postfix(List<SaveSlot> ___slots)
        {
            for (int i = 0; i < ___slots.Count; i++)
            {
                PersistentManager.PublicSlotData slotData = Core.Persistence.GetSlotData(i);
                if (slotData == null)
                    continue;

                // Check if this save file was played in multiworld
                string type = "(Vanilla)";
                if (slotData.flags.flags.ContainsKey("MULTIWORLD"))
                    type = "(Multiworld)";
                else if (slotData.flags.flags.ContainsKey("RANDOMIZED"))
                    type = "(Singleworld)";

                // Send extra info to the slot
                ___slots[i].SetData("ignorealso", type, 0, false, false, false, 0, SelectSaveSlots.SlotsModes.Normal);
            }
        }
    }
    [HarmonyPatch(typeof(SaveSlot), "SetData")]
    public class SaveSlotData_Patch
    {
        public static bool Prefix(string zoneName, string info, ref Text ___ZoneText)
        {
            if (zoneName == "ignorealso")
            {
                int startIdx = ___ZoneText.text.IndexOf('(');
                ___ZoneText.text = ___ZoneText.text.Substring(0, startIdx) + info;
                return false;
            }
            return true;
        }
    }

    // Don't allow to open a save file unless connected
    [HarmonyPatch(typeof(SelectSaveSlots), "OnAcceptSlots")]
    public class SelectSaveSlotsBegin_Patch
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

    // Check if there are any input blockers
    [HarmonyPatch(typeof(InputManager), "HasBlocker")]
    public class InputManager_Patch
    {
        public static bool Prefix(string name, ref bool __result, List<string> ___inputBlockers)
        {
            if (name == "*")
            {
                __result = ___inputBlockers.Count > 0 && ___inputBlockers[0] != "PLAYER_LOGIC";
                return false;
            }
            return true;
        }
    }

    // Initialize Multiworld class
    [HarmonyPatch(typeof(AchievementsManager), "AllInitialized")]
    public class AchievementsManager_InitializePatch
    {
        public static void Postfix()
        {
            Main.Multiworld.Initialize();
        }
    }
    // Dispose Multiworld class
    [HarmonyPatch(typeof(AchievementsManager), "Dispose")]
    public class AchievementsManager_DisposePatch
    {
        public static void Postfix()
        {
            Main.Multiworld.Dispose();
        }
    }
}
