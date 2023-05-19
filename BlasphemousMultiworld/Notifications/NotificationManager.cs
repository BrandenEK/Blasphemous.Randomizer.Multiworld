using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlasphemousMultiworld.Structures;
using Gameplay.UI;

namespace BlasphemousMultiworld.Notifications
{
    public class NotificationManager
    {
        private const float MOVEMENT_TIME = 0.5f;
        private const float DISPLAY_TIME = 1.8f;
        private const float END_DELAY = 0.2f;

        private readonly Vector2 POSITION_HIDDEN = new Vector2(-80, -98);
        private readonly Vector2 POSITION_VISIBLE = new Vector2(55, -98);

        // UI elements
        RectTransform notificationBox;
        Image itemImage;
        Text receivedText;

        // Game process
        Queue<QueuedItem> queue = new Queue<QueuedItem>();
        bool isShowing = false;

        public void DisplayNotification(QueuedItem item)
        {
            if (notificationBox == null)
                CreateNotificationBox();

            if (isShowing)
            {
                queue.Enqueue(item);
            }
            else
            {
                UIController.instance.StartCoroutine(DisplayCorroutine(item));
            }
        }

        private IEnumerator DisplayCorroutine(QueuedItem item)
        {
            isShowing = true;
            if (item.itemId == "Death")
            {
                // Deathlink
                itemImage.sprite = Main.Multiworld.ImageDeathlink;
            }
            else
            {
                // Regular item
                itemImage.sprite = Main.Randomizer.data.items[item.itemId].getRewardInfo(false).sprite;
            }
            receivedText.text = Main.Multiworld.Localize("found") + ":\n" + item.player;
            notificationBox.anchoredPosition = POSITION_HIDDEN;
            float positionDifference = POSITION_VISIBLE.x - POSITION_HIDDEN.x;

            // Start at hidden and move towards visible
            for (float i = 0; i < MOVEMENT_TIME; i += Time.unscaledDeltaTime)
            {
                notificationBox.anchoredPosition = new Vector2(POSITION_HIDDEN.x + positionDifference * (i / MOVEMENT_TIME), POSITION_HIDDEN.y);
                yield return new WaitForEndOfFrame();
            }

            // Fully visible
            notificationBox.anchoredPosition = POSITION_VISIBLE;
            yield return new WaitForSecondsRealtime(DISPLAY_TIME);

            // Start at visible and move towards hidden
            for (float i = 0; i < MOVEMENT_TIME; i += Time.unscaledDeltaTime)
            {
                notificationBox.anchoredPosition = new Vector2(POSITION_VISIBLE.x - positionDifference * (i / MOVEMENT_TIME), POSITION_VISIBLE.y);
                yield return new WaitForEndOfFrame();
            }

            // Fully hidden
            notificationBox.anchoredPosition = POSITION_HIDDEN;
            yield return new WaitForSecondsRealtime(END_DELAY);

            if (queue.Count > 0)
            {
                UIController.instance.StartCoroutine(DisplayCorroutine(queue.Dequeue()));
            }
            else
            {
                isShowing = false;
            }
        }

        private void CreateNotificationBox()
        {
            if (m_ImageBackground == null || m_ImageBox == null || m_TextFont == null)
                return;
            Main.Multiworld.Log("Creating item receiver notification box!");

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
            notificationBox.anchoredPosition = POSITION_HIDDEN;
            notificationBox.sizeDelta = new Vector2(150, 40);
            notificationBox.GetComponent<Image>().sprite = m_ImageBackground;

            // Create item box holder
            obj = new GameObject("Item Box", typeof(RectTransform), typeof(Image));
            RectTransform itemBox = obj.GetComponent<RectTransform>();
            itemBox.SetParent(notificationBox, false);
            itemBox.sizeDelta = new Vector2(30, 30);
            itemBox.anchoredPosition = new Vector2(-30, 0);
            itemBox.GetComponent<Image>().sprite = m_ImageBox;

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
            receivedText.font = m_TextFont;
            receivedText.fontSize = 16;
            receivedText.alignment = TextAnchor.MiddleCenter;
        }

        // Set by other objects on awake
        private Sprite m_ImageBackground;
        public Sprite ImageBackground
        {
            set
            {
                if (m_ImageBackground == null) m_ImageBackground = value;
            }
        }
        private Sprite m_ImageBox;
        public Sprite ImageBox
        {
            set
            {
                if (m_ImageBox == null) m_ImageBox = value;
            }
        }
        private Font m_TextFont;
        public Font TextFont
        {
            set
            {
                if (m_TextFont == null) m_TextFont = value;
            }
        }
    }
}
