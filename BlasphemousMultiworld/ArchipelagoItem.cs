using BlasphemousRandomizer.Structures;

namespace BlasphemousMultiworld
{
    public class ArchipelagoItem : Item
    {
        public string playerName;

        public ArchipelagoItem(string name, string player) : base(name, 200, 0, false)
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
