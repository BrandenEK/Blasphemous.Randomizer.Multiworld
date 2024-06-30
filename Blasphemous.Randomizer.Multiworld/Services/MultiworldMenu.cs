using Blasphemous.Framework.Menus;
using UnityEngine;

namespace Blasphemous.Randomizer.Multiworld.Services;

public class MultiworldMenu : ModMenu
{
    protected override int Priority { get; } = int.MaxValue;

    protected override void CreateUI(Transform ui)
    {
        Main.Multiworld.LogWarning("Created mw menu ui");
    }
}
