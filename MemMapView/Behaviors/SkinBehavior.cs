using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace MemMapView.Behaviors {
    sealed class SkinBehavior : Behavior<FrameworkElement> {
        protected override void OnAttached() {
            base.OnAttached();

            SfSkinManager.SetVisualStyle(AssociatedObject, VisualStyle);
        }



        public VisualStyles VisualStyle {
            get { return (VisualStyles)GetValue(VisualStyleProperty); }
            set { SetValue(VisualStyleProperty, value); }
        }

        public static readonly DependencyProperty VisualStyleProperty =
            DependencyProperty.Register(nameof(VisualStyle), typeof(VisualStyles), typeof(SkinBehavior), 
                new PropertyMetadata(VisualStyles.Metro));


    }
}
