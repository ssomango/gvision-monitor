using GVisionWpf.Illuminations;
using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// LightWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LightWindow : Window
    {
        private readonly LightManager lightManager = LightManager.Instance;
        private readonly LightViewModel viewModel = LightViewModel.Instance;


        public LightWindow()
        {
            InitializeComponent();

            this.DataContext = this.viewModel;
            ((LightViewModel)this.DataContext).LoadRecipe();
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            this.lightManager.TurnOffAllLightsFromAllCamera();

            this.xLivePanel.StopLive();
            this.Close();
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.viewModel.LightControllerCollection.Clear();
            addColumn();
            this.viewModel.LoadDataGridItems();
        }

        private void addColumn()
        {
            this.viewModel.ELights.Clear();
            this.xShotDataGrid.Columns.Clear();
            this.viewModel.Columns.Clear();

            this.viewModel.IlluminationRecipe.Setting.TryGetValue(this.viewModel.CameraSelectedValue, out var value);
            if (value == null) { return; }

            var shotNumber = new DataGridTextColumn
            {
                Header = "Shot #",
                Binding = new Binding("ShotNumber"),
                IsReadOnly = true,
                Width = 60
            };
            this.viewModel.Columns.Add(shotNumber);
            this.xShotDataGrid.Columns.Add(shotNumber);


            foreach (KeyValuePair<ELight, Light> d in this.lightManager.Lights[this.viewModel.CameraSelectedValue])
            {
                this.viewModel.ELights.Add(d.Key);

                var column = new DataGridTextColumn
                {
                    Header = d.Value.Name,
                    Binding = new Binding(d.Key.ToString()),
                    IsReadOnly = true,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };
                this.viewModel.Columns.Add(column);
                this.xShotDataGrid.Columns.Add(column);
            }
        }

        //public void UpdateProperty(string selectedItem)
        //{
        //    bool isSuccessed = Enum.TryParse<ECamera>(selectedItem, out ECamera selectedCamera);
        //    if (!isSuccessed)
        //    {
        //        // 세상에 없는 카메라를 켜달라고 한거임
        //        // 경고를 띄우던 메시지를 보내던 하기
        //        return;
        //    }

        //    if (!this.CameraComboBox.Items.Contains(selectedCamera))
        //    {
        //        // 등록되어있지 않은 카메라를 켜달라고 한 상황
        //        return;
        //    }

        //    this.CameraComboBox.SelectedValue = selectedCamera;
        //}
    }
}
