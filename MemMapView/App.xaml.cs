using MemMapView.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Zodiacon.WPF;

namespace MemMapView {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public const string Title = "Memory Map Viewer";

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            var ui = new UIServicesDefaults();
            var vm = new MainViewModel(ui);
            var win = new MainWindow { DataContext = vm };
            win.Show();
            ui.MessageBoxService.SetOwner(win);
        }
    }
}
