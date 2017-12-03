using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MemMapView.ViewModels {
    abstract class TabItemViewModelBase : BindableBase {
        string _icon, _text;
        static readonly ICommand EmptyCommand = new DelegateCommand(() => { }, () => false);

        public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
        public string Text { get => _text; set => SetProperty(ref _text, value); }

        protected TabItemViewModelBase(string text = null, string icon = null) {
            Text = text;
            Icon = icon;
        }

        public ICommand RefreshCommand => EmptyCommand;
        public ICommand ViewMemoryCommand => EmptyCommand;
    }
}
