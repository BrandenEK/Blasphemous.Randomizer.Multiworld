using HarmonyLib;
using Tools.Playmaker2.Action;

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
}
