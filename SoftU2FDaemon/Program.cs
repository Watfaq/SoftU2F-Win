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
using System.Windows.Controls;
using System.Windows.Forms;
using U2FLib.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using U2FLib;
using Application = System.Windows.Forms.Application;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
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

        private static readonly string BinName = typeof(App).Assembly.GetName().Name;
        private static readonly string BinFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BinName);
        private static readonly string DBPath = Path.Combine(
            BinFolder, "db.sqlite");

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
                if (!Directory.Exists(BinFolder)) Directory.CreateDirectory(BinFolder);
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

        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenu();

            var item = new MenuItem("Auto Start");
            item.Checked = AutoStart();
            item.Click += OnAutoStartClick;
            _trayMenu.MenuItems.Add(item);

            _trayMenu.MenuItems.Add("Reset", OnResetClickedOnClick);
            _trayMenu.MenuItems.Add("-");
            _trayMenu.MenuItems.Add("Exit", (sender, args) => Application.Exit());

            components = new Container();

            _trayIcon = new NotifyIcon(components)
            {
                Text = "SoftU2F Daemon",
                ContextMenu = _trayMenu,
                Icon = new Icon("key.ico"),
                Visible = true
            };

            _trayIcon.BalloonTipClicked += (sender, args) => _userPresenceCallback?.Invoke(true);
            _trayIcon.BalloonTipShown += (sender, args) => _notificationOpen = true;
            _trayIcon.BalloonTipClosed += (sender, args) => _notificationOpen = false;
        }

        private void OnAutoStartClick(object sender, EventArgs e)
        {
            if (AutoStart())
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key?.DeleteValue(BinName, false);
                }
            }
            else
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key?.SetValue(BinName, "\"" + Application.ExecutablePath + "\"");
                }
            }

            var item = (MenuItem) sender;
            item.Checked = !item.Checked;
        }

        private bool AutoStart()
        {
            using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                return key != null && key.GetValueNames().Any(v => v == BinName);
            }
        }

        private void OnResetClickedOnClick(object sender, EventArgs args)
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
        }

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

        #region UserPresence

        private Action<bool> _userPresenceCallback;
        private readonly object _userPresenceCallbackLock = new object();
        private bool _notificationOpen;

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
            if (_notificationOpen) return;
            _trayIcon.ShowBalloonTip((int)TimeSpan.FromSeconds(10).TotalMilliseconds, title, message, ToolTipIcon.Info);
            UserPresenceCallback = userClicked;
        }

        #endregion
    }
}
