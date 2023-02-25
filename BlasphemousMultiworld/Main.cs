using BepInEx;
using BlasphemousRandomizer;

namespace BlasphemousMultiworld
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("com.damocles.blasphemous.modding-api", "1.1.0")]
    [BepInDependency("com.damocles.blasphemous.randomizer", "1.4.0")]
    [BepInProcess("Blasphemous.exe")]
    public class Main : BaseUnityPlugin
    {
        public const string MOD_ID = "com.damocles.blasphemous.multiworld";
        public const string MOD_NAME = "Multiworld";
        public const string MOD_VERSION = "1.0.1";

        public static Multiworld Multiworld;
        public static Randomizer Randomizer;

        private void Start()
        {
            Randomizer = BlasphemousRandomizer.Main.Randomizer;
            Multiworld = new Multiworld(MOD_ID, MOD_NAME, MOD_VERSION);
        }
    }
}
