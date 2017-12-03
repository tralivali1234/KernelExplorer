using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Zodiacon.ManagedWindows.Processes;

namespace MemMapView.Converters {
    sealed class GridRowBackgroundConverter : DependencyObject, IValueConverter {
        public Brush FreeBrush {
            get { return (Brush)GetValue(FreeBrushProperty); }
            set { SetValue(FreeBrushProperty, value); }
        }

        public static readonly DependencyProperty FreeBrushProperty =
            DependencyProperty.Register(nameof(FreeBrush), typeof(Brush), typeof(GridRowBackgroundConverter), new PropertyMetadata(null));

        public Brush ReservedBrush {
            get { return (Brush)GetValue(ReservedBrushProperty); }
            set { SetValue(ReservedBrushProperty, value); }
        }

        public static readonly DependencyProperty ReservedBrushProperty =
            DependencyProperty.Register("ReservedBrush", typeof(Brush), typeof(GridRowBackgroundConverter), new PropertyMetadata(null));

        public Brush CommittedBrush {
            get { return (Brush)GetValue(CommittedBrushProperty); }
            set { SetValue(CommittedBrushProperty, value); }
        }

        public static readonly DependencyProperty CommittedBrushProperty =
            DependencyProperty.Register("CommittedBrush", typeof(Brush), typeof(GridRowBackgroundConverter), new PropertyMetadata(null));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var state = (PageState)value;
            switch (state) {
                case PageState.Committed: return CommittedBrush;
                case PageState.Reserved: return ReservedBrush;
                case PageState.Free: return FreeBrush;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
