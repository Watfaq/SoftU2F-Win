using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoftU2F.Console.Storage;
using U2FHID;

namespace SoftU2F.Console
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public static string DBPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "db.sqlite");

        private CancellationTokenSource _cancellation;

        public App()
        {
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            using var db = new AppDbContext();
            db.Database.Migrate();
            var appData = db.ApplicationDatum.FirstOrDefault();
            if (appData == null)
            {
                appData = new ApplicationData();
                appData.Counter = 0;
                db.ApplicationDatum.Add(appData);
                db.SaveChanges();
            }

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            _cancellation = new CancellationTokenSource();
            var u2fBackgroundTask = new U2FHIDBackgroundTask();
            new Thread(() => { u2fBackgroundTask.StartIoLoop(_cancellation.Token); }).Start();
        }

        private void ConfigureServices(IServiceCollection service)
        {
        }
    }
}
