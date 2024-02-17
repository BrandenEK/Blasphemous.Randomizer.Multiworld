using HarmonyLib;
using Framework.Managers;

namespace Blasphemous.Randomizer.Multiworld.DeathLink;

// Send deathlink & prevent dropping guilt fragment
[HarmonyPatch(typeof(GuiltManager), "OnPenitentDead")]
class GuiltManager_Patch
{
    public static void Prefix(ref bool __state)
    {
        __state = Core.Logic.Penitent.GuiltDrop;
        DeathLinkManager deathlink = Main.Multiworld.DeathLinkManager;

        // Prevent guilt drop
        if (deathlink.CurrentStatus == DeathLinkStatus.Killing)
        {
            Core.Logic.Penitent.GuiltDrop = false;
        }
    }

    public static void Postfix(bool __state)
    {
        Core.Logic.Penitent.GuiltDrop = __state;
        DeathLinkManager deathlink = Main.Multiworld.DeathLinkManager;

        // Send deathlink
        if (deathlink.CurrentStatus == DeathLinkStatus.Nothing)
        {
            deathlink.SendDeath();
        }
        else if (deathlink.CurrentStatus == DeathLinkStatus.Killing)
        {
            deathlink.CurrentStatus = DeathLinkStatus.Nothing;
        }
    }
}
