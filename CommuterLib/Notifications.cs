using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MobileSrc.Commuter.Shared
{
    [Serializable]
    public class NotificationResponse
    {
        public string NotificationStatus
        {
            get;
            set;
        }
        public string SubscriptionStatus
        {
            get;
            set;
        }
        public string DeviceConnectionStatus
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format(@"Not:{0},Sub:{1},Dev:{2}", this.NotificationStatus, this.SubscriptionStatus, this.DeviceConnectionStatus);
        }
    }

    public static class Notifications
    {
        /*
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUri.Text))
            {
                MessageBox.Show("Channel Uri is empty!");
                return;
            }

            string subscriptionUri = txtUri.Text;

            //Create an http message
            HttpWebRequest sendNotificationRequest = (HttpWebRequest)WebRequest.Create(subscriptionUri);
            sendNotificationRequest.Method = "POST";

            //Indicate that you'll send toast notifications!
            sendNotificationRequest.ContentType = "text/xml";
            sendNotificationRequest.Headers = new WebHeaderCollection();
            sendNotificationRequest.Headers.Add("X-NotificationClass", "2");

            if (string.IsNullOrEmpty(txtMessage.Text)) return;

            if (toastRadioButton.IsChecked.Value)
            {
                SendToast(txtUri.Text, txtTitle.Text, txtMessage.Text);
            }
            if (tileRadioButton.IsChecked.Value)
            {
                SendTileUpdate(txtUri.Text, txtTitle.Text, 35, txtMessage.Text);
            }
        }
        */
        enum NotificationType
        {
            Toast = 1,
            Tile = 2,
            Raw = 3
        }

        enum BatchingInterval
        {
            TileImmediately = 1,
            ToastImmediately = 2,
            RawImmediately = 3,
            TileWait450 = 11,
            ToastWait450 = 12,
            RawWait450 = 13,
            TileWait900 = 21,
            ToastWait900 = 22,
            RawWait900 = 23
        }

        public static NotificationResponse SendToast(string clientUri, string title, string message)
        {
            var toastMessage = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<wp:Notification xmlns:wp=\"WPNotification\">" +
                   "<wp:Toast>" +
                      "<wp:Text1>{0}</wp:Text1>" +
                      "<wp:Text2>{1}</wp:Text2>" +
                   "</wp:Toast>" +
                "</wp:Notification>";

            toastMessage = string.Format(toastMessage, title, message);

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(toastMessage);

            return SendMessage(clientUri, messageBytes, NotificationType.Toast);
        }

        public static NotificationResponse SendTileUpdate(string clientUri, string title, int count, string imageUrl)
        {
            var tileMessage = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<wp:Notification xmlns:wp=\"WPNotification\">" +
                   "<wp:Tile>" +
                      "<wp:BackgroundImage>{0}</wp:BackgroundImage>" +
                      "<wp:Count>{1}</wp:Count>" +
                      "<wp:Title>{2}</wp:Title>" +
                   "</wp:Tile> " +
                "</wp:Notification>";

            tileMessage = string.Format(tileMessage, System.Web.HttpUtility.HtmlEncode(imageUrl), count, title);

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(tileMessage);

            return SendMessage(clientUri, messageBytes, NotificationType.Tile);
        }

        public static void SendRawNotification(string clientUri, string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            SendMessage(clientUri, messageBytes, NotificationType.Raw);
        }

        private static NotificationResponse SendMessage(string strUri, byte[] message, NotificationType notificationType)
        {
            NotificationResponse notResponse = new NotificationResponse();
            Uri uri = new Uri(strUri);

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = "text/xml";
            request.ContentLength = message.Length;

            request.Headers.Add("X-MessageID", Guid.NewGuid().ToString());

            switch (notificationType)
            {
                case NotificationType.Toast:
                    request.Headers["X-WindowsPhone-Target"] = "toast";
                    request.Headers.Add("X-NotificationClass", ((int)BatchingInterval.ToastImmediately).ToString());
                    break;
                case NotificationType.Tile:
                    request.Headers["X-WindowsPhone-Target"] = "token";
                    request.Headers.Add("X-NotificationClass", ((int)BatchingInterval.TileImmediately).ToString());
                    break;
                default:
                    request.Headers.Add("X-NotificationClass", ((int)BatchingInterval.RawImmediately).ToString());
                    break;
            }

            try
            {
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(message, 0, message.Length);
                }
                var response = (HttpWebResponse)request.GetResponse();

                var notificationStatus = response.Headers["X-NotificationStatus"];
                var subscriptionStatus = response.Headers["X-SubscriptionStatus"];
                var deviceConnectionStatus = response.Headers["X-DeviceConnectionStatus"];

                notResponse.NotificationStatus = notificationStatus;
                notResponse.SubscriptionStatus = subscriptionStatus;
                notResponse.DeviceConnectionStatus = deviceConnectionStatus;
            }
            catch (WebException ex)
            {
                notResponse.DeviceConnectionStatus = ex.ToString();
            }
            return notResponse;
        }
    }
}