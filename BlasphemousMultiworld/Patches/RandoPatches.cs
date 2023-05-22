using HarmonyLib;
using System.Collections.Generic;
using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using BlasphemousRandomizer.HintRando;
using BlasphemousMultiworld.AP;

namespace BlasphemousMultiworld.Patches
{
    // Change randomization methods
    [HarmonyPatch(typeof(Randomizer), "Randomize")]
    public class RandomizerRandomize_Patch
    {
        public static bool Prefix(Randomizer __instance, int ___seed)
        {
            if (Main.Multiworld.APManager.Connected)
            {
                Dictionary<string, string> mappedItems = Main.Multiworld.LoadMultiworldItems();

                __instance.itemShuffler.LoadMappedItems(mappedItems); // Set item list from multiworld data
                __instance.itemShuffler.LoadMappedDoors(null); // No door shuffle yet
                __instance.hintShuffler.Shuffle(___seed); // Uses built-in hint filler based on multiworld items
                __instance.enemyShuffler.Shuffle(___seed); // Uses built-in enemy shuffle

                Main.Multiworld.Log(mappedItems.Count + " items have been inserted from multiworld!");
            }
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
            Main.Multiworld.APManager.SendLocation(locationId);
        }
    }
}
