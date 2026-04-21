using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GVisionWpf.UIs.Overlays
{
    public static class TutorWalkthroughService
    {
        private static readonly object SyncRoot = new();
        private static CancellationTokenSource? _activeWalkthroughCts;
        private static int _runInProgress;
        private static string? _lastShownAnchorKey;
        private static long _lastShownTicksUtc;
        private static int _runSequence;

        public static void StopActiveWalkthrough()
        {
            Debug.WriteLine("[TutorWalkthrough] StopActiveWalkthrough requested.");
            CancellationTokenSource? toCancel = null;

            lock (SyncRoot)
            {
                if (_activeWalkthroughCts == null) return;
                toCancel = _activeWalkthroughCts;
                _activeWalkthroughCts = null;
            }

            try
            {
                toCancel.Cancel();
            }
            catch { /* ignore */ }
            finally
            {
                toCancel.Dispose();
            }

            TutorSpotlightOverlay.Hide("StopActiveWalkthrough");
            Debug.WriteLine("[TutorWalkthrough] Overlay hidden and cancellation propagated.");
        }

        public static async Task RunAsync(
            Window hostWindow,
            IEnumerable<TutorSpotlightStep> steps,
            string? requestId = null,
            CancellationToken cancellationToken = default)
        {
            if (hostWindow == null) throw new ArgumentNullException(nameof(hostWindow));

            var stepList = steps?.ToList() ?? new List<TutorSpotlightStep>();
            if (stepList.Count == 0) return;
            var runId = Interlocked.Increment(ref _runSequence);
            var rid = requestId ?? $"walk-{runId}";

            if (Interlocked.CompareExchange(ref _runInProgress, 1, 0) != 0)
            {
                Debug.WriteLine($"[TutorWalkthrough] DUPLICATE_START_IGNORED rid={rid} t={DateTime.Now:HH:mm:ss.fff}");
                return;
            }

            var hasActiveCts = false;
            lock (SyncRoot)
            {
                hasActiveCts = _activeWalkthroughCts != null;
            }
            Debug.WriteLine($"[TutorWalkthrough] RUN_START rid={rid} stepCount={stepList.Count} activeOverlayId={TutorSpotlightOverlay.ActiveOverlayId} hasActiveCts={hasActiveCts} t={DateTime.Now:HH:mm:ss.fff}\n{Environment.StackTrace}");

            CancellationTokenSource linkedCts;

            lock (SyncRoot)
            {
                _activeWalkthroughCts?.Cancel();
                _activeWalkthroughCts?.Dispose();

                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _activeWalkthroughCts = linkedCts;
            }

            void OnPreviewKeyDown(object? sender, KeyEventArgs e)
            {
                if (e.Key != Key.Escape) return;
                e.Handled = true;
                Debug.WriteLine("[TutorWalkthrough] ESC pressed -> cancel walkthrough.");
                StopActiveWalkthrough();
            }

            try
            {
                var escHookedWindows = new HashSet<Window>();
                void EnsureEscHandlers()
                {
                    foreach (var window in Application.Current.Windows.OfType<Window>())
                    {
                        if (!escHookedWindows.Add(window)) continue;
                        window.PreviewKeyDown += OnPreviewKeyDown;
                    }
                }
                EnsureEscHandlers();

                string? activeSidebarGroup = null;
                long? gateReleasedAtTick = null;
                string? gateReleasedAnchor = null;

                for (var stepIndex = 0; stepIndex < stepList.Count; stepIndex++)
                {
                    var step = stepList[stepIndex];
                    EnsureEscHandlers();
                    linkedCts.Token.ThrowIfCancellationRequested();

                    var currentSidebarGroup = GetSidebarGroupByAnchor(step.AnchorKey);
                    if (!string.IsNullOrWhiteSpace(activeSidebarGroup) &&
                        !string.Equals(activeSidebarGroup, currentSidebarGroup, StringComparison.OrdinalIgnoreCase))
                    {
                        await CollapseSidebarGroupAsync(hostWindow, activeSidebarGroup!, currentSidebarGroup, linkedCts.Token);
                        activeSidebarGroup = null;
                    }

                    var isGateStep = step.IsGateStep;
                    var skipStep = false;
                    Window? resolvedSpotlightHostWindow = null;
                    FrameworkElement? resolvedTarget = null;
                    Debug.WriteLine($"[TutorWalkthrough] STEP_START rid={rid} anchor={step.AnchorKey} gate={isGateStep} t={DateTime.Now:HH:mm:ss.fff}");

                    await hostWindow.Dispatcher.InvokeAsync(() =>
                    {
                        hostWindow.UpdateLayout();

                        if (!TryResolveStepTarget(hostWindow, step.AnchorKey, out var spotlightHostWindow, out var target))
                        {
                            Debug.WriteLine($"[TutorWalkthrough] target NOT found: {step.AnchorKey}");
                            skipStep = true;
                            return;
                        }

                        resolvedSpotlightHostWindow = spotlightHostWindow;
                        resolvedTarget = target;
                        resolvedSpotlightHostWindow.UpdateLayout();

                        Debug.WriteLine($"[TutorWalkthrough] TARGET_FOUND rid={rid} anchor={step.AnchorKey} type={target.GetType().Name} host={spotlightHostWindow.GetType().Name} t={DateTime.Now:HH:mm:ss.fff}");

                        if (IsDuplicateStepShow(step.AnchorKey))
                        {
                            Debug.WriteLine($"[TutorWalkthrough] STEP_SHOW_DEDUPED rid={rid} stepIndex={stepIndex} anchor={step.AnchorKey} t={DateTime.Now:HH:mm:ss.fff}");
                            skipStep = true;
                            return;
                        }
                    }, DispatcherPriority.Render);

                    if (skipStep)
                    {
                        Debug.WriteLine($"[TutorWalkthrough] step skipped: {step.AnchorKey}");
                        continue;
                    }

                    if (gateReleasedAtTick.HasValue && resolvedSpotlightHostWindow != null && resolvedTarget != null)
                    {
                        var overlayHost = Application.Current?.MainWindow as Window ?? hostWindow;

                        var stable = await WaitForStableRectAfterGateAsync(
                            overlayHost,
                            resolvedTarget,
                            timeoutMs: 200,
                            linkedCts.Token);
                        Debug.WriteLine($"[TutorWalkthrough] NEXT_TARGET_STABLE rid={rid} anchor={step.AnchorKey} stable={stable} timeoutMs=200 t={DateTime.Now:HH:mm:ss.fff}");
                    }

                    await hostWindow.Dispatcher.InvokeAsync(() =>
                    {
                        if (resolvedSpotlightHostWindow == null || resolvedTarget == null)
                        {
                            skipStep = true;
                            return;
                        }

                        var overlayHost = Application.Current?.MainWindow as Window ?? hostWindow;
                        Debug.WriteLine($"[TutorWalkthrough] SHOW_REQUEST rid={rid} stepIndex={stepIndex} anchor={step.AnchorKey} t={DateTime.Now:HH:mm:ss.fff}");
                        TutorSpotlightOverlay.Show(
                            //hostWindow: resolvedSpotlightHostWindow,
                            hostWindow: overlayHost,
                            target: resolvedTarget,
                            title: step.Title,
                            body: step.Body,
                            durationMs: 0,          // 자동 닫기 X
                            padding: 10,
                            dimOpacity: 0.62,
                            manualAdvance: true,
                            allowManualAdvanceInput: !isGateStep,
                            overlayHitTestVisible: !isGateStep,
                            debugContext: new TutorSpotlightOverlay.DebugContext(rid, stepIndex, step.AnchorKey, "Walkthrough"));
                        Debug.WriteLine($"[TutorWalkthrough] SHOW_DONE rid={rid} stepIndex={stepIndex} anchor={step.AnchorKey} t={DateTime.Now:HH:mm:ss.fff}");

                        if (gateReleasedAtTick.HasValue)
                        {
                            var elapsedMs = (Stopwatch.GetTimestamp() - gateReleasedAtTick.Value) * 1000.0 / Stopwatch.Frequency;
                            Debug.WriteLine($"[TutorWalkthrough] NEXT_SHOW_LATENCY rid={rid} fromGate={gateReleasedAnchor ?? "-"} to={step.AnchorKey} elapsedMs={elapsedMs:F1} target=<=200");
                            gateReleasedAtTick = null;
                            gateReleasedAnchor = null;
                        }
                    }, DispatcherPriority.Render);

                    if (skipStep)
                    {
                        Debug.WriteLine($"[TutorWalkthrough] step skipped: {step.AnchorKey}");
                        continue;
                    }

                    await WaitForStepCompletionAsync(hostWindow, step, linkedCts.Token, () =>
                    {
                        gateReleasedAtTick = Stopwatch.GetTimestamp();
                        gateReleasedAnchor = step.AnchorKey;
                        Debug.WriteLine($"[TutorWalkthrough] GATE_RELEASED rid={rid} anchor={step.AnchorKey} t={DateTime.Now:HH:mm:ss.fff}");
                    });
                    Debug.WriteLine($"[TutorWalkthrough] STEP_COMPLETE rid={rid} anchor={step.AnchorKey} t={DateTime.Now:HH:mm:ss.fff}");
                    if (!string.IsNullOrWhiteSpace(currentSidebarGroup))
                        activeSidebarGroup = currentSidebarGroup;

                    if (step.GapAfterMs > 0)
                    {
                        await Task.Delay(step.GapAfterMs, linkedCts.Token);
                    }
                }

                if (!string.IsNullOrWhiteSpace(activeSidebarGroup))
                {
                    await CollapseSidebarGroupAsync(hostWindow, activeSidebarGroup!, nextSidebarGroup: null, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[TutorWalkthrough] RUN_CANCELED rid={rid} t={DateTime.Now:HH:mm:ss.fff}");
            }
            finally
            {
                foreach (var window in Application.Current.Windows.OfType<Window>())
                    window.PreviewKeyDown -= OnPreviewKeyDown;
                TutorSpotlightOverlay.Hide("RunFinally");

                lock (SyncRoot)
                {
                    linkedCts.Dispose();
                    if (ReferenceEquals(_activeWalkthroughCts, linkedCts))
                        _activeWalkthroughCts = null;
                }
                Interlocked.Exchange(ref _runInProgress, 0);
                Debug.WriteLine($"[TutorWalkthrough] RUN_FINISH rid={rid} t={DateTime.Now:HH:mm:ss.fff}");
            }
        }

        private static bool IsDuplicateStepShow(string anchorKey, int dedupeMs = 350)
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var windowTicks = TimeSpan.FromMilliseconds(dedupeMs).Ticks;

            lock (SyncRoot)
            {
                if (!string.IsNullOrWhiteSpace(_lastShownAnchorKey) &&
                    _lastShownAnchorKey.Equals(anchorKey, StringComparison.OrdinalIgnoreCase) &&
                    (nowTicks - _lastShownTicksUtc) <= windowTicks)
                {
                    return true;
                }

                _lastShownAnchorKey = anchorKey;
                _lastShownTicksUtc = nowTicks;
                return false;
            }
        }

        private static Task WaitForStepCompletionAsync(Window hostWindow, TutorSpotlightStep step, CancellationToken token, Action? onGateReleased = null)
        {
            var tcs = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var isGateStep = step.IsGateStep;
            var isSidebarExpanderGate = TryGetSidebarHeader(step.AnchorKey, out _);
            EventHandler? onLayoutUpdated = null;
            Expander? observedExpander = null;
            RoutedEventHandler? onExpanded = null;
            RoutedEventHandler? onOperationClick = null;
            Button? operationButton = null;
            var gateCompletionQueued = false;

            void CompleteGateAfterRender(string reason)
            {
                if (!isGateStep || tcs.Task.IsCompleted || gateCompletionQueued) return;
                gateCompletionQueued = true;

                hostWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    gateCompletionQueued = false;
                    if (tcs.Task.IsCompleted) return;
                    Debug.WriteLine($"[TutorWalkthrough] GATE_RELEASE_SCHEDULED anchor={step.AnchorKey} reason={reason} t={DateTime.Now:HH:mm:ss.fff}");
                    onGateReleased?.Invoke();
                    tcs.TrySetResult(null);
                }), DispatcherPriority.Render);
            }

            void TryCompleteByCondition()
            {
                if (!step.IsGateStep || step.ContinueCondition == null) return;

                var satisfied = false;
                try
                {
                    satisfied = step.ContinueCondition(hostWindow);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tutor] condition evaluation failed ({step.AnchorKey}): {ex}");
                }

                if (satisfied)
                {
                    if (isSidebarExpanderGate)
                        CompleteGateAfterRender("ContinueCondition");
                    else
                    {
                        onGateReleased?.Invoke();
                        tcs.TrySetResult(null);
                    }
                }
            }

            void RewireExpanderHandlerIfNeeded()
            {
                if (!TryGetSidebarHeader(step.AnchorKey, out var header)) return;

                var current = FindExpanderByHeader(hostWindow, header);
                if (ReferenceEquals(current, observedExpander)) return;

                if (observedExpander != null && onExpanded != null)
                    observedExpander.Expanded -= onExpanded;

                observedExpander = current;
                if (observedExpander == null || onExpanded == null) return;

                observedExpander.Expanded += onExpanded;
                if (observedExpander.IsExpanded)
                    CompleteGateAfterRender("AlreadyExpanded");
            }

            void OnNext()
            {
                if (!isGateStep)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                // gate step에서는 manual advance 금지
            }

            void OnCancel()
            {
                // Walkthrough 중단 요청(Esc)
                StopActiveWalkthrough();
                tcs.TrySetCanceled(token);
            }

            TutorSpotlightOverlay.NextRequested += OnNext;
            TutorSpotlightOverlay.CancelRequested += OnCancel;

            // 외부 취소도 반영
            var registration = token.Register(() => tcs.TrySetCanceled(token));

            if (isGateStep && step.ContinueCondition == null)
            {
                Debug.WriteLine($"[TutorWalkthrough] gate step has no ContinueCondition: {step.AnchorKey}");
                tcs.TrySetResult(null);
            }

            if (isGateStep)
            {
                Debug.WriteLine($"[TutorWalkthrough] gate waiting start: {step.AnchorKey}");
                onExpanded = (_, __) =>
                {
                    CompleteGateAfterRender("ExpandedEvent");
                };

                onLayoutUpdated = (_, __) =>
                {
                    if (tcs.Task.IsCompleted) return;
                    RewireExpanderHandlerIfNeeded();
                    TryCompleteByCondition();
                };

                hostWindow.LayoutUpdated += onLayoutUpdated;
                RewireExpanderHandlerIfNeeded();
                if (step.AnchorKey.Equals("MAIN_OPERATION_BUTTON", StringComparison.OrdinalIgnoreCase))
                {
                    operationButton = hostWindow.FindName("xOperationButton") as Button;
                    if (operationButton != null)
                    {
                        onOperationClick = (_, __) => TryCompleteByCondition();
                        operationButton.Click += onOperationClick;
                    }
                }
                TryCompleteByCondition();
            }

            return tcs.Task.ContinueWith(async t =>
            {
                registration.Dispose();
                TutorSpotlightOverlay.NextRequested -= OnNext;
                TutorSpotlightOverlay.CancelRequested -= OnCancel;
                if (onLayoutUpdated != null)
                    hostWindow.LayoutUpdated -= onLayoutUpdated;
                if (observedExpander != null && onExpanded != null)
                    observedExpander.Expanded -= onExpanded;
                if (operationButton != null && onOperationClick != null)
                    operationButton.Click -= onOperationClick;
                if (isGateStep)
                    Debug.WriteLine($"[TutorWalkthrough] gate released: {step.AnchorKey}");

                await t.ConfigureAwait(false);
            }, TaskScheduler.Default).Unwrap();
        }

        private static bool TryResolveStepTarget(
            Window hostWindow,
            string anchorKey,
            out Window spotlightHostWindow,
            out FrameworkElement? target)
        {
            if (TryResolveSidebarHeaderTarget(hostWindow, anchorKey, out var headerTarget))
            {
                spotlightHostWindow = hostWindow;
                target = headerTarget;
                return true;
            }

            if (TutorAnchorResolver.TryResolveWithHost(hostWindow, anchorKey, out spotlightHostWindow, out target))
                return target != null;

            spotlightHostWindow = hostWindow;
            target = null;
            return false;
        }

        private static async Task CollapseSidebarGroupAsync(
            Window hostWindow,
            string sidebarGroupHeader,
            string? nextSidebarGroup,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await hostWindow.Dispatcher.InvokeAsync(() =>
            {
                token.ThrowIfCancellationRequested();

                hostWindow.UpdateLayout();

                var expander = FindExpanderByHeader(hostWindow, sidebarGroupHeader);
                if (expander == null) return;

                if (expander.IsExpanded)
                {
                    expander.IsExpanded = false;
                    expander.UpdateLayout();
                    hostWindow.UpdateLayout();
                }

                if (string.IsNullOrWhiteSpace(nextSidebarGroup)) return;

                var nextExpander = FindExpanderByHeader(hostWindow, nextSidebarGroup);
                if (nextExpander == null) return;

                nextExpander.ApplyTemplate();
                nextExpander.UpdateLayout();

                var nextHeaderTarget = FindDescendant<ToggleButton>(nextExpander, tb =>
                {
                    var text = tb.Content?.ToString()?.Trim() ?? string.Empty;
                    return tb.Name == "HeaderSite" || text.Equals(nextSidebarGroup, StringComparison.OrdinalIgnoreCase);
                });

                (nextHeaderTarget as FrameworkElement ?? nextExpander).BringIntoView();
            }, DispatcherPriority.Normal);

            await hostWindow.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        }

        private static async Task<bool> WaitForStableRectAfterGateAsync(
            Window spotlightHostWindow,
            FrameworkElement target,
            int timeoutMs,
            CancellationToken token)
        {
            var started = Stopwatch.GetTimestamp();
            while (ElapsedMs(started) <= timeoutMs)
            {
                token.ThrowIfCancellationRequested();
                var isReady = await spotlightHostWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (!target.IsLoaded || target.ActualWidth < 1 || target.ActualHeight < 1)
                        return false;

                    var surface = ResolveOverlaySurface(spotlightHostWindow);
                    return TutorSpotlightOverlay.TryGetStableTargetRectInSurface(surface, target, out _);
                }, DispatcherPriority.Render);

                if (isReady)
                    return true;
            }

            return false;
        }

        private static FrameworkElement ResolveOverlaySurface(Window hostWindow)
        {
            if (hostWindow.FindName("xMainWindowBorder") is FrameworkElement mainBorder)
                return mainBorder;

            if (hostWindow.Content is FrameworkElement contentRoot)
                return contentRoot;

            return hostWindow;
        }

        private static double ElapsedMs(long startedTick)
            => (Stopwatch.GetTimestamp() - startedTick) * 1000.0 / Stopwatch.Frequency;

        private static bool TryResolveSidebarHeaderTarget(Window hostWindow, string anchorKey, out FrameworkElement? target)
        {
            target = null;
            if (!TryGetSidebarHeader(anchorKey, out var header)) return false;

            var expander = FindExpanderByHeader(hostWindow, header);
            if (expander == null) return false;

            expander.ApplyTemplate();
            expander.UpdateLayout();

            var headerToggle = FindDescendant<ToggleButton>(expander, tb =>
            {
                var text = tb.Content?.ToString()?.Trim() ?? string.Empty;
                return tb.Name == "HeaderSite" || text.Equals(header, StringComparison.OrdinalIgnoreCase);
            });

            target = (FrameworkElement?)headerToggle ?? expander;


            return true;
        }

        private static bool TryGetSidebarHeader(string anchorKey, out string header)
        {
            header = string.Empty;
            if (anchorKey.Equals("SIDEBAR_TAB_TEACHING", StringComparison.OrdinalIgnoreCase)) { header = "TEACHING"; return true; }
            if (anchorKey.Equals("SIDEBAR_TAB_SPC", StringComparison.OrdinalIgnoreCase)) { header = "SPC"; return true; }
            if (anchorKey.Equals("SIDEBAR_TAB_SETUP", StringComparison.OrdinalIgnoreCase)) { header = "SETUP"; return true; }
            if (anchorKey.Equals("SIDEBAR_TAB_SYSTEM", StringComparison.OrdinalIgnoreCase)) { header = "SYSTEM"; return true; }
            return false;
        }

        private static string? GetSidebarGroupByAnchor(string anchorKey)
        {
            if (TryGetSidebarHeader(anchorKey, out var header)) 
                return header;

            if (anchorKey is "MENU_SETTINGS" or "MENU_CALIBRATION" or "MENU_LIGHT")
                return "SETUP";

            if (anchorKey is "MENU_MAPPING" or "MENU_BGA" or "MENU_LGA" or "MENU_QFN" or "MENU_STRIP" or "MENU_SIDE" or "SMART_ALIGN")
                return "TEACHING";

            if (anchorKey is "MENU_HISTORY" or "MENU_LOT_DATA")
                return "SPC";

            if (anchorKey is "MENU_MONITOR" or "MENU_AS" or "MENU_EXIT")
                return "SYSTEM";

            return null;
        }

        private static Expander? FindExpanderByHeader(DependencyObject root, string headerText)
        {
            return FindDescendant<Expander>(root, ex =>
            {
                var header = (ex.Header?.ToString() ?? string.Empty).Trim();
                return header.Equals(headerText, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static T? FindDescendant<T>(DependencyObject root, Func<T, bool>? predicate = null)
            where T : DependencyObject
        {
            if (root == null) return null;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t && (predicate == null || predicate(t)))
                    return t;

                var found = FindDescendant(child, predicate);
                if (found != null) return found;
            }

            return null;
        }

    }
}
