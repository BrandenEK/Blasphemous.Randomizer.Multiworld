using Blasphemous.Randomizer.ItemRando;
using Blasphemous.Randomizer.Notifications;

namespace Blasphemous.Randomizer.Multiworld.AP
{
    public class ArchipelagoItem : Item
    {
        private readonly string _playerName;
        public string PlayerName => _playerName;

        private readonly ItemType _type;
        public bool IsProgression => _type == ItemType.Progression;

        public ArchipelagoItem(string name, string player, ItemType type) : base("AP", name, "[AP]", 200, false, 0)
        {
            _playerName = player;
            _type = type;
        }

        public override void addToInventory() { }

        public override RewardInfo getRewardInfo(bool upgraded)
        {
            string descTerm = _type switch
            {
                ItemType.Progression => "arprog",
                ItemType.Useful => "arusef",
                ItemType.Trap => "artrap",
                _ => "arbasc"
            };

            return new RewardInfo
            (
                name: name,
                description: GetTextWithPlayerName(descTerm),
                notification: GetTextWithPlayerName("arnot"),
                sprite: Main.Multiworld.ImageAP
            );
        }

        private string GetTextWithPlayerName(string term)
        {
            string text = Main.Multiworld.LocalizationHandler.Localize(term);
            return text.Replace("*", _playerName);
        }

        public enum ItemType
        {
            Basic = 0,
            Progression = 1,
            Useful = 2,
            Trap = 4,
        }
    }
}
