using AnyDiff.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Panels;
using GVisionWpf.UIs.UiUpdaters;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Point = GVisionWpf.Models.Visions.Point;
using Size = GVisionWpf.Models.Visions.Size;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public sealed partial class LgaTeachingViewModel : ViewModelBase
    {
        private ITeachingInspectionService<LgaTeaching, LgaInspectionResult, LgaInspectionItem> teachingService;

        private HashSet<LgaInspectionItem> InspectionItems => GlobalSetting.Instance.Inspection.LgaItems;

        private RecipeRepository<LgaTeaching> Repository => LgaRepository.Instance;

        #region Fields

        private CornerDegree cornerDegree;

        private Size packageSize;
        #endregion

        #region Property
        public string Symbol => GlobalSetting.Instance.Inspection.LengthUnit.Symbol;

        public double CornerDegreeTopLeft
        {
            get => this.cornerDegree.TopLeft;
            set => SetField(ref this.cornerDegree.TopLeft, value);
        }

        public double CornerDegreeTopRight
        {
            get => this.cornerDegree.TopRight;
            set => SetField(ref this.cornerDegree.TopRight, value);
        }

        public double CornerDegreeBottomLeft
        {
            get => this.cornerDegree.BottomLeft;
            set => SetField(ref this.cornerDegree.BottomLeft, value);
        }

        public double CornerDegreeBottomRight
        {
            get => this.cornerDegree.BottomRight;
            set => SetField(ref this.cornerDegree.BottomRight, value);
        }

        public double PackageWidth
        {
            get => this.packageSize.Width;
            set
            {
                this.packageSize.Width = value;
                OnPropertyChanged();
            }
        }

        public double PackageHeight
        {
            get => this.packageSize.Height;
            set
            {
                this.packageSize.Height = value;
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private VisionWindow? visionWindow;

        [ObservableProperty]
        private LgaTeaching teaching;

        [ObservableProperty]
        private HObject teachingImage;

        [ObservableProperty]
        private string inspectionResultStr = string.Empty;

        [ObservableProperty]
        private double sawOffsetX;

        [ObservableProperty]
        private double sawOffsetY;

        [ObservableProperty]
        private int rejectMarkCount;

        [ObservableProperty]
        private int burrCount;

        [ObservableProperty]
        private int chippingCount;

        [ObservableProperty]
        private int leadCount;

        [ObservableProperty]
        private int leadContaminationCount;

        [ObservableProperty]
        private int scratchCount;

        [ObservableProperty]
        private int foreignMaterialCount;

        [ObservableProperty]
        private int contaminationCount;

        [ObservableProperty]
        private int padCount;

        [ObservableProperty]
        private int padContaminationCount;

        [ObservableProperty]
        private double avgPadWidth;

        [ObservableProperty]
        private double avgPadHeight;

        [ObservableProperty]
        private double maxPadWidth;

        [ObservableProperty]
        private double maxPadHeight;

        [ObservableProperty]
        private double minPadWidth;

        [ObservableProperty]
        private double minPadHeight;

        [ObservableProperty]
        private double avgLeadWidth;

        [ObservableProperty]
        private double avgLeadHeight;

        [ObservableProperty]
        private ObservableCollection<ESawOffsetStandardObject> sawOffsetStandardObjectSources = [ESawOffsetStandardObject.Lead, ESawOffsetStandardObject.Pad];

        #endregion

        public LgaTeachingViewModel()
        {
            try
            {
                Teaching = Repository.GetRecipe();
            }
            catch
            {
                Teaching = new LgaTeaching();
            }

            teachingService = new LgaTeachingInspectionService();
            teachingService.DisposeBy(DisposeBag);
        }

        [RelayCommand]
        private void saveRecipe()
        {
            var originTeaching = Repository.GetRecipe();
            var currentTeaching = DeepCopy.Copy(Teaching);

            originTeaching.ModelHandleForAlign = null;
            currentTeaching.ModelHandleForAlign = null;

            originTeaching.HomMat2DModelForAlign = null;
            currentTeaching.HomMat2DModelForAlign = null;

            var diff = originTeaching.Diff(currentTeaching, AnyDiff.ComparisonOptions.CompareProperties | AnyDiff.ComparisonOptions.TreatEmptyListAndNullTheSame);

            HistoryService.Instance.CreateHistory("Lga Teaching", diff);

            Teaching.IsTaught = true;

            Repository.SaveRecipe(Teaching);
        }

        #region Package
        private void trainPackage()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SinglePackageTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

            try
            {
                teachingService.SinglePackageTeachingService.TrainPackage(
                    teachingImage: TeachingImage,
                    teaching: Teaching
                    )
                    .MergeTo(Teaching);
            }
            catch
            {
                clearImage();
                VisionWindow?.Display(new FixedText("Failed to found", 1, EColor.Red));
            }
        }

        [RelayCommand]
        private void findPackageRoiAuto()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SinglePackageTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            clearImage();

            try
            {
                teachingService.SinglePackageTeachingService.TeachAutoRoi(
                    teachingImage: TeachingImage,
                    teaching: Teaching,
                    out InspectionRenderData renderData
                    )
                    .MergeTo(Teaching);

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
                VisionWindow?.Display(new FixedText("Successfully found the package ROI", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to package auto ROI", 1, EColor.Red));
            }
        }

        [RelayCommand]
        private void autoThreshold()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SinglePackageTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            clearImage();

            teachingService.SinglePackageTeachingService.TeachAutoThreshold(
                teachingImage: TeachingImage,
                teaching: Teaching,
                out InspectionRenderData renderData
                )
                .MergeTo(Teaching);

            var fixedText = Teaching.PackageThresholdDiff != 0
                ? new FixedText("Successfully found edge threshold amplitude", 1, EColor.Green)
                : new FixedText("Failed to find", 1, EColor.Red);

            VisionWindow?.Display(fixedText);
        }

        [RelayCommand]
        private void findPackages()
        {
            Debug.WriteLine("findPackages inside");
            ArgumentNullException.ThrowIfNull(teachingService.SinglePackageTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

            teachingService.SinglePackageTeachingService.TrainPackage(
               teachingImage: TeachingImage,
               teaching: Teaching
               )
               .MergeTo(Teaching);

            var packageResult = teachingService.SinglePackageTeachingService.InspectPackage(
                image: TeachingImage,
                teaching: Teaching,
                camera: ECamera.PRS,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out AlignContext alignContext,
                out InspectionRenderData renderData
                );

            PackageWidth = packageResult.PackageSize.Value.Width;
            PackageHeight = packageResult.PackageSize.Value.Height;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            ShowInspectionResultText([packageResult]);
        }
        #endregion

        #region FirstPin
        private void inspectFirstPin()
        {
            ArgumentNullException.ThrowIfNull(teachingService.FirstPinTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

            var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);
            teachingService.FirstPinTeachingService.InspectFirstPin(
                alignContext: alignContext,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }

        #endregion

        #region Multi Pads
        [RelayCommand]
        private void findPads()
        {
            ArgumentNullException.ThrowIfNull(teachingService.MultiPadTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);
            clearImage();

            try
            {
                teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out List<Point> packagePoints);
                var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

                teachingService.MultiPadTeachingService.TeachPads(
                    teachingImage: TeachingImage,
                    packageRegion: packageRegion,
                    camera: ECamera.PRS,
                    teaching: Teaching,
                    out InspectionRenderData padRender
                    )
                    .MergeTo(Teaching);

                PadCount = Teaching.PadPxPoses.Count();

                MaxPadWidth = Teaching.MultiPadSizes.MemberwiseMax().Width;
                MaxPadHeight = Teaching.MultiPadSizes.MemberwiseMax().Height;

                MinPadWidth = Teaching.MultiPadSizes.MemberwiseMin().Width;
                MinPadHeight = Teaching.MultiPadSizes.MemberwiseMin().Height;

                AvgPadWidth = Teaching.MultiPadSizes.MemberwiseAverage().Width;
                AvgPadHeight = Teaching.MultiPadSizes.MemberwiseAverage().Height;

                var result = teachingService.MultiPadTeachingService.InspectPads(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData renderData
                    );

                PadContaminationCount = result.MultiPadContamination.Value;

                padRender.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
                VisionWindow?.Display(new FixedText("Successfully found pads", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to found", 1, EColor.Red));
            }
        }
        #endregion

        #region Leads
        [RelayCommand]
        private void findLeads()
        {
            ArgumentNullException.ThrowIfNull(teachingService.LeadTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);
            clearImage();

            try
            {
                teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out List<Point> packagePoints);

                teachingService.LeadTeachingService.TeachLeads(
                    teachingImage: TeachingImage,
                    packageRegion: packageRegion,
                    camera: ECamera.PRS,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData leadRender
                    )
                    .MergeTo(Teaching);

                LeadCount = Teaching.LeadPxPoses.Count();

                AvgLeadWidth = Teaching.LeadAverageSize.Width;
                AvgLeadHeight = Teaching.LeadAverageSize.Height;

                var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);
                var result = teachingService.LeadTeachingService.InspectLeads(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData renderData
                    );

                LeadContaminationCount = result.LeadContamination.Value;

                leadRender.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
                VisionWindow?.Display(new FixedText("Successfully found pads", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to found", 1, EColor.Red));
            }
        }
        #endregion

        #region Surface
        [RelayCommand]
        private void findSurface()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);
            clearImage();

            try
            {
                InspectionRenderData renderData = new InspectionRenderData();
                var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

                var scratchResult = teachingService.SurfaceTeachingService.InspectScratches(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData scratchRender
                    );

                renderData.MergeWith(scratchRender);
                ScratchCount = scratchResult.Scratch.Value;

                var contaminationResult = teachingService.SurfaceTeachingService.InspectContaminations(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData contaminationRender
                    );

                renderData.MergeWith(contaminationRender);
                ContaminationCount = contaminationResult.Contamination.Value;

                var foreignMaterialsResult = teachingService.SurfaceTeachingService.InspectForeignMaterials(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData foreignMaterialRender
                    );

                renderData.MergeWith(foreignMaterialRender);
                ForeignMaterialCount = foreignMaterialsResult.ForeignMaterial.Value;

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
                VisionWindow?.Display(new FixedText("Inspection done", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Inspection fail", 1, EColor.Red));
            }
        }

        #endregion

        #region Sawing

        [RelayCommand]
        private void findSawing()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);
            clearImage();

            try
            {
                var renderData = new InspectionRenderData();
                var camera = ECamera.PRS;

                teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out List<Point> packagePoints);
                var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

                teachingService.SawingTeachingService.TeachSawOffset(
                    teachingImage: TeachingImage,
                    alignContext: alignContext,
                    camera: camera,
                    teaching: Teaching,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData teachedSawOffsetRender
                    )
                    .MergeTo(Teaching);

                renderData.MergeWith(teachedSawOffsetRender);

                var sawOffsetResult = teachingService.SawingTeachingService.InspectSawOffset(
                    alignContext: alignContext,
                    xTolerance: GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetX,
                    yTolerance: GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetY,
                    camera: camera,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData sawOffsetRender
                    );

                renderData.MergeWith(sawOffsetRender);
                SawOffsetX = sawOffsetResult.SawOffset.Value?.X ?? 0;
                SawOffsetY = sawOffsetResult.SawOffset.Value?.Y ?? 0;

                var chippingResult = teachingService.SawingTeachingService.InspectChipping(
                    alignContext: alignContext,
                    camera: camera,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData chippingRender
                    );

                renderData.MergeWith(chippingRender);
                ChippingCount = chippingResult.Chipping.Value;

                var burrResult = teachingService.SawingTeachingService.InspectBurr(
                    alignContext: alignContext,
                    camera: camera,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData burrRender
                    );

                renderData.MergeWith(burrRender);
                BurrCount = burrResult.Burr.Value;

                var cornerDegreeResult = teachingService.SawingTeachingService.InspectCornerDegree(
                    alignContext: alignContext,
                    tolerance: GlobalSetting.Instance.Inspection.Tolerance.LgaCornerDegree,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData cornerDegreeRender
                    );

                renderData.MergeWith(cornerDegreeRender);

                CornerDegreeTopLeft = cornerDegreeResult.CornerDegree.Value.TopLeft;
                CornerDegreeTopRight = cornerDegreeResult.CornerDegree.Value.TopRight;
                CornerDegreeBottomLeft = cornerDegreeResult.CornerDegree.Value.BottomLeft;
                CornerDegreeBottomRight = cornerDegreeResult.CornerDegree.Value.BottomRight;


                for (int pointIndex = 0; pointIndex < 4; pointIndex++)
                {
                    var angle = cornerDegreeResult.CornerDegree.Value[pointIndex];
                    _ = pointIndex switch
                    {
                        0 => CornerDegreeTopLeft = angle,
                        1 => CornerDegreeTopRight = angle,
                        2 => CornerDegreeBottomLeft = angle,
                        3 => CornerDegreeBottomRight = angle,
                        _ => throw new ArgumentOutOfRangeException(nameof(pointIndex), "Invalid point index. Expected values: 0 to 3.")
                    };

                    CurrentTeachingWindow.Instance?.Window?.Display(new FloatingText(angle.ToString("N2"), packagePoints[pointIndex], EColor.Green));
                }

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

                VisionWindow?.Display(new FixedText("Inspection done", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Inspection fail", 1, EColor.Red));
            }
        }

        #endregion

        #region Reject Mark

        [RelayCommand]
        private void inspectRejectMark()
        {
            ArgumentNullException.ThrowIfNull(teachingService.RejectMarkTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

            clearImage();

            RejectMarkCount = 0;

            var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

            var rejectMarkResult = teachingService.RejectMarkTeachingService.InspectRejectMark(
                alignContext: alignContext,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            if (rejectMarkResult.RejectMark.Type == EResultType.RejectMark)
            {
                RejectMarkCount += rejectMarkResult.RejectMark.Value;
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            ShowTeachingResultText(rejectMarkResult);
        }
        #endregion

        #region Inspect
        [RelayCommand]
        private async void inspect()
        {
            try
            {
                var results = (await teachingService.InspectAsync(
                    images: [TeachingImage],
                    teaching: Teaching,
                    camera: ECamera.PRS,
                    inspectionItems: InspectionItems
                    ))
                    .ToList();

                #region UI Handling

                string content = string.Empty;

                for (int i = 0; i < results.Count; i++)
                {
                    content += $"Device #{i + 1}\n{results[i].InspectionResult}\n";
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    InspectionResultStr = content;

                    CurrentTeachingWindow.Instance?.Window?.Display(results.First().InspectionResult.Image);

                    results.ForEach(r => VisionWindow?.Display(r));
                });
                #endregion
            }
            catch (Exception ex)
            {
                CurrentTeachingWindow.Instance.Window!.Display(new FixedText("Inspection fail", 1, EColor.Red));
                GlobalErrorHandler.HandleException(ex);
            }
        }

        #endregion

        private void clearImage()
        {
            VisionWindow?.Clear();
            VisionWindow?.Display(TeachingImage);
        }

        protected override void ShowInspectionResultText(IEnumerable<IInspectionResultModel> results)
        {
            var result = (LgaInspectionResult)results.First();
            List<FixedText> textList = new List<FixedText>();

            if (result.HasDevice.Type == EResultType.NoDevice)
            {
                textList.Add(new FixedText("Result : " + EResultType.NoDevice.ToString().ToUpper(), 1, EColor.Red));
            }
            else
            {
                if (InspectionItems.Contains(LgaInspectionItem.PackageOffset))
                {
                    textList.Add(new FixedText($"Package Offset: {result.PackageOffset.Value}", 4));
                }

                if (InspectionItems.Contains(LgaInspectionItem.PackageSize))
                {
                    textList.Add(new FixedText($"Package Szie: {result.PackageSize.Value}", 5));
                }

                EResultType resultType = InspectionResultConverter.ErrorTypeInEResultType(result);
                FixedText totalText = new FixedText("Result : " + resultType.ToString().ToUpper(), 1, resultType == EResultType.Good ? EColor.Green : EColor.Red);
                textList.Add(totalText);
            }

            VisionWindow?.Display(textList.OrderBy(x => x.Sequence).ToList());
        }
    }
}