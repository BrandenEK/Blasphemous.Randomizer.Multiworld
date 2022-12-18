using BlasphemousRandomizer.Structures;

namespace BlasphemousMultiworld.Structures
{
    public class QueuedItem
    {
        public QueuedItem(Item item, int index)
        {
            this.item = item;
            this.index = index;
        }

        public Item item;
        public int index;
    }
}
