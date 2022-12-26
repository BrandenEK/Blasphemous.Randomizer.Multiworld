using BlasphemousRandomizer.Structures;

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
            return new RewardInfo(name, "An item that belongs to " + playerName + ".", "Sending to " + playerName + "!", Main.Multiworld.getImage(0));
        }
    }
}
