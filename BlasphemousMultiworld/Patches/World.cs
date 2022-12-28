using HarmonyLib;
using Tools.Playmaker2.Action;
using Gameplay.GameControllers.Entities;
using Gameplay.GameControllers.Penitent;
using Framework.Managers;

namespace BlasphemousMultiworld.Patches
{
    // Send goal completion on specific cutscenes
    [HarmonyPatch(typeof(CutscenePlay), "OnEnter")]
    public class CutscenePlay_Patch
    {
        public static void Postfix(CutscenePlay __instance)
        {
            if (__instance.cutscene != null)
            {
                string name = __instance.cutscene.name;
                if (name == "CTS10-EndingA")
                    Main.Multiworld.sendGoal(1);
                else if (name == "CTS09-EndingB")
                    Main.Multiworld.sendGoal(0);
                else if (name == "CTS301-EndingC")
                    Main.Multiworld.sendGoal(2);
            }
        }
    }

    // Send deathlink & prevent dropping guilt fragment
    [HarmonyPatch(typeof(GuiltManager), "OnPenitentDead")]
    public class GuiltManager_Patch
    {
        public static void Prefix(ref bool __state)
        {
            __state = Core.Logic.Penitent.GuiltDrop;
            if (Main.Multiworld.deathlink == Multiworld.DeathLinkStatus.Killing)
            {
                Core.Logic.Penitent.GuiltDrop = false;
            }
        }

        public static void Postfix(bool __state)
        {
            Core.Logic.Penitent.GuiltDrop = __state;

            // Send deathlink
            if (Main.Multiworld.deathlink == Multiworld.DeathLinkStatus.Nothing)
            {
                Main.Multiworld.sendDeathLink();
            }
            else if (Main.Multiworld.deathlink == Multiworld.DeathLinkStatus.Killing)
            {
                Main.Multiworld.deathlink = Multiworld.DeathLinkStatus.Nothing;
            }
        }
    }
}
