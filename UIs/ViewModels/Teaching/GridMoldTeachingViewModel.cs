using System.Collections.ObjectModel;
using System.Windows;
using AnyDiff.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.UiUpdaters;
using Size = GVisionWpf.Models.Visions.Size;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public partial class GridMoldTeachingViewModel : GridTeachingViewModelBase<GridMoldTeaching>
    {
        private ITeachingInspectionService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem> teachingService;

        private RecipeRepository<GridMoldTeaching> Repository => GridMoldRepository.Instance;

        private HashSet<MoldInspectionItem> InspectionItems => GlobalSetting.Instance.Inspection.MoldItems;

        const int PACKAGE_TAB_NO = 0;
        const int MARK_TAB_NO = 1;
        const int SURFACE_TAB_NO = 3;
        const int SAWING_TAB_NO = 4;
        const int REJECT_TAB_NO = 5;
      
        #region Feilds
        private Size packageSize;
        private Pose textOffset;
        private CornerDegree cornerDegree;
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

        [ObservableProperty]
        private int autoThresholdRevision;

        [ObservableProperty]
        private int findPackageRevision;

        [ObservableProperty]
        private int lastFindPackageGoodCount;

        [ObservableProperty]
        private int lastFindPackageBadCount;
        #endregion

        public GridMoldTeachingViewModel()
        {
            teachingService = new GridMoldTeachingInspectionService().DisposeBy(DisposeBag);

            try { Teaching = Repository.GetRecipe(); }
            catch { Teaching = new GridMoldTeaching(); }

            if (Teaching.SawOffsetItems.IsNullOrEmpty())
                Teaching.SawOffsetItems = [new SawOffsetItem(sources: SawOffsetStandardObjectSources)];

            if (Teaching.ShotNoByTabNo.TryGetValue(0, out int shotNo))
                SelectedShotIndex = shotNo;

            Teaching.MarkItems.ForEach(elem => MarkItemSources.Add(new MarkItemSource(elem)));
        }

        #region Package
        [RelayCommand]
        private void autoThreshold()
        {
            ArgumentNullException.ThrowIfNull(teachingService.GridPackageTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            Teaching.ShotNoByTabNo.TryGetValue(PACKAGE_TAB_NO, out int shotNo);

            HObject image = Shots[shotNo];

            teachingService.GridPackageTeachingService.TeachAutoThreshold(
                teachingImage: image,
                teaching: Teaching
                )
                .MergeTo(Teaching);

            AutoThresholdRevision++;
        }

        [RelayCommand]
        private void findPackages()
        {
            ArgumentNullException.ThrowIfNull(teachingService.GridPackageTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            LastFindPackageGoodCount = 0;
            LastFindPackageBadCount = 0;

            ObservableCollection<int> packagerNumbers = [];
            //PackageNumbers.Clear();

            this.packages.Clear();

            var results = teachingService.GridPackageTeachingService.InspectGridPackages(
                teachingImage: TeachingImage,
                teaching: Teaching,
                camera: ECamera.Mapping,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData packageRender
                )
                .ToList();

            List<Size> sizeList = new List<Size>(results.Count);

            for (int i = 0; i < results.Count; i++)
            {
                packagerNumbers.Add(i + 1);
                //PackageNumbers.Add(i + 1);
                sizeList.Add(results[i].PackageSize.Value);

                var packageRegion = results[i].PackageRegion;
                var packagePoint = results[i].PackagePoints;

                this.packages.Add(key: i + 1, new PackageInfo(packageRegion, packagePoint));
            }

            LastFindPackageGoodCount = results.Count(result => InspectionResultConverter.ErrorTypeInEResultType(result) == EResultType.Good);
            LastFindPackageBadCount = results.Count - LastFindPackageGoodCount;

            StatisticalList<Size> sizes = new StatisticalList<Size>(sizeList);
            PackageWidth = sizes.MemberwiseAverage().Width;
            PackageHeight = sizes.MemberwiseAverage().Height;
            PackageNumbers = packagerNumbers;

            packageRender.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
            VisionWindow?.Display(packageRender.FloatingTexts);

            ShowInspectionResultText(results);
            FindPackageRevision++;
        }
        #endregion

        #region Mark / DataCode
        [RelayCommand]
        private void inspectMarksAndDataCode()
        {
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            if (this.teachingPackageRegion == null)
            {
                new AlertWindow("The Package Required",
                    AlertWindow.EIcon.ALERT,
                    "The package outline must be found at the previous stage.",
                    AlertWindow.EAlert.IMAGE,
                    "/Assets/UiImages/find-mark-guide.png").ShowDialog();
                return;
            }

            ArgumentNullException.ThrowIfNull(teachingService.MarkTeachingService);
            ArgumentNullException.ThrowIfNull(teachingService.DataCodeTeachingService);

            ClearImage();

            findMarks(TeachingImage, teachingPackageRegion, Teaching);

            var renderData = new InspectionRenderData();
            var nGoodDevices = 0;
            var nBadDevices = 0;

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var packageRegion = package.Value.PackageRegion;

                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                var markResult = teachingService.MarkTeachingService.InspectMarks(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData markRender
                    );

                renderData.MergeWith(markRender);

                var dataCodeResult = teachingService.DataCodeTeachingService.InspectDataCodes(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData dataCodeRender
                    );

                renderData.MergeWith(dataCodeRender);

                if (InspectionResultConverter.ErrorTypeInEResultType(markResult) == EResultType.Good
                    && InspectionResultConverter.ErrorTypeInEResultType(dataCodeResult) == EResultType.Good)
                {
                    nGoodDevices++;
                } 
                else
                {
                    nBadDevices++;
                }
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            FixedText totalText = new FixedText($"TOTAL: {nGoodDevices + nBadDevices}, " +
                $"GOOD: {nGoodDevices}, BAD: {nBadDevices}", 1, nBadDevices > 0 ? EColor.Red : EColor.Green);

            CurrentTeachingWindow.Instance?.Window?.Display(totalText);
        }

        private void findMarks(HObject teachingImage, HObject packageRegion, IMarkTeachingModel<GridMoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teachingService.MarkTeachingService);

            Teaching.MarkItems.Clear();

            foreach (var markItemSource in MarkItemSources)
            {
                Teaching.MarkItems.Add(new MarkItem(markItemSource));
            }

            teachingService.MarkTeachingService.TeachMarks(
                teachingImage: teachingImage,
                packageRegion: packageRegion,
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
        #endregion

        #region Surface
        [RelayCommand]
        private void inspectSurface()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);
            ArgumentNullException.ThrowIfNull(teachingPackageRegion);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            ClearImage();

            ScratchCount = 0;
            ForeignMaterialCount = 0;
            ContaminationCount = 0;

            var results = new List<MapInspectionResult>();

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                teachingService.SurfaceTeachingService.ResetInspectionState();
                var result = new MapInspectionResult();
                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                result.Scratch = findScratches(alignContext, Teaching).Scratch;
                result.ForeignMaterial = findForeignMaterials(alignContext, Teaching).ForeignMaterial;
                result.Contamination = findContaminations(alignContext, Teaching).Contamination;

                results.Add(result);
            }

            ShowInspectionResultText(results);
        }

        private IScratchInspectionResultModel<MapInspectionResult> findScratches(AlignContext alignContext, IScratchTeachingModel<GridMoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);
            var scratchesResult = teachingService.SurfaceTeachingService.InspectScratches(
                alignContext: alignContext,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            ScratchCount += scratchesResult.Scratch.Value;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            CurrentTeachingWindow.Instance?.Window?.Display(alignContext.PackageRegion, EColor.Green);

            return scratchesResult;
        }

        private IForeignMaterialInspectionResultModel<MapInspectionResult> findForeignMaterials(AlignContext alignContext, IForeignMaterialTeachingModel<GridMoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);
            var foreignMaterialsResult = teachingService.SurfaceTeachingService.InspectForeignMaterials(
                alignContext: alignContext,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            ForeignMaterialCount += foreignMaterialsResult.ForeignMaterial.Value;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            CurrentTeachingWindow.Instance?.Window?.Display(alignContext.PackageRegion, EColor.Green);

            return foreignMaterialsResult;
        }

        private IContaminationInspectionResultModel<MapInspectionResult> findContaminations(AlignContext alignContext, IContaminationTeachingModel<GridMoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);
            var contaminationsResult = teachingService.SurfaceTeachingService.InspectContaminations(
                alignContext: alignContext,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            ContaminationCount += contaminationsResult.Contamination.Value;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            CurrentTeachingWindow.Instance?.Window?.Display(alignContext.PackageRegion, EColor.Green);

            return contaminationsResult;
        }

        #endregion

        #region RejectMark
        private void teachRejectMark(AlignContext alignContext, HObject packageRegion, IRejectMarkTeachingModel<GridMoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teachingService.RejectMarkTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            var rejectMarkResult = teachingService.RejectMarkTeachingService.InspectRejectMark(
                alignContext: alignContext,
                teaching: teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            if (rejectMarkResult.RejectMark.Type == EResultType.RejectMark)
            {
                RejectMarkCount++;
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            CurrentTeachingWindow.Instance?.Window?.Display(packageRegion, EColor.Green);
        }

        [RelayCommand]
        private void inspectRejectMark()
        {
            RejectMarkCount = 0;

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);
                teachRejectMark(alignContext, alignContext.PackageRegion, Teaching);
            }
        }
        #endregion

        #region Sawing
        private IChippingInspectionResultModel<MapInspectionResult> findChipping(AlignContext alignContext, ECamera camera, ISawingTeachingModel<GridMoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            alignContext.AlignedImage = TeachingImage;

            var chippingResult = teachingService.SawingTeachingService.InspectChipping(
                alignContext: alignContext,
                camera: camera,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                );

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            CurrentTeachingWindow.Instance?.Window?.Display(alignContext.PackageRegion, EColor.Green);

            return chippingResult;
        }

        private ICornerDegreeInspectionResultModel<MapInspectionResult> findCornerDegree(AlignContext alignContext, double tolerance, ISawingTeachingModel<GridMoldTeaching> teaching)
        {
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

        private ISawOffsetInspectionResultModel<MapInspectionResult> findSawOffset(AlignContext alignContext, double xTolerance, double yTolerance, ECamera camera, ISawingTeachingModel<GridMoldTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(alignContext);
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
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            ECamera camera = ECamera.Mapping;

            double cornerDegreeTolerance = GlobalSetting.Instance.Inspection.Tolerance.MapCornerDegree;
            double xTolerance = GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX;
            double yTolerance = GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetY;

            var alignContext = teachingService.GetGridAlignContext(SelectedPackageNo - 1, OriginalImages.ToList(), TeachingImage, Teaching, camera);

            teachingService.SawingTeachingService.TeachSawOffset(
                teachingImage: TeachingImage,
                alignContext: alignContext,
                camera: camera,
                teaching: Teaching,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                )
                .MergeTo(Teaching);

            var results = new List<MapInspectionResult>();

            ChippingCount = 0;

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var packageAlignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, camera);

                var result = new MapInspectionResult();

                result.CornerDegree = findCornerDegree(packageAlignContext, cornerDegreeTolerance, Teaching).CornerDegree;

                result.SawOffset = findSawOffset(packageAlignContext, xTolerance, yTolerance, camera, Teaching).SawOffset;

                result.Chipping = findChipping(packageAlignContext, camera, Teaching).Chipping;

                // Chipping Count만 합산으로 표기
                ChippingCount += result.Chipping.Value;

                results.Add(result);
            }

            ShowInspectionResultText(results);
        }
        #endregion

        #region Inspect

        [RelayCommand]
        private async void inspect()
        {
            try
            {
                var results = (await teachingService.InspectAsync(
                    images: OriginalImages.ToList(),
                    teaching: Teaching,
                    camera: ECamera.Mapping,
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

                    VisionWindow?.Display(results);

                    ShowInspectionResultText(results.Select(r => r.InspectionResult).ToList());
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

        [RelayCommand]
        private void close() => Dispose();

        [RelayCommand]
        private void saveRecipe()
        { 
            if (this.teachingPackageRegion == null)
            {
                new AlertWindow("The Package Required",
                    AlertWindow.EIcon.ALERT,
                    "The package outline must be found at the previous stage.",
                    AlertWindow.EAlert.IMAGE,
                    "/Assets/UiImages/find-mark-guide.png").ShowDialog();
                return;
            }

            SaveShotNumber();

            var originTeaching = Repository.GetRecipe();

            var diff = originTeaching.Diff(Teaching, AnyDiff.ComparisonOptions.CompareProperties | AnyDiff.ComparisonOptions.TreatEmptyListAndNullTheSame);

            HistoryService.Instance.CreateHistory("Grid Mold Teaching", diff);

            Teaching.IsTaught = true;

            Repository.SaveRecipe(Teaching);

            Dispose();
        }

        public override void SaveShotNumber()
        {
            Teaching.ShotNumberForInspection.Clear();

            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.NoDevice, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.PackageSize, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.PackageOffset, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.NoMark, Teaching.ShotNoByTabNo[MARK_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.TextAngle, Teaching.ShotNoByTabNo[MARK_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.TextOffset, Teaching.ShotNoByTabNo[MARK_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.DataCode, Teaching.ShotNoByTabNo[MARK_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.MissingChar, Teaching.ShotNoByTabNo[MARK_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.MarkCount, Teaching.ShotNoByTabNo[MARK_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.WrongMark, Teaching.ShotNoByTabNo[MARK_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.Scratch, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.ForeignMaterial, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.Contamination, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.SawOffset, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.Chipping, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.Burr, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.CornerDegree, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(MoldInspectionItem.RejectMark, Teaching.ShotNoByTabNo[REJECT_TAB_NO]);
        }
    }
}
