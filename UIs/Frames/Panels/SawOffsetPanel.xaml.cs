using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    public partial class SawOffsetPanel : UserControl
    {
        public EventHandler? PointSelected;

        public SawOffsetItem SawOffsetItem
        {
            get { return (SawOffsetItem)GetValue(SawOffsetItemProperty); }
            set { SetValue(SawOffsetItemProperty, value); }
        }

        public ObservableCollection<ESawOffsetStandardObject> SawOffsetStandardObjectSources
        {
            get { return (ObservableCollection<ESawOffsetStandardObject>)GetValue(SawOffsetStandardObjectSourcesProperty); }
            set { SetValue(SawOffsetStandardObjectSourcesProperty, value); }
        }

        public static readonly DependencyProperty SawOffsetItemProperty =
            DependencyProperty.Register("SawOffsetItem", typeof(SawOffsetItem), typeof(SawOffsetPanel), new PropertyMetadata(null, updateCheckedState));

        private static void updateCheckedState(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SawOffsetPanel control && e.NewValue is SawOffsetItem item)
            {
                control.xCheckBox_Top.IsChecked = item.Directions.Contains(EDirection.Top);
                control.xCheckBox_Bottom.IsChecked = item.Directions.Contains(EDirection.Bottom);
                control.xCheckBox_Left.IsChecked = item.Directions.Contains(EDirection.Left);
                control.xCheckBox_Right.IsChecked = item.Directions.Contains(EDirection.Right);
            }
        }

        public static readonly DependencyProperty SawOffsetStandardObjectSourcesProperty =
            DependencyProperty.Register("SawOffsetStandardObjectSources", typeof(ObservableCollection<ESawOffsetStandardObject>), typeof(SawOffsetPanel), new PropertyMetadata(null));


        public SawOffsetPanel()
        {
            InitializeComponent();

            xPointPickPanel.PointSelected += xPointPickPanel_PointSelected;
        }

        private void xPointPickPanel_PointSelected(object? sender, EventArgs e)
        {
            PointSelected?.Invoke(sender, e);
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            EDirection direction = (EDirection)checkBox.CommandParameter;

            SawOffsetItem.Directions.Add(direction);
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            EDirection direction = (EDirection)checkBox.CommandParameter;

            SawOffsetItem.Directions.Remove(direction);
        }
    }
}
