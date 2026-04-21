using GVisionWpf.UIs.ViewModels.Teaching;
using System.Windows.Media.Media3D;
using System.Windows.Media.Media3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;

namespace GVisionWpf.UIs.Overlays
{
    public static class GridMoldTeachingFlow
    {
        private static readonly object Sync = new();

        private static bool _pendingStart;
        private static int _pendingTop6ShotIndex = -1;
        private static List<string> _pendingSelectedFiles = new();
        private static TutorFlowRunner? _activeRunner;
        private static Window? _activeWindow;
        private static int _runSeq;

        public static void SetTriggerBySelectedFiles(IReadOnlyList<string> selectedFiles, bool shouldStart, int top6ShotIndex)
        {
            lock (Sync)
            {
                _pendingSelectedFiles = selectedFiles.ToList();
                _pendingStart = shouldStart;
                _pendingTop6ShotIndex = top6ShotIndex;
            }

            var fileLog = string.Join(", ", selectedFiles.Select(Path.GetFileName));
            Debug.WriteLine($"[MapTeachingTutor] trigger updated. shouldStart={shouldStart}, top6ShotIndex={top6ShotIndex}, files=[{fileLog}]");
        }

        public static void StartMappingTutor(Window hostWindow, GridMoldTeachingViewModel viewModel, Guid? gateToken)
        {
            bool shouldStart;
            int top6ShotIndex;
            List<string> selectedFiles;

            lock (Sync)
            {
                shouldStart = _pendingStart;
                top6ShotIndex = _pendingTop6ShotIndex;
                selectedFiles = _pendingSelectedFiles.ToList();
            }

            if (!gateToken.HasValue)
            {
                Debug.WriteLine("[MapTeachingTutor] start skipped: no gate token on GridMoldTeachingWindow");
                return;
            }

            if (!TutorGate.TryConsume("mapping", gateToken.Value))
            {
                Debug.WriteLine($"[MapTeachingTutor] start skipped: gate token rejected token={gateToken}");
                return;
            }

            if (!shouldStart)
            {
                Debug.WriteLine("[MapTeachingTutor] start skipped: trigger=false");
                return;
            }

            StopActive("RestartBeforeStart");

            var steps = BuildPackageMvpSteps(viewModel);
            var runId = $"gridmold-flow-{hostWindow.GetHashCode()}-{Interlocked.Increment(ref _runSeq)}";
            var runner = new TutorFlowRunner(hostWindow, steps, _ => OnRunnerStopped(hostWindow), runId);

            lock (Sync)
            {
                _pendingStart = false;
                _pendingTop6ShotIndex = -1;
                _pendingSelectedFiles.Clear();
                _activeRunner = runner;
                _activeWindow = hostWindow;
            }

            var fileLog = string.Join(", ", selectedFiles.Select(Path.GetFileName));
            Debug.WriteLine($"[MapTeachingTutor] start. top6ShotIndex={top6ShotIndex}, selected=[{fileLog}], stepCount={steps.Count}");
            TutorFlowStartHub.Start(runner, runId, "GridMoldTeachingFlow.StartMappingTutor");
        }

        public static void StopForWindow(Window hostWindow, string reason)
        {
            lock (Sync)
            {
                if (_activeWindow == null || !ReferenceEquals(_activeWindow, hostWindow)) return;
            }

            StopActive(reason);
        }

        public static void StopActive(string reason)
        {
            TutorFlowRunner? runner;

            lock (Sync)
            {
                runner = _activeRunner;
                _activeRunner = null;
                _activeWindow = null;
                _pendingStart = false;
                _pendingTop6ShotIndex = -1;
                _pendingSelectedFiles.Clear();
            }

            runner?.Stop(reason);

            Debug.WriteLine($"[MapTeachingTutor] stop active. reason={reason}");
        }

        private static void OnRunnerStopped(Window hostWindow)
        {
            lock (Sync)
            {
                if (_activeWindow != null && ReferenceEquals(_activeWindow, hostWindow))
                {
                    _activeRunner = null;
                    _activeWindow = null;
                }

                _pendingStart = false;
                _pendingTop6ShotIndex = -1;
                _pendingSelectedFiles.Clear();
            }

            Debug.WriteLine("[MapTeachingTutor] runner stopped and static state cleared.");
        }

        private static List<TutorStep> BuildPackageMvpSteps(GridMoldTeachingViewModel viewModel)
        {
            var initialShotIndex = viewModel.SelectedShotIndex;
            var initialEdgeDirection = viewModel.Teaching.PackageEdgeDetectDirection;
            var initialEdgeMode = viewModel.Teaching.PackageEdgeDetectMode;

            var autoThresholdRevisionBefore = viewModel.AutoThresholdRevision;
            var findPackageRevisionBefore = viewModel.FindPackageRevision;

            var steps = new List<TutorStep>
            {
                new WaitValueStep(
                    key: "step-a-shot-select",
                    title: "Shot 선택",
                    body: "Shot #를 선택하세요.",
                    anchorAutomationId: "GRIDMOLD_PACKAGE_SHOT_COMBO",
                    isChanged: _ => viewModel.SelectedShotIndex != initialShotIndex || viewModel.SelectedShotIndex >= 0),

                new WaitClickStep(
                    key: "step-b-rotate",
                    title: "회전 설정",
                    body: "현재 이미지가 회전되어있습니다. 적절한 방향으로 각도를 선택하여 회전시켜보세요.",
                    anchorAutomationId: "GRIDMOLD_ROTATE_GROUP",
                    clickAutomationIds: ["GRIDMOLD_ROTATE_90_RADIO"],
                    consumeMatchingClick: false),

                new WaitClickStep(
                    key: "step-c1-gridroi-create",
                    title: "Grid ROI 생성",
                    body: "오른쪽 패널에서 CREATE 버튼을 눌러 Grid ROI 틀을 생성하세요.",
                    anchorAutomationId: "GRIDMOLD_PACKAGE_GRID_ROI",
                    clickAutomationIds: [ "GRIDROI_CREATE_BTN" ],
                    consumeMatchingClick: false),

                // ✅ c2 삭제. 바로 안내만 하고 다음(Enter/Space)으로 진행
                new InfoStep(
                    key: "step-c3-gridroi-adjust",
                    title: "ROI 위치/크기 맞추기",
                    body:
                        "왼쪽 화면에서 ROI를 드래그하거나 Tracker로 예시처럼 ROI를 맞춰주세요.\n" +
                        "ROI를 다 맞췄으면 Enter(또는 Space)로 다음 단계로 진행하세요.\n" +
                        "마우스 휠: 줌인/줌아웃\n" +
                        "*ROI 선이 자재 영역을 침범하지 않도록 주의하세요.*",
                    anchorAutomationId: "GRIDMOLD_VISION_WINDOW",
                    cardAnchorAutomationId: "GRIDMOLD_VISION_WINDOW",
                    cardImageUri: "pack://application:,,,/Assets/Tutor/package_gridroi_example.png"),

                // ✅ ROI 조정 후, Edge Detection 먼저
                new WaitValueStep(
                    key: "step-d-edge-dir-type",
                    title: "Direction/Type 변경",
                    body:
                        "자재 가장자리를 찾기 위해 방향과 색상을 선택합니다.\n" +
                        "보통 자재 내부에서 바깥을 볼 때(In → Out),\n"+
                        "밝은색 → 어두운색 (White → Black)을 많이 사용합니다.",
                    anchorAutomationId: "GRIDMOLD_EDGE_SETTING_GROUP",
                    isChanged: _ =>
                        viewModel.Teaching.PackageEdgeDetectDirection != initialEdgeDirection ||
                        viewModel.Teaching.PackageEdgeDetectMode != initialEdgeMode),

                new ActionStep(
                    key: "step-f0-snapshot-autothreshold-revision",
                    title: "",
                    body: "",
                    anchorAutomationId: "GRIDMOLD_AUTO_THRESHOLD_BTN",
                    action: (w, ct) =>
                    {
                        autoThresholdRevisionBefore = viewModel.AutoThresholdRevision;
                        return Task.CompletedTask;
                    }),

                new WaitClickStep(
                    key: "step-f1-auto-threshold-click",
                    title: "Auto Threshold",
                    body: "Auto Threshold를 눌러 값을 자동으로 맞춰주세요.",
                    anchorAutomationId: "GRIDMOLD_AUTO_THRESHOLD_BTN",
                    clickAutomationIds: ["GRIDMOLD_AUTO_THRESHOLD_BTN"],
                    consumeMatchingClick: false),

                new WaitConditionStep(
                    key: "step-f2-auto-threshold-wait",
                    title: "Auto Threshold 적용 확인",
                    body: "자동 계산이 끝날 때까지 잠시만 기다려주세요.",
                    anchorAutomationId: "GRIDMOLD_AUTO_THRESHOLD_BTN",
                    condition: _ => viewModel.AutoThresholdRevision > autoThresholdRevisionBefore,
                    pollIntervalMs: 50),

                new InfoStep(
                    key: "step-f3-thresholddiff-tip",
                    title: "ThresholdDiff 미세 조정",
                    body: "Auto Threshold 결과를 기준으로 **±3 정도 여유**를 두고 조정하는 걸 추천해요.",
                    anchorAutomationId: "GRIDMOLD_THRESHOLD_DIFF_TEXTBOX"),

                new ActionStep(
                    key: "step-g0-find-package-snapshot",
                    title: "",
                    body: "",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    action: (_, _) =>
                    {
                        findPackageRevisionBefore = viewModel.FindPackageRevision;
                        return Task.CompletedTask;
                    }),

                new WaitClickStep(
                    key: "step-g-find-package",
                    title: "Find Package 실행",
                    body: "Find Package를 클릭하여 패키지를 찾아줘요.",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    clickAutomationIds: ["GRIDMOLD_FIND_PACKAGE_BTN"],
                    consumeMatchingClick: false),

                new WaitConditionStep(
                    key: "step-g1-find-package-wait",
                    title: "Find Package 결과 확인",
                    body: "Find Package 결과가 반영될 때까지 잠시만 기다려주세요.",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    condition: _ => viewModel.FindPackageRevision > findPackageRevisionBefore,
                    pollIntervalMs: 50),

                new BranchStep(
                    key: "step-g2-result-check",
                    title: "",
                    body: "",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    branch: (_, _) => Task.FromResult(GetPackageResultStep(viewModel, successStepKey: "step-h-select-package", retryStepKey: "retry-r0-gridroi-delete"))),

                new InfoStep(
                    key: "step-h-select-package",
                    title: "티칭 과정에서 사용할 자재 선택",
                    body:
                        "티칭을 진행할 자재(패키지) 번호를 선택하세요",
                    anchorAutomationId: "GRIDMOLD_PACKAGE_NUMBER",
                    cardAnchorAutomationId: "GRIDMOLD_PACKAGE_NUMBER",
                    cardImageUri: "pack://application:,,,/Assets/Tutor/package_select_package_num.png")
            };

            steps.AddRange(BuildRetryPackageSteps(viewModel, () => autoThresholdRevisionBefore, value => autoThresholdRevisionBefore = value, () => findPackageRevisionBefore, value => findPackageRevisionBefore = value));

            return steps;
        }

        private static IEnumerable<TutorStep> BuildRetryPackageSteps(
            GridMoldTeachingViewModel viewModel,
            Func<int> getAutoThresholdRevisionBefore,
            Action<int> setAutoThresholdRevisionBefore,
            Func<int> getFindPackageRevisionBefore,
            Action<int> setFindPackageRevisionBefore)
        {
            var retryEdgeDirectionSnapshot = viewModel.Teaching.PackageEdgeDetectDirection;
            var retryEdgeModeSnapshot = viewModel.Teaching.PackageEdgeDetectMode;

            return
            [
                new WaitClickStep(
                    key: "retry-r0-gridroi-delete",
                    title: "ROI 되돌리기",
                    body: "기존 ROI를 되돌리기 위해 undo 버튼을 클릭하세요.",
                    anchorAutomationId: "GRIDROI_DELETE_BTN",
                    clickAutomationIds: ["GRIDROI_DELETE_BTN"],
                    consumeMatchingClick: false),

                new WaitClickStep(
                    key: "retry-r1-gridroi-create",
                    title: "Grid ROI 다시 생성",
                    body: "CREATE 버튼을 눌러 Grid ROI를 다시 생성하세요.",
                    anchorAutomationId: "GRIDMOLD_PACKAGE_GRID_ROI",
                    clickAutomationIds: ["GRIDROI_CREATE_BTN"],
                    consumeMatchingClick: false),

                new InfoStep(
                    key: "retry-r2-gridroi-adjust",
                    title: "ROI 다시 맞추기",
                    body:
                        "왼쪽 화면에서 ROI를 다시 조정해주세요.\n" +
                        "ROI를 다 맞췄으면 Enter(또는 Space)로 다음 단계로 진행하세요.\n" +
                        "마우스 휠: 줌인/줌아웃\n" +
                        "*ROI 선이 자재 영역을 침범하지 않도록 주의하세요.*",
                    anchorAutomationId: "GRIDMOLD_VISION_WINDOW",
                    cardAnchorAutomationId: "GRIDMOLD_VISION_WINDOW",
                    cardImageUri: "pack://application:,,,/Assets/Tutor/package_gridroi_example.png"),

                new ActionStep(
                    key: "retry-r3-edge-snapshot",
                    title: "",
                    body: "",
                    anchorAutomationId: "GRIDMOLD_EDGE_SETTING_GROUP",
                    action: (_, _) =>
                    {
                        retryEdgeDirectionSnapshot = viewModel.Teaching.PackageEdgeDetectDirection;
                        retryEdgeModeSnapshot = viewModel.Teaching.PackageEdgeDetectMode;
                        return Task.CompletedTask;
                    }),

                new WaitValueStep(
                    key: "retry-r3-edge-dir-type",
                    title: "Edge Detection 다시 설정",
                    body:
                        "Direction/Type을 다시 설정해 주세요.\n" +
                        "이전 값에서 방향 또는 타입 중 하나 이상 변경되면 다음으로 진행합니다.",
                    anchorAutomationId: "GRIDMOLD_EDGE_SETTING_GROUP",
                    isChanged: _ =>
                        viewModel.Teaching.PackageEdgeDetectDirection != retryEdgeDirectionSnapshot ||
                        viewModel.Teaching.PackageEdgeDetectMode != retryEdgeModeSnapshot),

                new ActionStep(
                    key: "retry-r4-autothreshold-snapshot",
                    title: "",
                    body: "",
                    anchorAutomationId: "GRIDMOLD_AUTO_THRESHOLD_BTN",
                    action: (_, _) =>
                    {
                        setAutoThresholdRevisionBefore(viewModel.AutoThresholdRevision);
                        return Task.CompletedTask;
                    }),

                new WaitClickStep(
                    key: "retry-r5-autothreshold-click",
                    title: "Auto Threshold 다시 실행",
                    body: "Auto Threshold를 다시 눌러 값을 계산하세요.",
                    anchorAutomationId: "GRIDMOLD_AUTO_THRESHOLD_BTN",
                    clickAutomationIds: ["GRIDMOLD_AUTO_THRESHOLD_BTN"],
                    consumeMatchingClick: false),

                new WaitConditionStep(
                    key: "retry-r6-autothreshold-wait",
                    title: "Auto Threshold 적용 확인",
                    body: "자동 계산이 끝날 때까지 잠시만 기다려주세요.",
                    anchorAutomationId: "GRIDMOLD_AUTO_THRESHOLD_BTN",
                    condition: _ => viewModel.AutoThresholdRevision > getAutoThresholdRevisionBefore(),
                    pollIntervalMs: 50),

                new InfoStep(
                    key: "retry-r7-thresholddiff-adjust",
                    title: "ThresholdDiff 조정",
                    body:
                        "Auto Threshold 결과를 기준으로\n" +
                        "Amplitude 값을 약 +3 정도 여유 있게 조정해보세요.\n\n" +
                        "조정 후 Find Package를 다시 실행하여 패키지를 잘 검출하는지 확인합니다.",
                    anchorAutomationId: "GRIDMOLD_THRESHOLD_DIFF_TEXTBOX"),

                new ActionStep(
                    key: "retry-r8-find-package-snapshot",
                    title: "",
                    body: "",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    action: (_, _) =>
                    {
                        setFindPackageRevisionBefore(viewModel.FindPackageRevision);
                        return Task.CompletedTask;
                    }),

                new WaitClickStep(
                    key: "retry-r8-find-package",
                    title: "Find Package 다시 실행",
                    body: "Find Package를 다시 클릭하여 결과를 확인하세요.",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    clickAutomationIds: ["GRIDMOLD_FIND_PACKAGE_BTN"],
                    consumeMatchingClick: false),

                new WaitConditionStep(
                    key: "retry-r9-find-package-wait",
                    title: "Find Package 결과 확인",
                    body: "Find Package 결과가 반영될 때까지 잠시만 기다려주세요.",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    condition: _ => viewModel.FindPackageRevision > getFindPackageRevisionBefore(),
                    pollIntervalMs: 50),

                new BranchStep(
                    key: "retry-r9-result-check",
                    title: "",
                    body: "",
                    anchorAutomationId: "GRIDMOLD_FIND_PACKAGE_BTN",
                    branch: (_, _) => Task.FromResult(GetPackageResultStep(viewModel, successStepKey: "step-h-select-package", retryStepKey: "retry-r0-gridroi-delete")))
            ];
        }

        private static StepExecutionResult GetPackageResultStep(GridMoldTeachingViewModel viewModel, string successStepKey, string retryStepKey)
        {
            return viewModel.LastFindPackageGoodCount == 4
                ? StepExecutionResult.JumpTo(successStepKey)
                : StepExecutionResult.JumpTo(retryStepKey);
        }

        private static void ApplyThresholdDiffOffset(Window hostWindow, GridMoldTeachingViewModel viewModel, int offset)
        {
            var thresholdTextBox = FindByAutomationId<TextBox>(hostWindow, "GRIDMOLD_THRESHOLD_DIFF_TEXTBOX");
            var newThreshold = Math.Clamp(viewModel.Teaching.PackageThresholdDiff + offset, 1, 255);

            viewModel.Teaching.PackageThresholdDiff = newThreshold;

            if (thresholdTextBox == null)
            {
                return;
            }

            thresholdTextBox.Text = newThreshold.ToString();
            var binding = BindingOperations.GetBindingExpression(thresholdTextBox, TextBox.TextProperty);
            binding?.UpdateSource();
        }

        private static T? FindByAutomationId<T>(DependencyObject root, string automationId)
            where T : FrameworkElement
        {
            if (root == null || string.IsNullOrWhiteSpace(automationId))
            {
                return null;
            }

            var stack = new Stack<DependencyObject>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is T element &&
                    string.Equals(AutomationProperties.GetAutomationId(element), automationId, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }

                if (current is Visual || current is Visual3D)
                {
                    var childCount = VisualTreeHelper.GetChildrenCount(current);
                    for (var i = 0; i < childCount; i++)
                    {
                        stack.Push(VisualTreeHelper.GetChild(current, i));
                    }
                }
            }

            return null;
        }


        private static TabControl? FindTabControl(DependencyObject root)
        {
            if (root == null) return null;

            if (root is TabControl tc)
            {
                var id = AutomationProperties.GetAutomationId(tc);
                if (string.Equals(id, "GRIDMOLD_TAB", StringComparison.OrdinalIgnoreCase))
                    return tc;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var found = FindTabControl(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
