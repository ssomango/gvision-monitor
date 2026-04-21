using AnyDiff.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Panels;
using System.Windows;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public partial class StripTeachingViewModel : ViewModelBase
    {
        private StripTeachingInspectionService teachingService;

        private StripRepository Repository;
        

        [ObservableProperty]
        private HObject teachingImage;

        [ObservableProperty]
        private StripTeaching teaching;

        [ObservableProperty]
        private VisionWindow? visionWindow;

        [ObservableProperty]
        private string decodedString = string.Empty;

        public StripTeachingViewModel()
        {
            teachingService = new StripTeachingInspectionService();
            Repository = StripRepository.Instance;

            try
            {
                Teaching = StripRepository.Instance.GetRecipe();
            }
            catch
            {
                Teaching = new StripTeaching();
            }
        }

        [RelayCommand]
        private void findCode()
        {
            ArgumentNullException.ThrowIfNull(teachingService.StripTeachingService);

            ClearImage();

            var result = teachingService.StripTeachingService.InspectStripDataCode(
                TeachingImage, 
                Teaching,
                out InspectionRenderData render
                );

            DecodedString = result.StripDataCode.Value == string.Empty ? "Not Found DataCode" : result.StripDataCode.Value;

            render.ResultDrawings.ForEach(r => VisionWindow?.Display(r.drawingObject, r.color));
            VisionWindow?.Display(render.FixedTexts);

            try
            {
                MessageBox.Show($"DataCode {DecodedString}\n\nCenter X {result.XOffset?.Value ?? 0}\nCenter Y {result.YOffset?.Value ?? 0}");
            }
            catch
            {
                MessageBox.Show("Not found datacode");
            }
           
        }

        [RelayCommand]
        private void saveRecipe()
        {
            var originTeaching = Repository.GetRecipe();

            var diff = originTeaching.Diff(Teaching, AnyDiff.ComparisonOptions.CompareProperties | AnyDiff.ComparisonOptions.TreatEmptyListAndNullTheSame);

            HistoryService.Instance.CreateHistory("Strip Teaching", diff);

            Teaching.IsTaught = true;

            StripRepository.Instance.SaveRecipe(Teaching);
        }

        public void ClearImage()
        {
            VisionWindow?.Clear();
            VisionWindow?.Display(TeachingImage);
        }
    }
}
