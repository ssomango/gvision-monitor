using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.Models.Entities.Result;
using System.Windows;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class DetailResultViewModel : ViewModelBase
    {
        private static readonly Lazy<DetailResultViewModel> lazy = new Lazy<DetailResultViewModel>(() => new DetailResultViewModel());
        public static DetailResultViewModel Instance => lazy.Value;

        private string description = "Default Value";
        private Visibility visibility = Visibility.Hidden;

        #region Property

        public string Description
        {
            get => this.description;
            set => SetField(ref this.description, value);
        }

        public Visibility Visibility
        {
            get => this.visibility;
            set => SetField(ref this.visibility, value);
        }

        #endregion

        private DetailResultViewModel() 
        {
            GVisionMessenger.Instance.RegisterAll(this);
        }

        public void Display(List<RenderableInspectionResult> renderableResults)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (Visibility == Visibility.Hidden)
                {
                    Visibility = Visibility.Visible;
                }

                string content = string.Empty;

                var inspectionResults = renderableResults.Select(result => result.InspectionResult).ToList();

                for (int i = 0; i < inspectionResults.Count; i++)
                {
                    content += $"Device #{i + 1}\n{inspectionResults[i]}\n";
                }

                Description = content;
            });
        }
    }

    partial class DetailResultViewModel : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.DisplayInspectionResult:
                    if (message.RenderableResult is null)
                        return;

                    Display([message.RenderableResult]);
                    break;

                default:
                    break;
            }
        }
    }

    partial class DetailResultViewModel : IRecipient<MoldInspectionUIUpdateMessage>
    {
        public void Receive(MoldInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EMoldInspectionUIUpdateType.DisplayInspectionResult:
                    Display(message.RenderableResults);
                    break;

                default:
                    break;
            }
        }
    }
}