using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Timer = System.Timers.Timer;

namespace U2FHID
{
    class UserPresence
    {
        private static bool _present;
        private static readonly object _presentLock =  new object();

        internal enum PresenceType: byte
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

        static UserPresence()
        {
            PresenceTimeout = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds)
            {
                AutoReset = true
            };
            PresenceTimeout.Elapsed += (sender, args) => Present = false;
            PresenceTimeout.Enabled = false;
        }

        public static Task AskAsync(PresenceType type, string facet)
        {
            var title = "";
            var message = "";
            switch (type)
            {
                case PresenceType.Authentication:
                    title = "Authentication Request";
                    message = facet;
                    break;
                case PresenceType.Registration:
                    title = "Registration Request";
                    message = facet;
                    break;
            }

            return Task.Run(() =>
            {
                var messageBoxResult = MessageBox.Show(message, title, MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    Set();
                }
            });

        }

        // use pressed the button
        public static void Set()
        {

            Present = true;
            PresenceTimeout.Enabled = true;
        }

        // the presence is used
        public static void Take()
        {
            PresenceTimeout.Stop();
            Present = false;
        }
    }
}
