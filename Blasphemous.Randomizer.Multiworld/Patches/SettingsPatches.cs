using Blasphemous.Framework.UI;
using Blasphemous.Randomizer.Services;
using HarmonyLib;
using UnityEngine;

namespace Blasphemous.Randomizer.Multiworld.Patches;

/// <summary>
/// Instead of creating normal UI, just add override text
/// </summary>
[HarmonyPatch(typeof(RandomizerMenu), "CreateUI")]
class RandomizerMenu_CreateUI_Patch
{
    public static bool Prefix(Transform ui)
    {
        UIModder.Create(new RectCreationOptions()
        {
            Name = "RandoMenuOverride",
            Parent = ui,
        }).AddText(new TextCreationOptions()
        {
            FontSize = 54,
            Contents = "Settings will be determined by multiworld",
        });

        return false;
    }
}

/// <summary>
/// Change MenuSettings to instead return the config from multiworld
/// </summary>
[HarmonyPatch(typeof(RandomizerMenu), nameof(RandomizerMenu.MenuSettings), MethodType.Getter)]
class RandomizerMenu_GetSettings_Patch
{
    public static bool Prefix(ref Config __result)
    {
        __result = Main.Multiworld.ServerSettings.Config;
        return false;
    }
}
[HarmonyPatch(typeof(RandomizerMenu), nameof(RandomizerMenu.MenuSettings), MethodType.Setter)]
class RandomizerMenu_SetSettings_Patch
{
    public static bool Prefix()
    {
        return false;
    }
}