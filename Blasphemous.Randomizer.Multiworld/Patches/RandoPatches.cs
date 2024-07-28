using HarmonyLib;
using System.Collections.Generic;
using Blasphemous.Randomizer.ItemRando;
using Blasphemous.Randomizer.HintRando;
using Blasphemous.Randomizer.Multiworld.Models;

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
            Main.Multiworld.LogError("Error with filling: " + e.Message);
        }

        Main.Multiworld.Log("Overrode randomizer shuffle");
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
        if (item == null || item is not MultiworldOtherItem otherItem)
            return;

        // This is a valid location that holds another player's item
        string itemHint = $"'{otherItem.name}' for {otherItem.PlayerName}";
        __result = __result.Replace("[AP]", itemHint);
        Main.Multiworld.APManager.ScoutLocation(location);
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
