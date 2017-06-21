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

namespace JobView {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>(4);

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void LoadAssemblies() {
			var appAssembly = Assembly.GetExecutingAssembly();
			foreach (var resourceName in appAssembly.GetManifestResourceNames()) {
				if (resourceName.StartsWith("jobview.assemblies", StringComparison.InvariantCultureIgnoreCase)) {
					using (var stream = appAssembly.GetManifestResourceStream(resourceName)) {
						var assemblyData = new byte[(int)stream.Length];
						stream.Read(assemblyData, 0, assemblyData.Length);
						var assembly = Assembly.Load(assemblyData);
						_assemblies.Add(assembly.GetName().Name, assembly);
					}
				}
			}
			AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
		}

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
			LoadAssemblies();
			LoadNativeModules();
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			var vm = new MainViewModel();
			var win = new MainWindow { DataContext = vm };
			win.Show();
		}
	}
}
