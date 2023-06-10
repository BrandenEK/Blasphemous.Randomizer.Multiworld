using BlasphemousRandomizer.ItemRando;
using BlasphemousRandomizer.Notifications;

namespace BlasphemousMultiworld.AP
{
    public class ArchipelagoItem : Item
    {
        private readonly string _playerName;
        public string PlayerName => _playerName;

        private readonly bool _progression;
        public bool IsProgression => _progression;

        public ArchipelagoItem(string name, string player, bool progression) : base("AP", name, "[AP]", 200, false, 0)
        {
            _playerName = player;
            _progression = progression;
        }

        public override void addToInventory() { }

        public override RewardInfo getRewardInfo(bool upgraded)
        {
            return new RewardInfo(name, $"{Main.Multiworld.Localize("ardesc")} {_playerName}.", $"{Main.Multiworld.Localize("arnot")} {_playerName}!", Main.Multiworld.ImageAP);
        }
    }
}
