using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Gameplay.UI.Others.MenuLogic;

namespace BlasphemousMultiworld.Notifications
{
    // Send receive notification data to the item receiver
    [HarmonyPatch(typeof(PopupAchievementWidget), "Awake")]
    public class PopupAchievementWidget_Patch
    {
        public static void Postfix(RectTransform ___PopUp)
        {
            Main.Multiworld.NotificationManager.ImageBackground = ___PopUp.GetComponent<Image>().sprite;
            Main.Multiworld.NotificationManager.TextFont = ___PopUp.GetChild(1).GetComponent<Text>().font;
        }
    }
    [HarmonyPatch(typeof(NewInventory_GridItem), "Awake")]
    public class InvGridItem_Patch
    {
        public static void Postfix(Sprite ___backEquipped)
        {
            Main.Multiworld.NotificationManager.ImageBox = ___backEquipped;
        }
    }
}
