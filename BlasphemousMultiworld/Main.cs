using BepInEx;
using BlasphemousRandomizer;

namespace BlasphemousMultiworld
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("com.damocles.blasphemous.modding-api", "1.3.4")]
    [BepInDependency("com.damocles.blasphemous.randomizer", "2.0.0")]
    [BepInProcess("Blasphemous.exe")]
    public class Main : BaseUnityPlugin
    {
        public const string MOD_ID = "com.damocles.blasphemous.multiworld";
        public const string MOD_NAME = "Multiworld";
        public const string MOD_VERSION = "1.1.0";

        public static Multiworld Multiworld { get; private set; }
        public static Randomizer Randomizer { get; private set; }

        private void Start()
        {
            Randomizer = BlasphemousRandomizer.Main.Randomizer;
            Multiworld = new Multiworld(MOD_ID, MOD_NAME, MOD_VERSION);
        }
    }
}
