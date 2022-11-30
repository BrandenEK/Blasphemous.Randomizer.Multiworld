using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return null;
        }
    }
}
