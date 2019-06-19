using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using U2FLib.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using U2FLib;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace SoftU2FDaemon
{
    class App : Form, INotifySender
    {
        private NotifyIcon _trayIcon;
        private ContextMenu _trayMenu;
        private IContainer components;
        private CancellationTokenSource _cancellation;

        private IServiceProvider _serviceProvider;

        #region App settings

        private static readonly string DBPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "db.sqlite");

        #endregion

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new App());
        }

        public App()
        {
            SetupApplication();
            InitializeTrayIcon();
            InitializeBackgroundDaemon();
        }

        #region Application Initialization

        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenu();
            _trayMenu.MenuItems.Add("Exit", ((sender, args) =>
            {
                Application.Exit();
            }));

            _trayMenu.MenuItems.Add("Reset", (sender, args) =>
            {
                var confirm = MessageBox.Show("Do you want to reset SoftU2F? this will delete all your local data.",
                    "Reset Database", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes)
                {
                    MessageBox.Show("Reset cancelled");
                    return;
                }

                if (File.Exists(DBPath))
                {
                    var bak = $"{DBPath}.bak";
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(DBPath, bak);
                    Restart();
                }
            });
          

            components = new Container();

            _trayIcon = new NotifyIcon(components)
            {
                Text = "SoftU2F Daemon", ContextMenu = _trayMenu, Icon = new Icon("tray.ico"), Visible = true
            };

            _trayIcon.BalloonTipClicked += (sender, args) => _userPresenceCallback?.Invoke(true);
        }

        private void Restart()
        {
            Application.Restart();
        }

        private void SetupApplication()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            var dbContext = _serviceProvider.GetService<AppDbContext>();
            using (dbContext)
            {
                dbContext.Database.Migrate();
                var appData = dbContext.ApplicationDatum.FirstOrDefault();
                if (appData == null)
                {
                    appData = new ApplicationData();
                    appData.Counter = 0;
                    dbContext.ApplicationDatum.Add(appData);
                    dbContext.SaveChanges();
                }
            }

            _cancellation = new CancellationTokenSource();
        }


        private void InitializeBackgroundDaemon()
        {
            var daemon = _serviceProvider.GetService<IU2FBackgroundTask>();
            (new Thread(() => { daemon.StartIoLoop(_cancellation.Token); })).Start();
            UserPresence.Sender = this;
        }

        private void ConfigureServices(IServiceCollection service)
        {
            service.AddLogging();
            service.AddSingleton<IU2FBackgroundTask, BackgroundTask>();
            service.AddDbContext<AppDbContext>(options => { options.UseSqlite($"Filename={DBPath}"); });
            Environment.SetEnvironmentVariable("DBPath", DBPath); // for DbContext outside container
        }

        #endregion

        #region System Tray Icon

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            base.OnLoad(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            _cancellation.Cancel();

            base.Dispose(disposing);
        }

        #endregion


        private Action<bool> _userPresenceCallback;
        private readonly object _userPresenceCallbackLock = new object();
        private Action<bool> UserPresenceCallback
        {
            set
            {
                lock (_userPresenceCallbackLock)
                {
                    _userPresenceCallback?.Invoke(false);
                    _userPresenceCallback = value;
                }
            }
        }
        public void Send(string title, string message, Action<bool> userClicked)
        {
            _trayIcon.ShowBalloonTip((int)TimeSpan.FromSeconds(10).TotalMilliseconds, title, message, ToolTipIcon.Info);
            UserPresenceCallback = userClicked;
        }
    }
}
