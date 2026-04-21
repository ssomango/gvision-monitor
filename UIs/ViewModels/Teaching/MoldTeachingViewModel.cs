using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using AnyDiff.Extensions;
using GVisionWpf.Services;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.Inspection.Result;
using System.Linq;
using GVisionWpf.UIs.UiUpdaters;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public sealed partial class MoldTeachingViewModel : SingleTeachingViewModelBase<MoldTeaching>
    {
        private RecipeRepository<MoldTeaching> Repository;

        private ITeachingInspectionService<MoldTeaching, MapInspectionResult, MoldInspectionItem> teachingService;

        private HashSet<MoldInspectionItem> InspectionItems => GlobalSetting.Instance.Inspection.MoldItems;

        #region Fields
        private Size packageSize;
        private CornerDegree cornerDegree;
        private Pose textOffset;
        #endregion

        #region Property
        public double CornerDegreeTopLeft
        {
            get => this.cornerDegree.TopLeft;
            set => SetProperty(ref this.cornerDegree.TopLeft, value);
        }

        public double CornerDegreeTopRight
        {
            get => this.cornerDegree.TopRight;
            set => SetProperty(ref this.cornerDegree.TopRight, value);
        }

        public double CornerDegreeBottomLeft
        {
            get => this.cornerDegree.BottomLeft;
            set => SetProperty(ref this.cornerDegree.BottomLeft, value);
        }

        public double CornerDegreeBottomRight
        {
            get => this.cornerDegree.BottomRight;
            set => SetProperty(ref this.cornerDegree.BottomRight, value);
        }

        public double TextOffsetX
        {
            get => this.textOffset.X;
            set => SetProperty(ref this.textOffset.X, value);
        }

        public double TextOffsetY
        {
            get => this.textOffset.Y;
            set => SetProperty(ref this.textOffset.Y, value);
        }

        public double PackageWidth
        {
            get => this.packageSize.Width;
            set => SetProperty(ref this.packageSize.Width, value);
        }

        public double PackageHeight
        {
            get => this.packageSize.Height;
            set => SetProperty(ref this.packageSize.Height, value);
        }


        [ObservableProperty]
        private double sawOffsetX;

        [ObservableProperty]
        private double sawOffsetY;

        [ObservableProperty]
        private int rejectMarkCount;

        [ObservableProperty]
        private int chippingCount;

        [ObservableProperty]
        private int packageCount;

        [ObservableProperty]
        private int scratchCount;

        [ObservableProperty]
        private int foreignMaterialCount;

        [ObservableProperty]
        private int contaminationCount;

        [ObservableProperty]
        private ObservableCollection<MarkItemSource> markItemSources = new ObservableCollection<MarkItemSource>();

        [ObservableProperty]
        private ObservableCollection<ESawOffsetStandardObject> sawOffsetStandardObjectSources = [ESawOffsetStandardObject.FirstPin, ESawOffsetStandardObject.Mark];
        #endregion

        public MoldTeachingViewModel()
        {
            Repository = MoldRepository.Instance;
            teachingService = new MoldTeachingInspectionService();

            Teaching = new MoldTeaching();

            try { Teaching = Repository.GetRecipe(); }
            catch { Teaching = new MoldTeaching(); }

            if (Teaching.SawOffsetItems.IsNullOrEmpty())
                Teaching.SawOffsetItems = [new SawOffsetItem(sources: SawOffsetStandardObjectSources)];

            Teaching.MarkItems.ForEach(elem => MarkItemSources.Add(new MarkItemSource(elem)));
        }

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

            await HistoryService.Instance.CreateHistory("Mold Teaching", diff);

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
        #endregion

        #region Mark / DataCode
        [RelayCommand]
        private void inspectMarksAndDataCode()
        {
            ClearImage();

            try
            {
                findMarks();
                findDataCode();

                VisionWindow?.Display(new FixedText("Inspection done", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to found", 1, EColor.Red));
            }
        }

        private void findMarks()
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            ArgumentNullException.ThrowIfNull(teachingService.MarkTeachingService);

            Teaching.MarkItems.Clear();

            foreach (var markItemSource in MarkItemSources)
            {
                Teaching.MarkItems.Add(new MarkItem(markItemSource));
            }

            var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

            teachingService.MarkTeachingService.TeachMarks(
                teachingImage: TeachingImage,
                packageRegion: alignContext.PackageRegion,
                teaching: Teaching,
                dontcare: Teaching,
                out InspectionRenderData renderData
                )
                .MergeTo(Teaching);

            Pose convertedOffset = Teaching.TextOffset.ConvertFromPixel(ECamera.Mapping);

            TextOffsetX = convertedOffset.X;
            TextOffsetY = convertedOffset.Y;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }

        private void findDataCode()
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            ArgumentNullException.ThrowIfNull(teachingService.DataCodeTeachingService);

            var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, ECamera.PRS);

            teachingService.DataCodeTeachingService.InspectDataCodes(
                alignContext: alignContext,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }

        #endregion

        #region Surface
        [RelayCommand]
        private void inspectSurface()
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);

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

                renderData.MergeWith(scratchRender);
                ScratchCount = scratchResult.Scratch.Value;

                var foreignMaterialsResult = teachingService.SurfaceTeachingService.InspectForeignMaterials(
                  alignContext: alignContext,
                  teaching: Teaching,
                  enforceAllChecks: true,
                  inspectionItems: InspectionItems,
                  out InspectionRenderData foreignMaterialRender
                  );

                renderData.MergeWith(foreignMaterialRender);
                ForeignMaterialCount = foreignMaterialsResult.ForeignMaterial.Value;

                var contaminationResult = teachingService.SurfaceTeachingService.InspectContaminations(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData contaminationRender
                    );

                renderData.MergeWith(contaminationRender);
                ContaminationCount = contaminationResult.Contamination.Value;

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

        #region Sawing
        private IChippingInspectionResultModel<MapInspectionResult> findChipping(AlignContext alignContext, ECamera camera, ISawingTeachingModel<MoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            var chippingResult = teachingService.SawingTeachingService.InspectChipping(
                alignContext: alignContext,
                camera: camera,
                teaching: teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            ChippingCount = chippingResult.Chipping.Value;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            VisionWindow?.Display(alignContext.PackageRegion, EColor.Green);

            return chippingResult;
        }

        private ICornerDegreeInspectionResultModel<MapInspectionResult> findCornerDegree(AlignContext alignContext, double tolerance, ISawingTeachingModel<MoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            var cornerDegreeResult = teachingService.SawingTeachingService.InspectCornerDegree(
                alignContext: alignContext,
                tolerance: tolerance,
                teaching: teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

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

                CurrentTeachingWindow.Instance?.Window?.Display(new FloatingText(angle.ToString("N2"), alignContext.PackagePoints[pointIndex], EColor.Green));
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            return cornerDegreeResult;
        }

        private ISawOffsetInspectionResultModel<MapInspectionResult> findSawOffset(AlignContext alignContext, double xTolerance, double yTolerance, ECamera camera, ISawingTeachingModel<MoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoiRight);

            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            var sawOffsetResult = teachingService.SawingTeachingService.InspectSawOffset(
                alignContext: alignContext,
                xTolerance: xTolerance,
                yTolerance: yTolerance,
                camera: camera,
                teaching: teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            SawOffsetX = sawOffsetResult.SawOffset.Value?.X ?? 0.0;
            SawOffsetY = sawOffsetResult.SawOffset.Value?.Y ?? 0.0;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            CurrentTeachingWindow.Instance?.Window?.Display(alignContext.PackageRegion, EColor.Green);

            return sawOffsetResult;
        }

        [RelayCommand]
        private void inspectSawing()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            ECamera camera = ECamera.PRS;

            double cornerDegreeTolerance = GlobalSetting.Instance.Inspection.Tolerance.MapCornerDegree;
            double xTolerance = GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX;
            double yTolerance = GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetY;

            var alignContext = teachingService.GetSingleAlignContext(TeachingImage, Teaching, camera);

            teachingService.SawingTeachingService.TeachSawOffset(
                teachingImage: TeachingImage,
                alignContext: alignContext,
                camera: camera,
                teaching: Teaching,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                )
                .MergeTo(Teaching);

            var result = new MapInspectionResult();

            result.CornerDegree = findCornerDegree(alignContext, cornerDegreeTolerance, Teaching).CornerDegree;

            result.SawOffset = findSawOffset(alignContext, xTolerance, yTolerance, camera, Teaching).SawOffset;

            result.Chipping = findChipping(alignContext, camera, Teaching).Chipping;

            ShowInspectionResultText([result]);
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

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            ShowInspectionResultText([rejectMarkResult]);
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

        protected override void ShowInspectionResultText(IEnumerable<IInspectionResultModel> results)
        {
            var result = (MapInspectionResult)results.First();
            List<FixedText> textList = new List<FixedText>();


            if (result.HasDevice.Type == EResultType.NoDevice)
            {
                textList.Add(new FixedText("Result : " + EResultType.NoDevice.ToString().ToUpper(), 1, EColor.Red));
            }
            else
            {
                if (InspectionItems.Contains(MoldInspectionItem.PackageOffset))
                {
                    textList.Add(new FixedText($"Package Offset: {result.PackageOffset.Value}", 4));
                }

                if (InspectionItems.Contains(MoldInspectionItem.PackageSize))
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
