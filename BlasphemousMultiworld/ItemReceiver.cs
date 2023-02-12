using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlasphemousMultiworld.Structures;
using Gameplay.UI;

namespace BlasphemousMultiworld
{
    public class ItemReceiver
    {
        // Set by other objects on awake
        public Sprite backgroundSprite;
        public Sprite boxSprite;
        public Font textFont;

        // UI elements
        RectTransform notificationBox;
        Image itemImage;
        Text receivedText;

        // Game process
        Queue<QueuedItem> queue;
        bool isShowing;

        // Data
        Vector2 hiddenPosition;
        Vector2 visiblePosition;
        float moveTime = 0.4f;
        float showTime = 2f;
        float endTime = 0.2f;

        public ItemReceiver()
        {
            queue = new Queue<QueuedItem>();
            hiddenPosition = new Vector2(-80, -98);
            visiblePosition = new Vector2(55, -98);
            isShowing = false;
        }

        public void receiveItem(QueuedItem item)
        {
            if (notificationBox == null)
                createReceiverUI();

            if (isShowing)
            {
                queue.Enqueue(item);
            }
            else
            {
                UIController.instance.StartCoroutine(showReceivedItem(item));
            }
        }

        private IEnumerator showReceivedItem(QueuedItem item)
        {
            isShowing = true;
            if (item.item == null)
            {
                // Deathlink
                itemImage.sprite = Main.Multiworld.getImage(1);
            }
            else
            {
                // Regular item
                itemImage.sprite = item.item.getRewardInfo(false).sprite;
            }
            receivedText.text = "Found by:\n" + item.player;
            notificationBox.anchoredPosition = hiddenPosition;
            float positionDifference = visiblePosition.x - hiddenPosition.x;

            // Start at hidden and move towards visible
            for (float i = 0; i < moveTime; i += Time.unscaledDeltaTime)
            {
                notificationBox.anchoredPosition = new Vector2(hiddenPosition.x + positionDifference * i, hiddenPosition.y);
                yield return new WaitForEndOfFrame();
            }

            // Fully visible
            notificationBox.anchoredPosition = visiblePosition;
            yield return new WaitForSecondsRealtime(showTime);

            // Start at visible and move towards hidden
            for (float i = 0; i < moveTime; i += Time.unscaledDeltaTime)
            {
                notificationBox.anchoredPosition = new Vector2(visiblePosition.x - positionDifference * i, visiblePosition.y);
                yield return new WaitForEndOfFrame();
            }

            // Fully hidden
            notificationBox.anchoredPosition = hiddenPosition;
            yield return new WaitForSecondsRealtime(endTime);

            if (queue.Count > 0)
            {
                UIController.instance.StartCoroutine(showReceivedItem(queue.Dequeue()));
            }
            else
            {
                isShowing = false;
            }
        }

        private void createReceiverUI()
        {
            if (backgroundSprite == null || boxSprite == null || textFont == null)
                return;
            Main.Randomizer.Log("Creating item receiver notification box!");

            // Find correct canvas
            Transform parent = null;
            foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>())
                if (canvas.name == "Game UI")
                    parent = canvas.transform;

            // Create notification display
            GameObject obj = new GameObject("Receive Notification", typeof(RectTransform), typeof(Image));
            notificationBox = obj.GetComponent<RectTransform>();
            notificationBox.SetParent(parent, false);
            notificationBox.anchorMin = new Vector2(0, 1);
            notificationBox.anchorMax = new Vector2(0, 1);
            notificationBox.anchoredPosition = hiddenPosition;
            notificationBox.sizeDelta = new Vector2(150, 40);
            notificationBox.GetComponent<Image>().sprite = backgroundSprite;

            // Create item box holder
            obj = new GameObject("Item Box", typeof(RectTransform), typeof(Image));
            RectTransform itemBox = obj.GetComponent<RectTransform>();
            itemBox.SetParent(notificationBox, false);
            itemBox.sizeDelta = new Vector2(30, 30);
            itemBox.anchoredPosition = new Vector2(-30, 0);
            itemBox.GetComponent<Image>().sprite = boxSprite;

            // Create item image
            obj = new GameObject("Item Image", typeof(RectTransform), typeof(Image));
            RectTransform image = obj.GetComponent<RectTransform>();
            image.SetParent(itemBox, false);
            image.sizeDelta = new Vector2(32, 32);
            itemImage = image.GetComponent<Image>();

            // Create recevied text
            obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform text = obj.GetComponent<RectTransform>();
            text.SetParent(notificationBox, false);
            text.sizeDelta = new Vector2(90, 40);
            text.anchoredPosition = new Vector2(30, 0);
            receivedText = text.GetComponent<Text>();
            receivedText.font = textFont;
            receivedText.fontSize = 16;
            receivedText.alignment = TextAnchor.MiddleCenter;
        }
    }
}
