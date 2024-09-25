using BepInEx;
using Blasphemous.ModdingAPI.Helpers;

namespace Blasphemous.Randomizer.Multiworld;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.4.1")]
[BepInDependency("Blasphemous.Randomizer", "3.0.3")]
[BepInDependency("Blasphemous.CheatConsole", "1.0.1")]
[BepInDependency("Blasphemous.Framework.Menus", "0.3.4")]
internal class Main : BaseUnityPlugin
{
    public static Multiworld Multiworld { get; private set; }
    public static Randomizer Randomizer { get; private set; }

    private void Start()
    {
        Multiworld = new Multiworld();
        Randomizer = (Randomizer)ModHelper.GetModByName("Randomizer");
    }
}
