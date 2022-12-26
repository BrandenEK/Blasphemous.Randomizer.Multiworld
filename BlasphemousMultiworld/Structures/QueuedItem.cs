using BlasphemousRandomizer.Structures;

namespace BlasphemousMultiworld.Structures
{
    public class QueuedItem
    {
        public QueuedItem(Item item, int index, string player)
        {
            this.item = item;
            this.index = index;
            this.player = player;
        }

        public Item item;
        public int index;
        public string player;
    }
}
