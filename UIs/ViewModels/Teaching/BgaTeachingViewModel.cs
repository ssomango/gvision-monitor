using AnyDiff.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Data.Inspection.Result;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.Types;
using GVisionWpf.UIs.UiUpdaters;
using GVisionWpf.Visions;
using System.Collections.ObjectModel;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public partial class BgaTeachingViewModel : SingleTeachingViewModelBase<BgaTeaching>
    {

        private RecipeRepository<BgaTeaching> Repository;

        private ITeachingInspectionService<BgaTeaching, BgaInspectionResult, BgaInspectionItem> teachingService;

        private HashSet<BgaInspectionItem> InspectionItems => GlobalSetting.Instance.Inspection.BgaItems;

        private Size packageSize;

        private CornerDegree cornerDegree;

        #region Property

        public double PackageWidth
        {
            get => packageSize.Width;
            set => SetProperty(ref packageSize.Width, value);
        }
        public double PackageHeight
        {
            get => packageSize.Height;
            set => SetProperty(ref packageSize.Height, value);
        }
        public double CornerDegreeTopLeft
        {
            get => cornerDegree.TopLeft;
            set => SetProperty(ref cornerDegree.TopLeft, value);
        }
        public double CornerDegreeTopRight
        {
            get => cornerDegree.TopRight;
            set => SetProperty(ref cornerDegree.TopRight, value);
        }
        public double CornerDegreeBottomLeft
        {
            get => cornerDegree.BottomLeft;
            set => SetProperty(ref cornerDegree.BottomLeft, value);
        }
        public double CornerDegreeBottomRight
        {
            get => cornerDegree.BottomRight;
            set => SetProperty(ref cornerDegree.BottomRight, value);
        }

        [ObservableProperty]
        private int scratchCount;

        [ObservableProperty]
        private int foreignMaterialCount;

        [ObservableProperty]
        private int contaminationCount;

        [ObservableProperty]
        private int chippingCount;

        [ObservableProperty]
        private int burrCount;

        [ObservableProperty]
        private double sawOffsetX;

        [ObservableProperty]
        private double sawOffsetY;

        [ObservableProperty]
        private int rejectMarkCount;

        [ObservableProperty]
        private int patternCount;

        [ObservableProperty]
        private int ballCount;

        [ObservableProperty]
        private ObservableCollection<ESawOffsetStandardObject> sawOffsetStandardObjectSources = [ESawOffsetStandardObject.FirstPin, ESawOffsetStandardObject.Pattern, ESawOffsetStandardObject.Ball];

        [ObservableProperty]
        private string symbol = "";

        #endregion

        public BgaTeachingViewModel()
        {
            Repository = BgaRepository.Instance;
            teachingService = new BgaTeachingInspectionService();

            try
            {
                Teaching = BgaRepository.Instance.GetRecipe();
            }
            catch
            {
                Teaching = new BgaTeaching();
            }
        }

        [RelayCommand]
        private void close() => Dispose();

        [RelayCommand]
        private async void saveRecipe()
        {
            var originTeaching = Repository.GetRecipe();
            var currentTeaching = DeepCopy.Copy(Teaching);

            originTeaching.ModelHandleForAlign = null;
            currentTeaching.ModelHandleForAlign = null;

            originTeaching.HomMat2DModelForAlign = null;
            currentTeaching.HomMat2DModelForAlign = null;

            var diff = originTeaching.Diff(currentTeaching, AnyDiff.ComparisonOptions.CompareProperties | AnyDiff.ComparisonOptions.TreatEmptyListAndNullTheSame);

            await HistoryService.Instance.CreateHistory("BGA Teaching", diff);

            Teaching.IsTaught = true;

            Repository.SaveRecipe(Teaching);

        }

        #region Packages
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
                ClearImage();
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
            ClearImage();

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
        private void findAutoThreshold()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SinglePackageTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            teachingService.SinglePackageTeachingService.TeachAutoThreshold(
                teachingImage: TeachingImage,
                teaching: Teaching,
                out InspectionRenderData renderData
                )
                .MergeTo(Teaching);

            if (Teaching.PackageThresholdDiff != 0)
            {
                VisionWindow?.Display(new FixedText("Successfully found edge threshold amplitude", 1, EColor.Green));
            }
            else
            {
                VisionWindow?.Display(new FixedText("Failed to find", 1, EColor.Red));
            }
        }

        [RelayCommand]
        private void inspectPackage()
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

                var packageResult = teachingService.SinglePackageTeachingService.InspectPackage(
                    image: TeachingImage,
                    teaching: Teaching,
                    camera: ECamera.PRS,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out _,
                    out InspectionRenderData renderData
                    );

                PackageWidth = packageResult.PackageSize.Value.Width;
                PackageHeight = packageResult.PackageSize.Value.Height;

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

                ShowInspectionResultText([packageResult]);
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to found", 1, EColor.Red));
            }
        }
        #endregion

        #region Patterns / FirstPin
        private void findPatterns()
        {
            ArgumentNullException.ThrowIfNull(teachingService.PatternTeachingService);

            try
            {
                teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out _);

                teachingService.PatternTeachingService.TeachPatterns(
                    teachingImage: TeachingImage,
                    teaching: Teaching,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData renderData
                    )
                    .MergeTo(Teaching);

                PatternCount = Teaching.Patterns.Count;

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

                VisionWindow?.Display(new FixedText("Successfully found firstPin and patterns", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to found", 1, EColor.Red));
            }
        }

        private void findFirstPin()
        {
            ArgumentNullException.ThrowIfNull(teachingService.FirstPinTeachingService);

            teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out _);

            teachingService.FirstPinTeachingService.TeachFirstPin(
                teachingImage: TeachingImage,
                teaching: Teaching,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                )
                .MeregTo(Teaching);

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }


        [RelayCommand]
        private void findFirstPinAndPattern()
        {
            findFirstPin();
            findPatterns();
        }
        #endregion

        #region Ball
        [RelayCommand]
        private void findBalls()
        {
            ArgumentNullException.ThrowIfNull(teachingService.BallTeachingService);

            teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out _);
            HOperatorSet.ErosionCircle(packageRegion, out packageRegion, 2);
            VisionOperation.ReduceDomain(TeachingImage!, packageRegion, out HObject reducedImage);

            teachingService.BallTeachingService.TeachBalls(
                teachingImage: reducedImage,
                teaching: Teaching,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                )
                .MergeTo(Teaching);

            BallCount = Teaching.Balls.Count;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            var textColor = BallCount > 0 ? EColor.Green : EColor.Red;
            VisionWindow?.Display(new FixedText($"Successfully found {BallCount} balls", 1, textColor));
        }

        [RelayCommand]
        private void findBallRoiAuto()
        {
            ArgumentNullException.ThrowIfNull(teachingService.BallTeachingService);

            try
            {
                teachingService.BallTeachingService.FindBallAutoRoi(TeachingImage, Teaching)
                    .MergeTo(Teaching);

                VisionWindow?.Display(new FixedText("Successfully found the ball ROI", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to ball auto ROI", 1, EColor.Red));
            }
        }
        #endregion

        #region surface
        [RelayCommand]
        private void inspectSurface()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

            ClearImage();

            try
            {
                teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out _);

                InspectionRenderData renderData = new InspectionRenderData();

                var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

                var scratchResult = teachingService.SurfaceTeachingService.InspectScratches(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData scratchRender
                    );

                ScratchCount = scratchResult.Scratch.Value;
                renderData.MergeWith(scratchRender);

                var foreignMaterialsResult = teachingService.SurfaceTeachingService.InspectForeignMaterials(
                  alignContext: alignContext,
                  teaching: Teaching,
                  enforceAllChecks: true,
                  inspectionItems: InspectionItems,
                  out InspectionRenderData foreignMaterialRender
                  );

                ForeignMaterialCount = foreignMaterialsResult.ForeignMaterial.Value;
                renderData.MergeWith(foreignMaterialRender);

                var contaminationResult = teachingService.SurfaceTeachingService.InspectContaminations(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData contaminationRender
                    );

                ContaminationCount = contaminationResult.Contamination.Value;
                renderData.MergeWith(contaminationRender);

                renderData.ResultDrawings.Add((packageRegion, EColor.Green));
                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

                ShowInspectionResultText([scratchResult, contaminationResult, foreignMaterialsResult]);
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Inspection fail", 1, EColor.Red));
            }
        }
        #endregion

        #region sawing
        [RelayCommand]
        private void inspectSawing()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

            ClearImage();

            try
            {
                var renderData = new InspectionRenderData();
                var camera = ECamera.PRS;

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
                    xTolerance: GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetX,
                    yTolerance: GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetY,
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
                    tolerance: GlobalSetting.Instance.Inspection.Tolerance.BgaCornerDegree,
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

                VisionWindow?.Display(alignContext.PackageRegion, EColor.Green);

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

                    VisionWindow?.Display(new FloatingText(angle.ToString("N2"), alignContext.PackagePoints[pointIndex], EColor.Green));
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

        #region rejectMark
        [RelayCommand]
        private void inspectRejectMark()
        {
            ArgumentNullException.ThrowIfNull(teachingService.RejectMarkTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

            RejectMarkCount = 0;

            teachingService.GetPackageRegion(TeachingImage, Teaching, out HObject packageRegion, out _);

            var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

            var rejectMarkResult = teachingService.RejectMarkTeachingService.InspectRejectMark(
                alignContext: alignContext,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData);

            if (rejectMarkResult.RejectMark.Type == EResultType.RejectMark)
            {
                RejectMarkCount += rejectMarkResult.RejectMark.Value;
            }

            ShowInspectionResultText([rejectMarkResult]);

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }
        #endregion

        [RelayCommand]
        private async void inspect()
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(Teaching.PackageModelRoi);

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

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    InspectionResultStr = content;

                    CurrentTeachingWindow.Instance?.Window?.Display(results.First().InspectionResult.Image);

                    VisionWindow?.Display(results);

                    //results.ForEach(r => VisionWindow?.Display(r));
                });
                #endregion
            }
            catch (Exception ex)
            {
                CurrentTeachingWindow.Instance.Window!.Display(new FixedText("Inspection fail", 1, EColor.Red));
                GlobalErrorHandler.HandleException(ex);
            }
        }

        protected override void ShowInspectionResultText(IEnumerable<IInspectionResultModel> results)
        {
            //var result = (BgaInspectionResult)results.First();
            List<FixedText> textList = new List<FixedText>();

            var displayHasDeviceText = false;
            var displayPackageOffset = false;
            var displayPackageSize = false;
            EResultType resultType = EResultType.Good;

            foreach (var result in results)
            {
                var bgaResult = (BgaInspectionResult)result;

                if (bgaResult.HasDevice.Type == EResultType.NoDevice && !displayHasDeviceText)
                {
                    displayHasDeviceText = true;
                    textList.Add(new FixedText("Result : " + EResultType.NoDevice.ToString().ToUpper(), 1, EColor.Red));
                }
                else
                {
                    if (InspectionItems.Contains(BgaInspectionItem.PackageOffset) && !displayPackageOffset
                        && !bgaResult.PackageOffset.Value.IsZero())
                    {
                        displayPackageOffset = true;
                        textList.Add(new FixedText($"Package Offset: {bgaResult.PackageOffset.Value}", 4));
                    }

                    if (InspectionItems.Contains(BgaInspectionItem.PackageSize) && !displayPackageSize
                        && !bgaResult.PackageSize.Value.IsZero())
                    {
                        displayPackageSize = true;
                        textList.Add(new FixedText($"Package Size: {bgaResult.PackageSize.Value}", 5));
                    }

                    if (InspectionResultConverter.ErrorTypeInEResultType(result) == EResultType.Good) continue;
                    resultType = InspectionResultConverter.ErrorTypeInEResultType(result);

                }
            }

            FixedText totalText = new FixedText("Result : " + resultType.ToString().ToUpper(), 1, resultType == EResultType.Good ? EColor.Green : EColor.Red);
            textList.Add(totalText);


            VisionWindow?.Display(textList.OrderBy(x => x.Sequence).ToList());
        }
    }
}