﻿using Framework.Dialog;
using Framework.Managers;
using Gameplay.UI.Others.MenuLogic;
using Gameplay.UI.Widgets;
using HarmonyLib;
using System.Collections.Generic;
using Tools.Playmaker2.Action;
using UnityEngine.UI;
using static UnityEngine.UI.ContentSizeFitter;

namespace Blasphemous.Randomizer.Multiworld.Patches;

// Show whether a save file was started in multiworld
[HarmonyPatch(typeof(SelectSaveSlots), "SetAllData")]
class SelectSaveSlotsData_Patch
{
    public static void Postfix(List<SaveSlot> ___slots)
    {
        for (int i = 0; i < ___slots.Count; i++)
        {
            PersistentManager.PublicSlotData slotData = Core.Persistence.GetSlotData(i);
            if (slotData == null)
                continue;

            // Check if this save file was played in multiworld
            string type = $"({Main.Multiworld.LocalizationHandler.Localize("vandis")})";
            if (slotData.flags.flags.ContainsKey("MULTIWORLD"))
                type = $"({Main.Multiworld.LocalizationHandler.Localize("muldis")})";
            else if (slotData.flags.flags.ContainsKey("RANDOMIZED"))
                type = $"({Main.Multiworld.LocalizationHandler.Localize("sindis")})";

            // Send extra info to the slot
            ___slots[i].SetData("ignorealso", type, 0, false, false, false, 0, SelectSaveSlots.SlotsModes.Normal);
        }
    }
}
[HarmonyPatch(typeof(SaveSlot), "SetData")]
class SaveSlotData_Patch
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

// Check if there are any input blockers
[HarmonyPatch(typeof(InputManager), "HasBlocker")]
class InputManager_Patch
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

// Send goal completion on specific cutscenes
[HarmonyPatch(typeof(CutscenePlay), "OnEnter")]
class CutscenePlay_Patch
{
    public static void Postfix(CutscenePlay __instance)
    {
        if (__instance.cutscene == null) return;
        string name = __instance.cutscene.name;

        int acquiredEnding, chosenEnding = Main.Multiworld.MultiworldSettings.RequiredEnding;
        if (name == "CTS10-EndingA")
            acquiredEnding = 1;
        else if (name == "CTS09-EndingB")
            acquiredEnding = 0;
        else if (name == "CTS301-EndingC")
            acquiredEnding = 2;
        else
            return;

        if (acquiredEnding >= chosenEnding)
        {
            Main.Multiworld.Log($"Completing goal {chosenEnding} with ending {acquiredEnding}!");
            Main.Multiworld.APManager.SendGoal();
        }
    }
}

// Send hint when looking at shop items, and show goal text for tirso
[HarmonyPatch(typeof(DialogManager), "StartConversation")]
class DialogManager_Patch
{
    public static void Prefix(string conversiationId, Dictionary<string, DialogObject> ___allDialogs)
    {
        // If talking to Tirso, change text to current goal
        if (conversiationId == "DLG_0302")
        {
            DialogObject current = ___allDialogs[conversiationId];
            string tirsoText = Main.Multiworld.LocalizationHandler.Localize(Main.Multiworld.APManager.Connected
                ? "tirse" + Main.Multiworld.MultiworldSettings.RequiredEnding
                : "tirseu");

            current.dialogLines.Clear();
            current.dialogLines.Add(tirsoText);
            return;
        }

        // If starting dialog for a shop item, send a hint
        string location;
        switch (conversiationId)
        {
            case "DLG_QT_1001": location = "QI58"; break;
            case "DLG_QT_1002": location = "RB05"; break;
            case "DLG_QT_1003": location = "RB09"; break;
            case "DLG_QT_1004": location = "QI11"; break;
            case "DLG_QT_1005": location = "RB37"; break;
            case "DLG_QT_1006": location = "RB02"; break;
            case "DLG_QT_1007": location = "QI71"; break;
            case "DLG_QT_1008": location = "RB12"; break;
            case "DLG_QT_1009": location = "QI49"; break;
            default: return;
        }

        Main.Multiworld.APManager.ScoutLocation(location);
    }
}

// Send hint when in a shrine menu
[HarmonyPatch(typeof(NewInventory_LayoutSkill), "ShowLayout")]
class InvSkillShow_Patch
{
    public static void Prefix(bool editMode) => InvDescription_Patch.EditFlag = editMode;
}
[HarmonyPatch(typeof(NewInventory_LayoutSkill), "CancelEditMode")]
class InvSkillCancel_Patch
{
    public static void Postfix() => InvDescription_Patch.EditFlag = false;
}
[HarmonyPatch(typeof(NewInventory_Description), "SetKill")]
class InvDescription_Patch
{
    public static void Postfix(string skillId)
    {
        // Only hint for items at shrine that are visible
        if (EditFlag && Core.SkillManager.CanUnlockSkillNoCheckPoints(skillId))
        {
            Main.Multiworld.APManager.ScoutLocation(skillId);
        }
    }

    public static bool EditFlag { get; set; }
}
