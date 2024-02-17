using BepInEx;
using Blasphemous.ModdingAPI;

namespace Blasphemous.Randomizer.Multiworld;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.1.0")]
[BepInDependency("Blasphemous.Randomizer", "2.0.12")]
[BepInDependency("Blasphemous.CheatConsole", "1.0.0")]
internal class Main : BaseUnityPlugin
{
    public static Multiworld Multiworld { get; private set; }
    public static Randomizer Randomizer { get; private set; }

    private void Start()
    {
        Multiworld = new Multiworld();
        Randomizer = Multiworld.IsModLoadedName("Randomizer", out BlasMod mod) ? mod as Randomizer
            : throw new System.Exception("Randomizer not loaded");
    }
}
