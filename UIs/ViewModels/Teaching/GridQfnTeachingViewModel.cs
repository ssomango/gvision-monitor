using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.DomainLayer.Extensions;
using AnyDiff.Extensions;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Services;
using GVisionWpf.Models.UiModels;
using GVisionWpf.UIs.UiUpdaters;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public partial class GridQfnTeachingViewModel : GridTeachingViewModelBase<GridQfnTeaching>
    {
        private ITeachingInspectionService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem> teachingService;

        private HashSet<QfnInspectionItem> InspectionItems => GlobalSetting.Instance.Inspection.QfnItems;

        private RecipeRepository<GridQfnTeaching> Repository => GridQfnRepository.Instance;

        const int PACKAGE_TAB_NO = 0;
        const int PAD_AND_LEAD_TAB_NO = 1;
        const int SURFACE_TAB_NO = 2;
        const int SAWING_TAB_NO = 3;
        const int REJECT_TAB_NO = 4;

        #region Fields
        private Size packageSize, padSize;
        private CornerDegree cornerDegree;
        #endregion

        #region Property
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

        public double PadWidth
        {
            get => this.padSize.Width;
            set => SetField(ref this.padSize.Width, value);
        }

        public double PadHeight
        {
            get => this.padSize.Height;
            set => SetField(ref this.padSize.Height, value);
        }

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

        public string Symbol => GlobalSetting.Instance.Inspection.LengthUnit.Symbol;

        [ObservableProperty]
        private double sawOffsetX;

        [ObservableProperty]
        private double sawOffsetY;

        [ObservableProperty]
        private double avgLeadWidth;

        [ObservableProperty]
        private double avgLeadHeight;

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
        private ObservableCollection<ESawOffsetStandardObject> sawOffsetStandardObjectSources = [ESawOffsetStandardObject.Lead, ESawOffsetStandardObject.Pad];
        #endregion

        public GridQfnTeachingViewModel()
        {
            teachingService = new GridQfnTeachingInspectionService().DisposeBy(DisposeBag);

            try { Teaching = Repository.GetRecipe(); }
            catch { Teaching = new GridQfnTeaching(); }

            if (Teaching.ShotNoByTabNo.TryGetValue(0, out int shotNo))
                SelectedShotIndex = shotNo;
        }

        [RelayCommand]
        private void saveRecipe()
        {
            var originTeaching = Repository.GetRecipe();

            var diff = originTeaching.Diff(Teaching, AnyDiff.ComparisonOptions.CompareProperties | AnyDiff.ComparisonOptions.TreatEmptyListAndNullTheSame);

            HistoryService.Instance.CreateHistory("Grid Qfn Teaching", diff);

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

        #region Pad/Leads
        private void teachPad()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SinglePadTeachingService);

            var packageRegion = packages[SelectedPackageNo].PackageRegion;

            teachingService.SinglePadTeachingService.TeachPad(
                teachingImage: TeachingImage,
                packageRegion: packageRegion,
                camera: ECamera.Mapping,
                teaching: Teaching,
                out InspectionRenderData rederData
                )
                .MergeTo(Teaching);


            Size convertedPadSize = Teaching.PadSize.ConvertFromPixel(ECamera.PRS);
            PadWidth = convertedPadSize.Width;
            PadHeight = convertedPadSize.Height;

            rederData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }

        private void teachLeads()
        {
            ArgumentNullException.ThrowIfNull(teachingService.LeadTeachingService);

            var packageRegion = packages[SelectedPackageNo].PackageRegion;

            teachingService.LeadTeachingService.TeachLeads(
                teachingImage: TeachingImage,
                packageRegion: packageRegion,
                camera: ECamera.Mapping,
                teaching: Teaching,
                enforceAllChecks: true,
                inspectionItems: InspectionItems,
                out InspectionRenderData leadRender
                )
                .MergeTo(Teaching);

            LeadCount = Teaching.LeadPxPoses.Count();

            AvgLeadWidth = Teaching.LeadAverageSize.Width;
            AvgLeadHeight = Teaching.LeadAverageSize.Height;

            var alignContext = teachingService.GetGridAlignContext(SelectedPackageNo - 1, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

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
        }

        [RelayCommand]
        private void inspectPadAndLeads()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SinglePadTeachingService);
            ArgumentNullException.ThrowIfNull(teachingService.LeadTeachingService);

            try
            {
                teachPad();
                teachLeads();

                var renderData = new InspectionRenderData();

                foreach (var (package, index) in packages.Select((package, index) => (package, index)))
                {
                    var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                    teachingService.SinglePadTeachingService.InspectPad(
                        alignContext: alignContext,
                        teaching: Teaching,
                        enforceAllChecks: true,
                        inspectionItems: InspectionItems,
                        out InspectionRenderData padRender
                        );

                    renderData.MergeWith(padRender);

                    teachingService.LeadTeachingService.InspectLeads(
                        alignContext: alignContext,
                        teaching: Teaching,
                        enforceAllChecks: true,
                        inspectionItems: InspectionItems,
                        out InspectionRenderData leadRender
                        );

                    renderData.MergeWith(leadRender);
                }

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
                VisionWindow?.Display(new FixedText("Successfully found pad and leads", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to found", 1, EColor.Red));
            }
        }
        #endregion

        #region Surface
        [RelayCommand]
        private void inspectSurface()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SurfaceTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            ClearImage();

            try
            {
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

                    var foreignMaterialsResult = teachingService.SurfaceTeachingService.InspectForeignMaterials(
                      alignContext: alignContext,
                      teaching: Teaching,
                      enforceAllChecks: true,
                      inspectionItems: InspectionItems,
                      out InspectionRenderData foreignMaterialRender
                      );

                    renderData.MergeWith(foreignMaterialRender);

                    var contaminationResult = teachingService.SurfaceTeachingService.InspectContaminations(
                        alignContext: alignContext,
                        teaching: Teaching,
                        enforceAllChecks: true,
                        inspectionItems: InspectionItems,
                        out InspectionRenderData contaminationRender
                        );

                    renderData.MergeWith(contaminationRender);

                    if (index + 1 == SelectedPackageNo)
                    {
                        ScratchCount = scratchResult.Scratch.Value;
                        ForeignMaterialCount = foreignMaterialsResult.ForeignMaterial.Value;
                        ContaminationCount = contaminationResult.Contamination.Value;
                        ShowInspectionResultText([scratchResult, contaminationResult, foreignMaterialsResult]);
                    }
                }

                renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Inspection fail", 1, EColor.Red));
            }
        }
        #endregion

        #region Sawing
        private void teachSawOffset()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);

            var camera = ECamera.Mapping;
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

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }

        [RelayCommand]
        private void inspectSawing()
        {
            ArgumentNullException.ThrowIfNull(teachingService.SawingTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            ClearImage();

            try
            {
                var renderData = new InspectionRenderData();
                var camera = ECamera.Mapping;

                teachSawOffset();

                foreach (var (package, index) in packages.Select((packages, index) => (packages, index)))
                {
                    var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, camera);

                    var cornerDegreeResult = teachingService.SawingTeachingService.InspectCornerDegree(
                        alignContext: alignContext,
                        tolerance: GlobalSetting.Instance.Inspection.Tolerance.QfnCornerDegree,
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

                    var sawOffsetResult = teachingService.SawingTeachingService.InspectSawOffset(
                       alignContext: alignContext,
                       xTolerance: GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetX,
                       yTolerance: GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetY,
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

                    VisionWindow?.Display(alignContext.PackageRegion, EColor.Green);

                    for (int pointIndex = 0; pointIndex < 4; pointIndex++)
                    {
                        var angle = cornerDegreeResult.CornerDegree.Value[pointIndex];
                        VisionWindow?.Display(new FloatingText(angle.ToString("N2"), alignContext.PackagePoints[pointIndex], EColor.Green));
                    }
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

        #region RejectMark
        [RelayCommand]
        private void inspectRejectMark()
        {
            ArgumentNullException.ThrowIfNull(teachingService.RejectMarkTeachingService);
            ArgumentNullException.ThrowIfNull(Teaching.PackageRoi);

            ClearImage();

            RejectMarkCount = 0;

            var renderData = new InspectionRenderData();

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                var rejectMarkResult = teachingService.RejectMarkTeachingService.InspectRejectMark(
                    alignContext: alignContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData rejectMarkRender
                    );


                renderData.MergeWith(rejectMarkRender);

                if (SelectedPackageNo == index + 1 && rejectMarkResult.RejectMark.Type == EResultType.RejectMark)
                {
                    RejectMarkCount += rejectMarkResult.RejectMark.Value;
                }
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
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


                System.Windows.Application.Current.Dispatcher.Invoke(() =>
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

            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.NoDevice, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.PackageSize, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.PackageOffset, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.PadArea, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.PadSize, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.LeadArea, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.LeadContamination, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.LeadCount, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.LeadOffset, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.LeadPerimeter, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.LeadPitch, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.LeadSize, Teaching.ShotNoByTabNo[PAD_AND_LEAD_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.Scratch, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.ForeignMaterial, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.Contamination, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.SawOffset, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.Chipping, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.Burr, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.CornerDegree, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(QfnInspectionItem.RejectMark, Teaching.ShotNoByTabNo[REJECT_TAB_NO]);
        }
    }
}
