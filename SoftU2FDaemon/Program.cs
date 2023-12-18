using ContextMenu = System.Windows.Forms.ContextMenuStrip;
using MenuItem = System.Windows.Forms.ToolStripMenuItem;

namespace SoftU2FDaemon
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Win32;
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;
    using U2FLib;
    using U2FLib.Storage;

    internal class App : Form, INotifySender
    {
        private CancellationTokenSource _cancellation;

        private IServiceProvider _serviceProvider;
        private NotifyIcon _trayIcon;
        private ContextMenu _trayMenu;

        public App()
        {
            SetupApplication();
            InitializeTrayIcon();
            InitializeBackgroundDaemon();

            if (DiagnoseMode)
            {
                tryOutNotification();
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new App());
        }

        #region App settings

        private static readonly string BinName = typeof(App).Assembly.GetName().Name;

        private static readonly string BinFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BinName);

        private static readonly string DBPath = Path.Combine(
            BinFolder, "db.sqlite");
        private static readonly string UnProtectedDBPath = Path.Combine(BinFolder, "db.unprotected.sqlite");

        public static bool UnprotectedMode => Environment.GetCommandLineArgs().Contains("--db-unprotected");

        public static bool DiagnoseMode => Environment.GetCommandLineArgs().Contains("--diagnose-mode");

        #endregion

        #region Application LifeCycle

        private IntPtr lastActiveWin = IntPtr.Zero;

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion

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

            _cancellation.Token.Register(() => {
                _cancellation.Dispose();
                if (_exitRequested) { Environment.Exit(0); }
            });
        }


        private void InitializeBackgroundDaemon()
        {
            var daemon = _serviceProvider.GetService<IU2FBackgroundTask>();
            if (!daemon.OpenDevice())
            {
                MessageBox.Show("Failed to load driver. Maybe installation was unsuccessful\nExiting", "Driver Error");
                if (Application.MessageLoop) Application.Exit();
                Environment.Exit(1);
                return;
            }

            new Thread(() => { daemon.StartIoLoop(_cancellation.Token); }).Start();
            UserPresence.Sender = this;
        }

        private void ConfigureServices(IServiceCollection service)
        {
            service.AddLogging();
            service.AddSingleton<IU2FBackgroundTask, BackgroundTask>();

            if (UnprotectedMode)
            {
                service.AddDbContext<AppDbContext>(options => { options.UseSqlite($"Filename={UnProtectedDBPath}"); });
                Environment.SetEnvironmentVariable("DBPath", UnProtectedDBPath); // for DbContext outside container
            }
            else
            {
                service.AddDbContext<AppDbContext>(options => { options.UseSqlite($"Filename={DBPath}"); });
                Environment.SetEnvironmentVariable("DBPath", DBPath); // for DbContext outside container
            }
        }

        #endregion

        #region System Tray Icon

        private bool _exitRequested = false;

        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenu();

            var item = new MenuItem { Text = @"Auto Start", Checked = AutoStart() };
            item.Click += OnAutoStartClick;
            _trayMenu.Items.Add(item);

            _trayMenu.Items.Add("Reset", null, OnResetClickedOnClick);
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Exit", null, (sender, args) => {
                _exitRequested = true;
                Application.Exit();
            });

            _trayIcon = new NotifyIcon
            {
                Text = @"SoftU2F Daemon",
                ContextMenuStrip = _trayMenu,
                Icon = new Icon("tray.ico"),
                Visible = true
            };

            _trayIcon.BalloonTipClicked += (sender, args) =>
            {
                if (lastActiveWin != IntPtr.Zero)
                    SetForegroundWindow(lastActiveWin);

                _notificationOpen = false;
                _userPresenceCallback?.Invoke(true);
            };
            _trayIcon.BalloonTipShown += (sender, args) =>
            {
                _notificationOpen = true;
                lastActiveWin = GetForegroundWindow();
            };
            _trayIcon.BalloonTipClosed += (sender, args) => _notificationOpen = false;

            this.FormClosing += (sender, e) =>
            {
                // Hide and dispose the icon
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            };
        }

        private void OnAutoStartClick(object sender, EventArgs e)
        {
            if (AutoStart())
            {
                using var key =
                    Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key?.DeleteValue(BinName, false);
            }
            else
            {
                using var key =
                    Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key?.SetValue(BinName, "\"" + Application.ExecutablePath + "\"");
            }

            var item = (MenuItem)sender;
            item.Checked = !item.Checked;
        }

        private static bool AutoStart()
        {
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return key != null && key.GetValueNames().Any(v => v == BinName);
        }

        private void OnResetClickedOnClick(object sender, EventArgs args)
        {
            var confirm = MessageBox.Show(@"Do you want to reset SoftU2F? this will delete all your local data.",
                @"Reset Database", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes)
            {
                MessageBox.Show("Reset cancelled");
                return;
            }

            if (!File.Exists(DBPath)) return;
            var bak = $"{DBPath}.bak";
            if (File.Exists(bak)) File.Delete(bak);
            File.Move(DBPath, bak);
            Restart();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            base.OnLoad(e);
        }

        #endregion

        #region IDisposable

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing) _cancellation.Cancel();
            base.Dispose(disposing);
        }

        ~App()
        {
            Dispose(true);
        }

        #endregion

        #region UserPresence

        private Action<bool> _userPresenceCallback;
        private readonly object _userPresenceCallbackLock = new();
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
            _trayIcon.ShowBalloonTip((int)TimeSpan.FromSeconds(10).TotalMilliseconds, title, message,
                ToolTipIcon.Info);
            UserPresenceCallback = userClicked;
        }

        private void tryOutNotification()
        {
            _trayIcon.ShowBalloonTip((int)TimeSpan.FromSeconds(5).TotalMilliseconds, "Test Notification", "If you didn't see this, you'd probabaly have issue with handling authentication actions", ToolTipIcon.Info);
        }

        #endregion
    }
}