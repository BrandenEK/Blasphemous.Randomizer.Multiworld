using HarmonyLib;
using System.Collections.Generic;
using BlasphemousRandomizer;
using BlasphemousRandomizer.Fillers;
using BlasphemousRandomizer.Shufflers;
using BlasphemousRandomizer.Structures;
using BlasphemousMultiworld.Structures;
using Framework.FrameworkCore;
using Framework.Managers;

namespace BlasphemousMultiworld.Patches
{
    // Change randomization methods
    [HarmonyPatch(typeof(Randomizer), "Randomize")]
    public class RandomizerRandomize_Patch
    {
        public static bool Prefix(Randomizer __instance, int ___seed)
        {
            if (Main.Multiworld.connection.connected)
            {
                // Door shuffle
                __instance.itemShuffler.Shuffle(___seed); // Patch inserts multiworld locations
                __instance.hintShuffler.Shuffle(___seed); // Uses built-in hint filler based on multiworld items
                __instance.enemyShuffler.Shuffle(___seed); // Uses built-in enemy shuffle
            }
            return false;
        }
    }

    // Load new item locations into item shuffler
    [HarmonyPatch(typeof(ItemShuffle), "Shuffle")]
    public class ItemShuffleShuffle_Patch
    {
        public static bool Prefix(ItemShuffle __instance, ref Dictionary<string, Item> ___newItems)
        {
            Main.Multiworld.modifyNewItems(ref ___newItems);
            Main.Randomizer.totalItems = ___newItems.Count;
            Main.Randomizer.Log(___newItems.Count + " items have been inserted from multiworld!");
            return false;
        }
    }

    // Fix hint shuffle
    [HarmonyPatch(typeof(HintFiller), "fillGrtdHintLocations")]
    public class HintFillerGrtd_Patch
    {
        public static void Postfix(Dictionary<int, string> grtd)
        {
            grtd.Remove(34);
            grtd.Remove(32);
            grtd.Remove(30);
            grtd.Remove(6);
        }
    }
    [HarmonyPatch(typeof(HintFiller), "getHintText")]
    public class HintFillerText_Patch
    {
        public static void Postfix(ref string __result, string location, Item item, Dictionary<string, string> ___locationHints)
        {
            if (item.type != 200 || !___locationHints.TryGetValue(location, out string locationHint))
                return;

            // This is a valid location that holds another player's item
            ArchipelagoItem archItem = item as ArchipelagoItem;
            string itemHint = $"'{archItem.name}' for {archItem.playerName}";
            string output = locationHint.Replace("*", itemHint);
            __result = char.ToUpper(output[0]).ToString() + output.Substring(1) + "...";
        }
    }

    // Load data on new game
    [HarmonyPatch(typeof(Randomizer), "newGame")]
    public class RandomizerNew_Patch
    {
        public static void Postfix()
        {
            Main.Multiworld.newGame();
        }
    }

    // Get all items from the item filler
    [HarmonyPatch(typeof(ItemFiller), "addSpecialItems")] // Will be obselete when data storage is implemented
    public class ItemFiller_Patch
    {
        public static void Postfix(List<Item> items)
        {
            Main.Multiworld.allItems = items;
        }
    }

    // Send location check when giving item
    [HarmonyPatch(typeof(ItemShuffle), "giveItem")]
    public class ItemShuffleLocation_Patch
    {
        public static void Postfix(string locationId)
        {
            Main.Multiworld.sendLocation(locationId);
        }
    }

    // Set multiworld flag when starting new game
    [HarmonyPatch(typeof(Randomizer), "setUpExtras")]
    public class RandomizerSetup_Patch
    {
        public static void Postfix()
        {
            Core.Events.SetFlag("MULTIWORLD", true, false);
        }
    }

    // Show different notification when receiving an item
    [HarmonyPatch(typeof(Item), "getRewardInfo")]
    public class Item_Patch
    {
        public static void Postfix(ref RewardInfo __result)
        {
            if (Main.Multiworld.receivedPlayer != "")
            {
                __result.notification = "Receieved from " + Main.Multiworld.receivedPlayer + "!";
                Main.Multiworld.receivedPlayer = "";
            }
        }
    }
}
