using BepInEx;
using HarmonyLib;
using BlasphemousRandomizer;

namespace BlasphemousMultiworld
{
    [BepInPlugin("com.damocles.blasphemous.multiworld", "Blasphemous Multiworld", PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.damocles.blasphemous.randomizer", "1.0.0")]
    [BepInProcess("Blasphemous.exe")]
    public class Main : BaseUnityPlugin
    {
        public static Multiworld Multiworld;
        public static Randomizer Randomizer;

        private void Awake()
        {
            Multiworld = new Multiworld();
            Randomizer = BlasphemousRandomizer.Main.Randomizer;
            Patch();
        }

        private void Update()
        {
            Multiworld.update();
        }

        private void Patch()
        {
            Harmony harmony = new Harmony("com.damocles.blasphemous.multiworld");
            harmony.PatchAll();
        }
    }
}
