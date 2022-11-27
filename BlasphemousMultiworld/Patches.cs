using System;
using System.Collections.Generic;
using Gameplay.UI.Widgets;
using Gameplay.UI.Console;
using HarmonyLib;
using UnityEngine.UI;

namespace BlasphemousMultiworld
{
    // Add multiworld commands to console
    [HarmonyPatch(typeof(ConsoleWidget), "InitializeCommands")]
    public class Console_Patch
    {
        public static void Postfix(List<ConsoleCommand> ___commands)
        {
            ___commands.Add(new MultiworldCommand());
        }
    }

    // Add multiworld version to menu
    [HarmonyPatch(typeof(VersionNumber), "Start")]
    public class VersionNumber_Patch
    {
        public static void Postfix(VersionNumber __instance)
        {
            Text versionText = __instance.GetComponent<Text>();
            if (versionText.text.Contains("v."))
                versionText.text = "";
            versionText.text += "\nMultiworld v" + PluginInfo.PLUGIN_VERSION;
        }
    }
}
