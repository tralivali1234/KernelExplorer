using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MemMapView.ViewModels {
    class ProcessViewModel {
        public string Name { get; set; }
        public int Id { get; set; }
        public ImageSource Icon { get; set; }
        int _session;
        public int Session {
            get => _session;
            set {
                if (value < 0)
                    value = 0;
                _session = value;
            }
        }
    }
}
