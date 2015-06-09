using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

namespace lab3Client
{
    public partial class App : Application
    {
        private Mutex mutex;
        private const string mutexName = "394cc884-3180-4c0c-a140-72223368323b";
        private bool mutexIsNew;

        private void CheckForSingleInstance()
        {
            mutex = new System.Threading.Mutex(true, mutexName, out mutexIsNew);
            if (!mutexIsNew)
            {
                MessageBox.Show("Another instance of application is running. Please close that instance first before trying to create a new one", "Initialization error");
                this.Shutdown(0);
            }
        }
        private void ChooseServer()
        {
            var serverWindow = new dialogs.chooseServer("127.0.0.1", "1270");
            if (serverWindow.ShowDialog() == true)
                Properties["client"] = new Client(serverWindow.host);
            else
                this.Shutdown(0);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CheckForSingleInstance();
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ChooseServer();
            var startingSkin = new DefaultSkin();
            startingSkin.InitializeComponent();
            App.Current.Resources = startingSkin;
        }
    }
}
