using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GVisionWpf.Models.UiModels
{
    public class MarkItemSource : INotifyPropertyChanged
    {
        private bool isOcrMode;
        private Roi roi;
        private int minMatchingRate;
        public int nCharacters;
        private HObject? sampleImage;
        private string ocrText;

        #region Property

        public bool IsOcrMode
        {
            get => this.isOcrMode;
            set
            {
                this.isOcrMode = value;
                OnPropertyChanged();
            }
        }

        public Roi Roi
        {
            get => this.roi;
            set
            {
                this.roi = value;
                OnPropertyChanged();
            }
        }

        public HObject? SampleImage
        {
            get => this.sampleImage;
            set
            {
                this.sampleImage = value;
                OnPropertyChanged();
            }
        }

        public int MinMatchingRate
        {
            get => this.minMatchingRate;
            set
            {
                this.minMatchingRate = value;
                OnPropertyChanged();
            }
        }

        public string OcrText
        {
            get => this.ocrText;
            set
            {
                this.ocrText = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public MarkItemSource()
        {
            this.roi = new Roi("MARK");
            this.minMatchingRate = 50;
            this.ocrText = string.Empty;
            this.nCharacters = 50;
        }

        public MarkItemSource(MarkItem markItem)
        {
            this.isOcrMode = markItem.Mode == EMarkMode.Ocr;
            this.roi = markItem.Roi;
            this.minMatchingRate = markItem.MinMatchingRate;
            this.nCharacters = markItem.nCharacters;
            this.ocrText = markItem.OcrText;
        }
    }
}