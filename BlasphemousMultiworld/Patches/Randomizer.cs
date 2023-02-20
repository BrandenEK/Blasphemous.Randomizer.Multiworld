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

    // Change hint text for other player's items
    [HarmonyPatch(typeof(HintShuffle), "getHintText")]
    public class HintShuffleText_Patch
    {
        public static void Postfix(ref string __result, string location)
        {
            Item item = Main.Randomizer.itemShuffler.getItemAtLocation(location);
            if (item == null || item.type != 200)
                return;

            // This is a valid location that holds another player's item
            ArchipelagoItem archItem = item as ArchipelagoItem;
            string itemHint = $"'{archItem.name}' for {archItem.playerName}";
            __result = __result.Replace("[AP]", itemHint);
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
}
