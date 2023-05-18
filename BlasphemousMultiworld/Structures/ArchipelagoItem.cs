using BlasphemousRandomizer.ItemRando;
using BlasphemousRandomizer.Notifications;

namespace BlasphemousMultiworld.Structures
{
    public class ArchipelagoItem : Item
    {
        public string playerName;

        public ArchipelagoItem(string name, string player) : base("AP", name, "[AP]", 200, false, 0)
        {
            playerName = player;
        }

        public override void addToInventory()
        {
            return;
        }

        public override RewardInfo getRewardInfo(bool upgraded)
        {
            return new RewardInfo(name, $"{Main.Multiworld.Localize("ardesc")} {playerName}.", $"{Main.Multiworld.Localize("arnot")} {playerName}!", Main.Multiworld.ImageAP);
        }
    }
}
