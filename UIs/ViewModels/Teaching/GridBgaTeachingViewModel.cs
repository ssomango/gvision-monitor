using AnyDiff.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.UiUpdaters;
using System.Collections.ObjectModel;
using System.Reflection;



namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public partial class GridBgaTeachingViewModel : GridTeachingViewModelBase<GridBgaTeaching>
    {
        // 싱글톤 인스턴스 구현 (쓰기 안전)
        private static readonly Lazy<GridBgaTeachingViewModel> _instance =
            new Lazy<GridBgaTeachingViewModel>(() => new GridBgaTeachingViewModel());
        public static GridBgaTeachingViewModel Instance => _instance.Value;

        private ITeachingInspectionService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem> teachingService;

        private HashSet<BgaInspectionItem> InspectionItems => GlobalSetting.Instance.Inspection.BgaItems;

        private RecipeRepository<GridBgaTeaching> Repository => GridBgaRepository.Instance;


        const int PACKAGE_TAB_NO = 0;
        const int FIRSTPIN_AND_PATTERN_TAB_NO = 1;
        const int BALL_TAB_NO = 2;
        const int SURFACE_TAB_NO = 3;
        const int SAWING_TAB_NO = 4;
        const int REJECT_TAB_NO = 5;

        #region Fields
        private Size packageSize;
        private CornerDegree cornerDegree;
        #endregion

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

        #endregion

        public GridBgaTeachingViewModel()
        {
            teachingService = new GridBgaTeachingInspectionService().DisposeBy(DisposeBag);

            try { Teaching = Repository.GetRecipe(); }
            catch { Teaching = new GridBgaTeaching(); }

            if (Teaching.ShotNoByTabNo.TryGetValue(0, out int shotNo))
                SelectedShotIndex = shotNo;
        }

        [RelayCommand]
        private void saveRecipe()
        {
            var originTeaching = Repository.GetRecipe();

            var diff = originTeaching.Diff(Teaching, AnyDiff.ComparisonOptions.CompareProperties | AnyDiff.ComparisonOptions.TreatEmptyListAndNullTheSame);

            HistoryService.Instance.CreateHistory("Grid Bga Teaching", diff);

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

        #region Patterns / FirstPin
        private void findPatterns()
        {
            ArgumentNullException.ThrowIfNull(teachingService.PatternTeachingService);

            try
            {
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
        private void inspectFirstPinAndPattern()
        {
            ClearImage();

            ArgumentNullException.ThrowIfNull(teachingService.PatternTeachingService);
            ArgumentNullException.ThrowIfNull(teachingService.FirstPinTeachingService);

            findPatterns();
            findFirstPin();

            var renderData = new InspectionRenderData();

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                var alginContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                teachingService.FirstPinTeachingService.InspectFirstPin(
                    alignContext: alginContext,
                    teaching: Teaching,
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData firstPinRender
                    );

                renderData.MergeWith(firstPinRender);

                teachingService.PatternTeachingService.InspectPatterns(
                    alignContext: alginContext,
                    teaching: Teaching,
                    enforceAllChecks: true, 
                    inspectionItems: InspectionItems,
                    out InspectionRenderData patternRender);

                renderData.MergeWith(patternRender);
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }
        #endregion

        #region Ball
        [RelayCommand]
        private void teachBalls()
        {
            ArgumentNullException.ThrowIfNull(teachingService.BallTeachingService);

            teachingService.BallTeachingService.TeachBalls(
                teachingImage: TeachingImage,
                teaching: Teaching,
                inspectionItems: InspectionItems,
                out InspectionRenderData renderData
                )
                .MergeTo(Teaching);

            BallCount = Teaching.Balls.Count;

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }

        [RelayCommand]
        private void inspectBalls()
        {
            ArgumentNullException.ThrowIfNull(teachingService.BallTeachingService);

            teachBalls();

            InspectionRenderData renderData = new InspectionRenderData();

            foreach (var (package, index) in packages.Select((package, index) => (package, index)))
            {
                ClearImage();

                var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, ECamera.Mapping);

                var result = teachingService.BallTeachingService.InspectBalls(
                    alignContext: alignContext,
                    teaching: Teaching, 
                    enforceAllChecks: true,
                    inspectionItems: InspectionItems,
                    out InspectionRenderData ballRender
                    );

                var ballCount = result.BallCount.Value;

                renderData.MergeWith(ballRender);
            }

            renderData.ResultDrawings.ForEach(elem => VisionWindow?.Display(elem.drawingObject, elem.color));
        }

        [RelayCommand]
        private void findBallRoiAuto()
        {
            ArgumentNullException.ThrowIfNull(teachingService.BallTeachingService);

            try
            {
                var selectedPackageRegion = packages[SelectedPackageNo].PackageRegion;
                var selectedPackageImage = TeachingImage.ReduceDomain(selectedPackageRegion);

                teachingService.BallTeachingService.FindBallAutoRoi(selectedPackageImage, Teaching)
                    .MergeTo(Teaching);

                VisionWindow?.Display(new FixedText("Successfully found the ball ROI", 1, EColor.Green));
            }
            catch
            {
                VisionWindow?.Display(new FixedText("Failed to ball auto ROI", 1, EColor.Red));
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
                        out InspectionRenderData scartchRender
                        );

                    renderData.MergeWith(scartchRender);

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

                foreach(var (package, index) in packages.Select((packages, index) => (packages, index)))
                {
                    var alignContext = teachingService.GetGridAlignContext(index, OriginalImages.ToList(), TeachingImage, Teaching, camera);

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
                    var sawOffsetResult = teachingService.SawingTeachingService.InspectSawOffset(
                       alignContext: alignContext,
                       xTolerance: GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetX,
                       yTolerance: GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetX,
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
                var results = (await ((GridBgaTeachingInspectionService)teachingService).InspectAsync(
                      images: OriginalImages.ToList(),
                    teaching: Teaching,
                    camera: ECamera.Mapping,
                    inspectionItems: InspectionItems
                    )).ToList();
                    

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

            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.NoDevice, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.PackageSize, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.PackageOffset, Teaching.ShotNoByTabNo[PACKAGE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.FirstPin, Teaching.ShotNoByTabNo[FIRSTPIN_AND_PATTERN_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.Pattern, Teaching.ShotNoByTabNo[FIRSTPIN_AND_PATTERN_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.BallBridging, Teaching.ShotNoByTabNo[BALL_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.BallCount, Teaching.ShotNoByTabNo[BALL_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.BallPitch, Teaching.ShotNoByTabNo[BALL_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.BallPosition, Teaching.ShotNoByTabNo[BALL_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.BallSize, Teaching.ShotNoByTabNo[BALL_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.CrackBall, Teaching.ShotNoByTabNo[BALL_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.ExtraBall, Teaching.ShotNoByTabNo[BALL_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.MissingBall, Teaching.ShotNoByTabNo[BALL_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.Scratch, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.ForeignMaterial, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.Contamination, Teaching.ShotNoByTabNo[SURFACE_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.SawOffset, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.Chipping, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.Burr, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);
            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.CornerDegree, Teaching.ShotNoByTabNo[SAWING_TAB_NO]);

            Teaching.ShotNumberForInspection.Add(BgaInspectionItem.RejectMark, Teaching.ShotNoByTabNo[REJECT_TAB_NO]);
        }

        public void UpdateProperty(string propertyName, string value)
        {
            Console.WriteLine($"[UpdateProperty] 시작: {propertyName} = {value}");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Console.WriteLine($"[UpdateProperty] Dispatcher 진입");

                    Type type = this.GetType();
                    Console.WriteLine($"[UpdateProperty] Type: {type.Name}");

                    PropertyInfo? prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                    if (prop == null)
                    {
                        Console.WriteLine($"[UpdateProperty] ERROR: 프로퍼티 '{propertyName}'를 찾을 수 없습니다.");

                        // 사용 가능한 모든 속성 출력
                        var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Select(p => p.Name);
                        Console.WriteLine($"[UpdateProperty] 사용 가능한 속성들: {string.Join(", ", allProps)}");

                        throw new ArgumentException($"프로퍼티 '{propertyName}'를 찾을 수 없습니다.");
                    }

                    Console.WriteLine($"[UpdateProperty] 프로퍼티 찾음: {prop.Name}, 타입: {prop.PropertyType}, CanWrite: {prop.CanWrite}");

                    if (!prop.CanWrite)
                    {
                        Console.WriteLine($"[UpdateProperty] ERROR: 읽기 전용 속성");
                        throw new InvalidOperationException($"프로퍼티 '{propertyName}'는 읽기 전용입니다.");
                    }

                    // 변경 전 값 확인
                    var oldValue = prop.GetValue(this);
                    Console.WriteLine($"[UpdateProperty] 변경 전 값: {oldValue}");

                    // 타입 변환
                    object convertedValue;
                    if (prop.PropertyType.IsEnum)
                    {
                        convertedValue = Enum.Parse(prop.PropertyType, value, true);
                    }
                    else if (prop.PropertyType == typeof(bool))
                    {
                        convertedValue = bool.Parse(value);
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        convertedValue = int.Parse(value);
                    }
                    else if (prop.PropertyType == typeof(double))
                    {
                        convertedValue = double.Parse(value);
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        convertedValue = value;
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(value, prop.PropertyType);
                    }

                    Console.WriteLine($"[UpdateProperty] 변환된 값: {convertedValue} (타입: {convertedValue.GetType()})");

                    // 값 설정
                    prop.SetValue(this, convertedValue);

                    // 변경 후 값 확인
                    var newValue = prop.GetValue(this);
                    Console.WriteLine($"[UpdateProperty] 변경 후 값: {newValue}");
                    Console.WriteLine($"[UpdateProperty] 성공!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UpdateProperty] 예외 발생: {ex.Message}");
                    Console.WriteLine($"[UpdateProperty] StackTrace: {ex.StackTrace}");
                    throw;
                }
            });
        }

    }
}
