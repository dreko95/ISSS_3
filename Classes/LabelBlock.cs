using System.Windows;
using System.Windows.Controls;

namespace ISSS3.Classes {
    public class LabelBlock : TextBlock {
        public static int TextHeight = 19;
        public static int LinesCount = 2;
        public static int TwoLinesHeightLimit = 2 * TextHeight - 1;

        public static readonly DependencyProperty IsSlimProperty = DependencyProperty.Register(nameof(IsSlim),
                 typeof(bool),
                 typeof(LabelBlock));

        public static readonly DependencyProperty ContentPlacementProperty = DependencyProperty.Register(nameof(ContentPlacement),
                 typeof(Dock),
                 typeof(LabelBlock));

        public int TwoLinesHeight => 2 * TextHeight;
        public int LinesHeight => LinesCount * TextHeight;

        public bool IsSlim {
            get => (bool)GetValue(IsSlimProperty);
            set => SetValue(IsSlimProperty, value);
        }

        public Dock ContentPlacement {
            get => (Dock)GetValue(ContentPlacementProperty);
            set => SetValue(ContentPlacementProperty, value);
        }
    }
}