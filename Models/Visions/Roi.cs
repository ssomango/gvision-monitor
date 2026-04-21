using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Interfaces;
using Newtonsoft.Json;

namespace GVisionWpf.Models.Visions
{
    public partial class Roi : ObservableObject, ICopyable<Roi>
    {
        [ObservableProperty]
        private double row1, col1, row2, col2;

        [ObservableProperty]
        private string name;

        public Roi()
        {
            this.name = "ROI";
            this.row1 = 500;
            this.col1 = 500;
            this.row2 = 1000;
            this.col2 = 1000;
        }

        public Roi(string name)
        {
            this.name = name;
            this.row1 = 500;
            this.col1 = 500;
            this.row2 = 1000;
            this.col2 = 1000;
        }

        [JsonConstructor]
        public Roi(string name, double row1, double col1, double row2, double col2)
        {
            this.name = name;
            this.row1 = row1;
            this.col1 = col1;
            this.row2 = row2;
            this.col2 = col2;
        }

        public Roi(Rect rect)
        {
            this.name = "ROI";
            this.row1 = rect.Row1;
            this.col1 = rect.Col1;
            this.row2 = rect.Row2;
            this.col2 = rect.Col2;
        }

        public void CopyFrom(Roi roi)
        {
            this.Name = roi.Name;
            this.Row1 = roi.Row1;
            this.Col1 = roi.Col1;
            this.Row2 = roi.Row2;
            this.Col2 = roi.Col2;
        }
    }
}