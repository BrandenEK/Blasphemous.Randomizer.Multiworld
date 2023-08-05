using Archipelago.MultiClient.Net.Helpers;
using System.Collections.Generic;

namespace BlasphemousMultiworld.AP.Receivers
{
    public class ItemReceiver
    {
        private readonly List<QueuedItem> itemQueue = new();
        private int itemsReceived = 0;

        public void OnReceiveItem(ReceivedItemsHelper helper)
        {
            string player = Main.Multiworld.APManager.GetPlayerNameFromSlot(helper.PeekItem().Player);
            if (player == null || player == string.Empty)
                player = "Server";
            string itemName = helper.PeekItemName();
            int itemIdx = helper.Index;
            helper.DequeueItem();

            if (Main.Multiworld.APManager.ItemNameExists(itemName, out string itemId))
            {
                itemQueue.Add(new QueuedItem(itemId, itemIdx, player));
                Main.Multiworld.Log("Queueing item: " + itemId);
            }
            else
            {
                Main.Multiworld.LogDisplay("Error: " + itemName + " doesn't exist!");
            }

            ProcessItemQueue();
        }

        public void ProcessItemQueue()
        {
            if (!Main.Multiworld.InGame || itemQueue.Count == 0)
                return;

            Main.Multiworld.LogWarning("Processing item queue");

            foreach (QueuedItem item in itemQueue)
            {
                Main.Multiworld.Log($"Item '{item.itemId}' is at index {item.index} with {itemsReceived} items currently received");
                if (item.index > itemsReceived)
                {
                    Main.Randomizer.data.items[item.itemId].addToInventory();
                    Main.Multiworld.NotificationManager.DisplayNotification(item);
                    itemsReceived++;
                }
            }

            ClearItemQueue();
        }

        public void ClearItemQueue()
        {
            itemQueue.Clear();
        }

        public int SaveItemsReceived() => itemsReceived;
        public void LoadItemsReceived(int items) => itemsReceived = items;
        public void ResetItemsReceived() => itemsReceived = 0;
    }
}
