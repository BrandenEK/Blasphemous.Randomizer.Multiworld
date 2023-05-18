
namespace BlasphemousMultiworld.Structures
{
    public class QueuedItem
    {
        public QueuedItem(string itemId, int index, string player)
        {
            this.itemId = itemId;
            this.index = index;
            this.player = player;
        }

        public string itemId;
        public int index;
        public string player;
    }
}
