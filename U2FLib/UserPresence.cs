using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using U2FLib;
using Org.BouncyCastle.Asn1.X509;
using Timer = System.Timers.Timer;

namespace U2FLib
{
    public interface INotifySender
    {
        void Send(string title, string message, Action<bool> userClicked);
    }

    public class UserPresence
    {
        private static bool _present;
        private static readonly object _presentLock =  new object();

        public enum PresenceType: byte
        {
            Registration = 1,
            Authentication = 2
        }

        public static bool Present
        {
            get
            {
                lock (_presentLock)
                {
                    return _present;
                }
            }
            private set
            {
                lock (_presentLock)
                {
                    _present = value;
                }
            }
        }

        private static readonly Timer PresenceTimeout;

        public static INotifySender Sender;

        static UserPresence()
        {
            PresenceTimeout = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds)
            {
                AutoReset = false  // only fire once
            };
            PresenceTimeout.Elapsed += (sender, args) => Present = false;
            PresenceTimeout.Enabled = false;
        }

        public static void AskAsync(PresenceType type, string facet)
        {
            var title = "";
            facet =  string.IsNullOrEmpty(facet) ? "Unknown Facet" : facet;
            var message = "";
            switch (type)
            {
                case PresenceType.Authentication:
                    title = "Authentication Request";
                    message = $"Authentication with {facet}";
                    break;
                case PresenceType.Registration:
                    title = "Registration Request";
                    message = $"Register with {facet}";
                    break;
            }

            message += "\nClick to Allow";
            Sender?.Send(title, message, delegate(bool b)
            {
                if (b)
                {
                    Set();
                }
            });
        }

        // use pressed the button
        public static void Set()
        {
            if (Present)
            {
                PresenceTimeout.Stop();
            }
            Present = true;
            PresenceTimeout.Start();
        }

        // the presence is used
        public static void Take()
        {
            Present = false;
            PresenceTimeout.Stop();
        }
    }
}
