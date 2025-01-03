using System.Windows;
using System.Windows.Controls;

namespace ISSS3.Classes {
    public class PropertyBlock : Label {
        private const double MinAdaptiveWidthDefault = 300;

        public static readonly DependencyProperty IsSlimProperty = DependencyProperty.Register(nameof(IsSlim),
                 typeof(bool),
                 typeof(PropertyBlock));

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label),
                 typeof(string),
                 typeof(PropertyBlock));

        public static readonly DependencyProperty MinAdaptiveWidthProperty = DependencyProperty.Register(nameof(MinAdaptiveWidth),
                 typeof(double),
                 typeof(PropertyBlock),
                 new PropertyMetadata(MinAdaptiveWidthDefault));

        public bool IsSlim {
            get => (bool)GetValue(IsSlimProperty);
            set => SetValue(IsSlimProperty, value);
        }

        public string Label {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public double MinAdaptiveWidth {
            get => (double)GetValue(MinAdaptiveWidthProperty);
            set => SetValue(MinAdaptiveWidthProperty, value);
        }
    }
}