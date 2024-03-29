﻿using HarmonyLib;
using System.Collections.Generic;
using Blasphemous.Randomizer.ItemRando;
using Blasphemous.Randomizer.HintRando;
using Blasphemous.Randomizer.Multiworld.AP;

namespace Blasphemous.Randomizer.Multiworld.Patches;

// Change randomization methods
[HarmonyPatch(typeof(Randomizer), "Randomize")]
class RandomizerRandomize_Patch
{
    public static bool Prefix(Randomizer __instance)
    {
        if (!Main.Multiworld.APManager.Connected)
            return true;

        Dictionary<string, string> mappedItems = Main.Multiworld.LoadMultiworldItems();
        Dictionary<string, string> mappedDoors = Main.Multiworld.LoadMultiworldDoors();
        int seed = Main.Randomizer.GameSeed;

        try
        {
            __instance.itemShuffler.LoadMappedItems(mappedItems); // Set item list from multiworld data
            __instance.itemShuffler.LoadMappedDoors(mappedDoors); // Set door list from multiworld data
            __instance.enemyShuffler.Shuffle(seed); // Uses built-in enemy shuffle
            __instance.hintShuffler.Shuffle(seed); // Uses built-in hint filler based on multiworld items
        }
        catch (System.Exception e)
        {
            Main.Multiworld.LogError("Error with filling: " + e.Message);
        }

        Main.Multiworld.Log(mappedItems.Count + " items have been inserted from multiworld!");
        return false;
    }
}

// Change hint text for other player's items
[HarmonyPatch(typeof(HintShuffle), "getHintText")]
class HintShuffleText_Patch
{
    public static void Postfix(ref string __result, string location)
    {
        Item item = Main.Randomizer.itemShuffler.getItemAtLocation(location);
        if (item == null || item.type != 200)
            return;

        // This is a valid location that holds another player's item
        ArchipelagoItem archItem = item as ArchipelagoItem;
        string itemHint = $"'{archItem.name}' for {archItem.PlayerName}";
        __result = __result.Replace("[AP]", itemHint);
        Main.Multiworld.APManager.ScoutLocation(location);
    }
}

// Send location check when giving item
[HarmonyPatch(typeof(ItemShuffle), "giveItem")]
class ItemShuffleLocation_Patch
{
    public static void Postfix(string locationId)
    {
        Main.Multiworld.APManager.SendLocation(locationId);
    }
}

// Return archipelago item when checking location
[HarmonyPatch(typeof(ItemShuffle), "getItemAtLocation")]
class ItemShuffleItem_Patch
{
    public static bool Prefix(string locationId, ref Item __result)
    {
        Dictionary<string, string> mappedItems = Main.Randomizer.itemShuffler.SaveMappedItems();

        if (mappedItems == null || !mappedItems.ContainsKey(locationId) || !mappedItems[locationId].StartsWith("AP"))
            return true;

        __result = Main.Multiworld.APManager.GetAPItem(mappedItems[locationId]);
        return false;
    }
}
