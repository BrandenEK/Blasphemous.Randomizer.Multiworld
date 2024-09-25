using Blasphemous.ModdingAPI;
using Blasphemous.Randomizer.HintRando;
using Blasphemous.Randomizer.ItemRando;
using Blasphemous.Randomizer.Multiworld.Models;
using HarmonyLib;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.Multiworld.Patches;

// Change randomization methods
[HarmonyPatch(typeof(Randomizer), "Randomize")]
class RandomizerRandomize_Patch
{
    public static bool Prefix(Randomizer __instance)
    {
        int seed = Main.Randomizer.GameSettings.Seed;

        try
        {
            var mappedItems = new Dictionary<string, string>()
            {
                { "AP", "AP" }
            };

            __instance.itemShuffler.LoadMappedItems(mappedItems); // Just add one location to keep valid seed
            __instance.itemShuffler.LoadMappedDoors(null); // No door shuffle
            __instance.enemyShuffler.Shuffle(seed); // Uses built-in enemy shuffle
            __instance.hintShuffler.Shuffle(seed); // Uses built-in hint filler based on multiworld items
        }
        catch (System.Exception e)
        {
            ModLog.Error("Error with filling: " + e.Message);
        }

        ModLog.Info("Overrode randomizer shuffle");
        return false;
    }
}

// Change hint text for other player's items
[HarmonyPatch(typeof(HintShuffle), "getHintText")]
class HintShuffleText_Patch
{
    public static bool Prefix(ref string __result, string location)
    {
        string locationHint, itemHint;

        if (location == "SIERPES")
        {
            Item item1 = Main.Randomizer.itemShuffler.getItemAtLocation("BossTrigger[5000]");
            Item item2 = Main.Randomizer.itemShuffler.getItemAtLocation("QI202");
            locationHint = Main.Randomizer.data.itemLocations["QI202"].Hint;
            itemHint = $"{GetHintForItem(item1)} and {GetHintForItem(item2)}";

            Main.Multiworld.APManager.ScoutLocation("BossTrigger[5000]");
            Main.Multiworld.APManager.ScoutLocation("QI202");
        }
        else
        {
            Item item = Main.Randomizer.itemShuffler.getItemAtLocation(location);
            locationHint = Main.Randomizer.data.itemLocations[location].Hint;
            itemHint = GetHintForItem(item);

            Main.Multiworld.APManager.ScoutLocation(location);
        }

        string output = locationHint.Replace("*", itemHint);
        __result = char.ToUpper(output[0]).ToString() + output.Substring(1) + "...";
        return false;
    }

    private static string GetHintForItem(Item item)
    {
        if (item?.hint == null)
            return "???";

        if (item is not MultiworldOtherItem other)
            return item.hint;

        return $"'{other.name}' for {other.PlayerName}";
    }
}

// Return archipelago item when checking location
[HarmonyPatch(typeof(ItemShuffle), "getItemAtLocation")]
class ItemShuffleItem_Patch
{
    public static bool Prefix(string locationId, ref Item __result)
    {
        __result = Main.Multiworld.LocationScouter.GetItemAtLocation(locationId);
        return false;
    }
}
