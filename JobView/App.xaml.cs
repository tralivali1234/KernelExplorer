using JobView.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Zodiacon.WPF;

namespace JobView {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>(4);

        public static string Title => "Job Objects View";


        void LoadNativeModules() {
            var appAssembly = Assembly.GetExecutingAssembly();
            var location = Path.GetDirectoryName(appAssembly.Location) + "\\";
            var resPrefix = "jobview.nativebinaries.";

            foreach (var resourceName in appAssembly.GetManifestResourceNames()) {
                if (resourceName.StartsWith(resPrefix, StringComparison.InvariantCultureIgnoreCase)) {
                    using (var stream = appAssembly.GetManifestResourceStream(resourceName)) {
                        var file = location + resourceName.Substring(resPrefix.Length);
                        if (File.Exists(file))
                            continue;

                        var assemblyData = new byte[(int)stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        File.WriteAllBytes(file, assemblyData);
                    }
                }
            }
        }

        Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            var shortName = new AssemblyName(args.Name).Name;
            Assembly assembly;
            if (_assemblies.TryGetValue(shortName, out assembly)) {
                return assembly;
            }
            return null;
        }

        public App() {
            //LoadNativeModules();
        }

        MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            IUIServices ui = new UIServicesDefaults();
            var win = new MainWindow();
            ui.MessageBoxService.SetOwner(win);
            _mainViewModel = new MainViewModel(ui);
            win.DataContext = _mainViewModel;
            win.Show();
        }

        protected override void OnExit(ExitEventArgs e) {
            base.OnExit(e);

            _mainViewModel.Dispose();
        }
    }
}
