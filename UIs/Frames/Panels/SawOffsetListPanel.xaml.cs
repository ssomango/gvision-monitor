using GVisionWpf.DomainLayer.Extensions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    public partial class SawOffsetListPanel : UserControl
    {
        public EventHandler? ItemAdded;
        public EventHandler? ItemDeleted;
        public EventHandler? PointsUpdated;

        public ObservableCollection<SawOffsetItem> SawOffsetItems
        {
            get { return (ObservableCollection<SawOffsetItem>)GetValue(SawOffsetItemsProperty); }
            set { SetValue(SawOffsetItemsProperty, value); }
        }

        public ObservableCollection<ESawOffsetStandardObject> SawOffsetStandardObjects
        {
            get { return (ObservableCollection<ESawOffsetStandardObject>)GetValue(SawOffsetStandardObjectsProperty); }
            set { SetValue(SawOffsetStandardObjectsProperty, value); }
        }

        public static readonly DependencyProperty SawOffsetItemsProperty =
            DependencyProperty.Register("SawOffsetItems", typeof(ObservableCollection<SawOffsetItem>), typeof(SawOffsetListPanel), new PropertyMetadata(null));

        public static readonly DependencyProperty SawOffsetStandardObjectsProperty =
              DependencyProperty.Register("SawOffsetStandardObjects", typeof(ObservableCollection<ESawOffsetStandardObject>), typeof(SawOffsetListPanel), new PropertyMetadata(null));

        public SawOffsetListPanel()
        {
            InitializeComponent();
        }

        private void add_SawOffsetItem(object sender, RoutedEventArgs e)
        {
            SawOffsetItem item = new SawOffsetItem(sources: SawOffsetStandardObjects);

            SawOffsetItems.Add(item);
        }

        private void delete_SawOffsetItem(object sender, RoutedEventArgs e)
        {
            if (SawOffsetItems.IsNullOrEmpty()) return;

            Button button = (Button)sender;

            SawOffsetItem item = (SawOffsetItem)button.CommandParameter;

            int selectedIndex = findIndex(item);

            SawOffsetItems.RemoveAt(selectedIndex);

            PointsUpdated?.Invoke(this, new ItemEventArgs<List<Models.Visions.Point>>(SawOffsetItems.Select(e => e.SawOffsetTargetPoint).ToList()));
        }

        private int findIndex(SawOffsetItem item)
        {
            int selectedIndex = SawOffsetItems.IndexOf(item);
            return selectedIndex;
        }

        private void xSawOffsetPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ItemAdded?.Invoke(sender, new ItemEventArgs<int>(SawOffsetItems.Count - 1));

            if (sender is SawOffsetPanel sawOffsetPanel)
            {
                sawOffsetPanel.PointSelected += sendPointsUpdated;
            }
        }

        private void xSawOffsetPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            ItemDeleted?.Invoke(sender, e);

            if (sender is SawOffsetPanel sawOffsetPanel)
            {
                sawOffsetPanel.PointSelected -= sendPointsUpdated;
            }
        }

        private void sendPointsUpdated(object? sender, EventArgs e)
        {
            PointsUpdated?.Invoke(this, new ItemEventArgs<List<Models.Visions.Point>>(SawOffsetItems.Select(e => e.SawOffsetTargetPoint).ToList()));
        }
    }
}