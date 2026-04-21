using AnyDiff.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.UiUpdaters;
using System.Collections.ObjectModel;
using System.Windows;
using Size = GVisionWpf.Models.Visions.Size;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public sealed partial class GridLgaTeachingViewModel : GridTeachingViewModelBase<GridLgaTeaching>
    {
        private ITeachingInspectionService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem> teachingService;

        private HashSet<LgaInspectionItem> InspectionItems => GlobalSetting.Instance.Inspection.LgaItems;

        private RecipeRepository<GridLgaTeaching> Repository => GridLgaRepository.Instance;

        const int PACKAGE_TAB_NO = 0;
        const int PADS_TAB_NO = 1;
        const int LEADS_TAB_NO = 2;
        const int SURFACE_TAB_NO = 3;
        const int SAWING_TAB_NO = 4;
        const int REJECT_TAB_NO = 5;

        #region Fields
        private Size packageSize;
        private CornerDegree cornerDegree;
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

        public GridLgaTeachingViewModel()
        {
            teachingService = new GridLgaTeachingInspectionService().DisposeBy(DisposeBag);

            try { Teaching = Repository.GetRecipe(); }
            catch { Teaching = new GridLgaTeaching(); }

            if (Teaching.ShotNoByTabNo.TryGetValue(0, out int shotNo)) 
                SelectedShotIndex = shotNo;
        }

        [RelayCommand]
        private void saveRecipe()
        {
            var originTeaching = Repository.GetRecipe();

            var diff = originTeaching.Diff(Teaching, AnyDiff.ComparisonOptions.CompareProperties | AnyDiff.ComparisonOptions.TreatEmptyListAndNullTheSame);

            HistoryService.Instance.CreateHistory("Grid Lga Teaching", diff);

            Teaching.IsTaught = true;

            Repository.SaveRecipe(Teaching);
        }

        #region Package
        [RelayCommand]
        private void autoThreshold()
        {
            ArgumentNullException.ThrowIfNull(teachingService.GridPackageTeachingService);
            teachingService.GridPackageTeachingService.TeachAutoThreshold(
                teachingImage: TeachingImage,
                teaching: Teaching
                )
                .MergeTo(Teaching);
        }

        [RelayCommand]
        private void findPackages()
        {
            ArgumentNullException.ThrowIfNull(teachingService.GridPackageTeachingService);

            PackageNumbers.Clear();

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
                PackageNumbers.Add(i + 1);
                sizeList.Add(results[i].PackageSize.Value);

                var packageRegion = results[i].PackageRegion;
                var packagePoint = results[i].PackagePoints;

                this.packages.Add(key: i + 1, new PackageInfo(packageRegion, packagePoint));
            }

            StatisticalList<Size> sizes = new StatisticalList<Size>(sizeList);
            PackageWidth = sizes.MemberwiseAverage().Width;
            PackageHeight = sizes.MemberwiseAverage().Height;

            packageRender.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
            VisionWindow?.Display(packageRender.FloatingTexts);

            ShowInspectionResultText(results);
        }
        #endregion

        #region FirstPin 
        private void InspectFirstPin()
        {
            ArgumentNullException.ThrowIfNull(teachingService.FirstPinTeachingService);

            var alignContext = teachingService.GetGridAlignContext(SelectedPackageNo - 1, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.PRS);

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
        private void teachMultiPad()
        {
            ArgumentNullException.ThrowIfNull(teachingService.MultiPadTeachingService);
            try
            {
                HObject packageRegion = packages[SelectedPackageNo].PackageRegion;
                ArgumentNullException.ThrowIfNull(packageRegion);

                var alignContext = teachingService.GetGridAlignContext(SelectedPackageNo - 1, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.PRS);

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

                AvgPadWidth = Teaching.MultiPadAvgSize.Width;
                AvgPadHeight = Teaching.MultiPadAvgSize.Height;

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

        [RelayCommand]
        private void inspectMultiPads()
        {
            ArgumentNullException.ThrowIfNull(teachingService.MultiPadTeachingService);

            ClearImage();

            InspectionRenderData renderData = new InspectionRenderData();

            teachMultiPad();

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                var result = teachingService.MultiPadTeachingService.InspectPads(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData leadRender
                    );

                renderData.MergeWith(leadRender);
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            VisionWindow?.Display(new FixedText("Successfully found pads", 1, EColor.Green));
        }
        #endregion

        #region Leads
        private void teachLeads()
        {
            ArgumentNullException.ThrowIfNull(teachingService.LeadTeachingService);

            try
            {
                HObject packageRegion = packages[SelectedPackageNo].PackageRegion;
                ArgumentNullException.ThrowIfNull(packageRegion);

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

                var alignContext = teachingService.GetGridAlignContext(SelectedPackageNo - 1, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.PRS);
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

        [RelayCommand]
        private void inspectLeads()
        {
            ArgumentNullException.ThrowIfNull(teachingService.LeadTeachingService);

            ClearImage();

            teachLeads();

            InspectionRenderData renderData = new InspectionRenderData();

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                var result = teachingService.LeadTeachingService.InspectLeads(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData leadRender
                    );

                renderData.MergeWith(leadRender);
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            VisionWindow?.Display(new FixedText("Successfully found pads", 1, EColor.Green));
        }
        #endregion

        #region Surface
        [RelayCommand]
        private void findSurface()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);

            ClearImage();

            try
            {
                HObject packageRegion = packages[SelectedPackageNo].PackageRegion;
                ArgumentNullException.ThrowIfNull(packageRegion);

                InspectionRenderData renderData = new InspectionRenderData();
                var alignContext = teachingService.GetGridAlignContext(SelectedPackageNo - 1, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.PRS);

                var scratchResult = teachingService.SurfaceTeachingService.InspectScratches(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData scartchRender
                    );

                renderData.MergeWith(scartchRender);
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
            catch (Exception ex)
            {
                VisionWindow?.Display(new FixedText("Inspection fail", 1, EColor.Red));
            }
        }

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

            InspectionRenderData renderData = new InspectionRenderData();

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                var scratchResult = teachingService.SurfaceTeachingService.InspectScratches(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData scratchRender
                    );

                renderData.MergeWith(scratchRender);
                if (SelectedPackageNo == index) ScratchCount = scratchResult.Scratch.Value;
             

                var foreignMaterialsResult = teachingService.SurfaceTeachingService.InspectForeignMaterials(
                  alignContext: alignContext,
                  teaching: Teaching,
                  enforceAllChecks: true,
                  inspectionItems: InspectionItems,
                  out InspectionRenderData foreignMaterialRender
                  );

                renderData.MergeWith(foreignMaterialRender);
                if (SelectedPackageNo == index) ForeignMaterialCount = foreignMaterialsResult.ForeignMaterial.Value;

                var contaminationResult = teachingService.SurfaceTeachingService.InspectContaminations(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData contaminationRender
                    );

                renderData.MergeWith(contaminationRender);
                if (SelectedPackageNo == index) ContaminationCount = contaminationResult.Contamination.Value;
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            VisionWindow?.Display(new FixedText("Inspection done", 1, EColor.Green));
        }
        #endregion

        #region Sawing
        private void findSawing()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            ClearImage();

            try
            {
                var renderData = new InspectionRenderData();
                var camera = ECamera.Mapping;
                var alignContext = teachingService.GetGridAlignContext(SelectedPackageNo - 1, OriginalImages.ToList(), TeachingImage, Teaching, camera);

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

                    CurrentTeachingWindow.Instance?.Window?.Display(new FloatingText(angle.ToString("N2"), alignContext.PackagePoints[pointIndex], EColor.Green));
                }

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

                VisionWindow?.Display(new FixedText("Inspection done", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Inspection fail", 1, EColor.Red));
            }
        }

        [RelayCommand]
        private void inspectSawing()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            ClearImage();

            InspectionRenderData renderData = new InspectionRenderData();

            var camera = ECamera.PRS;

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, camera);

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

                if (index == SelectedPackageNo)
                {
                    SawOffsetX = sawOffsetResult.SawOffset.Value?.X ?? 0;
                    SawOffsetY = sawOffsetResult.SawOffset.Value?.Y ?? 0;
                }

                var chippingResult = teachingService.SawingTeachingService.InspectChipping(
                    alignContext: alignContext,
                    camera: camera,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData chippingRender
                    );

                renderData.MergeWith(chippingRender);

                if (index == SelectedPackageNo) ChippingCount = chippingResult.Chipping.Value;

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

                if (index == SelectedPackageNo)
                {
                    CornerDegreeTopLeft = cornerDegreeResult.CornerDegree.Value.TopLeft;
                    CornerDegreeTopRight = cornerDegreeResult.CornerDegree.Value.TopRight;
                    CornerDegreeBottomLeft = cornerDegreeResult.CornerDegree.Value.BottomLeft;
                    CornerDegreeBottomRight = cornerDegreeResult.CornerDegree.Value.BottomRight;
                }


                //for (int pointIndex = 0; pointIndex < 4; pointIndex++)
                //{
                //    var angle = cornerDegreeResult.CornerDegree.Value[pointIndex];
                //    _ = pointIndex switch
                //    {
                //        0 => CornerDegreeTopLeft = angle,
                //        1 => CornerDegreeTopRight = angle,
                //        2 => CornerDegreeBottomLeft = angle,
                //        3 => CornerDegreeBottomRight = angle,
                //        _ => throw new ArgumentOutOfRangeException(nameof(pointIndex), "Invalid point index. Expected values: 0 to 3.")
                //    };

                //CurrentTeachingWindow.Instance?.Window?.Display(new FloatingText(angle.ToString("N2"), packagePoints[pointIndex], EColor.Green));
                //}
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));

            VisionWindow?.Display(new FixedText("Inspection done", 1, EColor.Green));
        }
        #endregion

        #region RejectMark
        private void teachRejectMark(AlignContext alignContext, HObject packageRegion, IRejectMarkTeachingModel<GridLgaTeaching> teaching)
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

            renderData.ResultDrawings.ForEach(elem => CurrentTeachingWindow.Instance?.Window?.Display(elem.drawingObject, elem.color));
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

                    VisionWindow?.Display(results.First().InspectionResult.Image);

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

        public override void SaveShotNumber()
        {
            Teaching.ShotNumberForInspection.Clear();

            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.NoDevice, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.PackageSize, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.PackageOffset, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.MultiPadArea, Teaching.ShotNoByTabNo[PADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.MultiPadContamination, Teaching.ShotNoByTabNo[PADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.MultiPadCount, Teaching.ShotNoByTabNo[PADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.MultiPadOffset, Teaching.ShotNoByTabNo[PADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.MultiPadPerimeter, Teaching.ShotNoByTabNo[PADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.MultiPadPitch, Teaching.ShotNoByTabNo[PADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.MultiPadSize, Teaching.ShotNoByTabNo[PADS_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.LeadArea, Teaching.ShotNoByTabNo[LEADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.LeadContamination, Teaching.ShotNoByTabNo[LEADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.LeadCount, Teaching.ShotNoByTabNo[LEADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.LeadOffset, Teaching.ShotNoByTabNo[LEADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.LeadPerimeter, Teaching.ShotNoByTabNo[LEADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.LeadPitch, Teaching.ShotNoByTabNo[LEADS_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.LeadSize, Teaching.ShotNoByTabNo[LEADS_TAB_NO]);


            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.Scratch, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.ForeignMaterial, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.Contamination, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.SawOffset, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.Chipping, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.Burr, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.CornerDegree, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(LgaInspectionItem.RejectMark, Teaching.ShotNoByTabNo[REJECT_TAB_NO]);
        }
    }
}