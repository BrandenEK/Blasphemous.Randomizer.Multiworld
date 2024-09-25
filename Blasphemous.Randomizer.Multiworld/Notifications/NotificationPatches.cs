﻿using Blasphemous.ModdingAPI;
using Gameplay.UI.Others.MenuLogic;
using Gameplay.UI.Widgets;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Blasphemous.Randomizer.Multiworld.Notifications;

// Send receive notification data to the item receiver
[HarmonyPatch(typeof(PopupAchievementWidget), nameof(PopupAchievementWidget.Awake))]
class PopupAchievementWidget_Patch
{
    public static void Postfix(RectTransform ___PopUp)
    {
        Main.Multiworld.NotificationManager.ImageBackground = ___PopUp.GetComponent<Image>().sprite;
        Main.Multiworld.NotificationManager.TextFont = ___PopUp.GetChild(1).GetComponent<Text>().font;
    }
}
[HarmonyPatch(typeof(NewInventory_GridItem), nameof(NewInventory_GridItem.Awake))]
class InvGridItem_Patch
{
    public static void Postfix(Sprite ___backEquipped)
    {
        Main.Multiworld.NotificationManager.ImageBox = ___backEquipped;
    }
}

// Make the console use rich text
[HarmonyPatch(typeof(ConsoleWidget), nameof(ConsoleWidget.Write))]
class ConsoleWrite_Patch
{
    public static void Postfix(ConsoleWidget __instance, ref bool ___scrollToBottom)
    {
        try
        {
            Text lastText = __instance.content.GetChild(__instance.content.childCount - 1).GetComponent<Text>();
            lastText.supportRichText = true;
        }
        catch (System.Exception)
        {
            ModLog.Warn("Failed to change console line to rich text");
        }
        ___scrollToBottom = true;
    }
}
