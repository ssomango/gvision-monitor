using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

// ✅ 제거 추천: 이 줄 때문에 Window가 모호해짐
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

// ✅ 가장 안전: WPF Window 별칭
using WpfWindow = System.Windows.Window;

namespace GVisionWpf.UIs.Overlays
{
    public static class TutorSpotlightService
    {
        private static long _lastMode1TicksUtc;
        private static string? _lastMode1Anchor;
        private static readonly object _mode1Lock = new();
        private static int _mode1Sequence;


        public static void ShowMode1(string anchorKey, string? title, string? body, int durationMs = 5000, string? requestId = null)
        {
            if (string.IsNullOrWhiteSpace(anchorKey)) return;
            var rid = requestId ?? $"mode1-{System.Threading.Interlocked.Increment(ref _mode1Sequence)}";
            Debug.WriteLine($"[TutorMode1] SHOW_REQUEST rid={rid} anchor={anchorKey} activeOverlayId={TutorSpotlightOverlay.ActiveOverlayId} t={DateTime.Now:HH:mm:ss.fff}");

            lock (_mode1Lock)
            {
                var now = DateTime.UtcNow.Ticks;
                var window = TimeSpan.FromMilliseconds(300).Ticks;
                if (_lastMode1Anchor != null &&
                    _lastMode1Anchor.Equals(anchorKey, StringComparison.OrdinalIgnoreCase) &&
                    (now - _lastMode1TicksUtc) <= window)
                {
                    Debug.WriteLine($"[Tutor] Mode1 deduped: {anchorKey}");
                    return;
                }
                _lastMode1Anchor = anchorKey;
                _lastMode1TicksUtc = now;
            }
            try
            {
                if (Application.Current?.MainWindow is not WpfWindow w) return;

                // UI thread에서 async로 실행
                _ = w.Dispatcher.InvokeAsync(async () =>
                {
                    // ✅ 레이아웃 안정화 후 Show
                    await w.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
                    await w.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

                    w.UpdateLayout();

                    var target = ResolveAnchor(w, anchorKey);
                    if (target == null)
                    {
                        Debug.WriteLine("[Tutor] spotlight target NULL");
                        return;
                    }
                    Debug.WriteLine($"[Tutor] spotlight target={target.GetType().Name} W={target.ActualWidth} H={target.ActualHeight}");

                    target.UpdateLayout();

                    TutorSpotlightOverlay.Show(
                        hostWindow: w,
                        target: target,
                        title: title,
                        body: body,
                        durationMs: durationMs,
                        padding: 10,
                        dimOpacity: 0.62,
                        debugContext: new TutorSpotlightOverlay.DebugContext(rid, null, anchorKey, "Mode1")
                    );
                    Debug.WriteLine($"[TutorMode1] SHOW_DONE rid={rid} anchor={anchorKey} activeOverlayId={TutorSpotlightOverlay.ActiveOverlayId} t={DateTime.Now:HH:mm:ss.fff}");
                }, DispatcherPriority.Normal).Task.ContinueWith(t =>
                    {
                        if (t.Exception != null)
                            Debug.WriteLine($"[Tutor] ShowMode1 dispatcher task failed: {t.Exception}");
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tutor] ShowMode1 failed: {ex}");
            }

        }

        // ------------------------------------------------------------
        // Anchor Resolver
        // ------------------------------------------------------------
        private static FrameworkElement? ResolveAnchor(WpfWindow rootWindow, string anchorKey)
        {
            // 1) RUN/SETUP 토글
            if (string.Equals(anchorKey, "RUN_SETUP_TOGGLE", StringComparison.OrdinalIgnoreCase))
            {
                if (rootWindow.FindName("xOperationButton") is FrameworkElement fe) return fe;
                return FindDescendantByName<FrameworkElement>(rootWindow, "xOperationButton");
            }

            // 2) "티칭 메뉴 전체"
            if (string.Equals(anchorKey, "MENU_TEACHING", StringComparison.OrdinalIgnoreCase))
            {
                var exp = FindAllDescendants<Expander>(rootWindow)
                    .FirstOrDefault(e =>
                    {
                        var h = e.Header?.ToString() ?? "";
                        return h.IndexOf("teach", StringComparison.OrdinalIgnoreCase) >= 0
                            || h.IndexOf("티칭", StringComparison.OrdinalIgnoreCase) >= 0;
                    });

                if (exp != null)
                {
                    var items = FindAllDescendants<ItemsControl>(exp).ToList();

                    var best = items
                        .Select(ic => new
                        {
                            Control = ic as FrameworkElement,
                            ButtonCount = FindAllDescendants<Button>(ic).Count()
                        })
                        .OrderByDescending(x => x.ButtonCount)
                        .FirstOrDefault(x => x.ButtonCount >= 2);

                    if (best?.Control != null) return best.Control;

                    return exp as FrameworkElement;
                }

                return null;
            }

            // 3)
            //if(string.Equals(anchorKey, "", StringComp))
            return null;
        }

        // ------------------------------------------------------------
        // Helpers (VisualTree search)
        // ------------------------------------------------------------
        private static T? FindDescendantByAutomationId<T>(DependencyObject root, string automationId)
            where T : DependencyObject
        {
            foreach (var d in FindAllDescendants<T>(root))
            {
                var id = AutomationProperties.GetAutomationId(d);
                if (string.Equals(id, automationId, StringComparison.OrdinalIgnoreCase))
                    return d;
            }
            return null;
        }

        private static T? FindDescendantByName<T>(DependencyObject root, string name)
            where T : FrameworkElement
        {
            foreach (var fe in FindAllDescendants<FrameworkElement>(root))
            {
                if (fe is T typed && string.Equals(typed.Name, name, StringComparison.Ordinal))
                    return typed;
            }
            return null;
        }

        private static System.Collections.Generic.IEnumerable<T> FindAllDescendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null) yield break;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) yield return t;

                foreach (var sub in FindAllDescendants<T>(child))
                    yield return sub;
            }
        }
    }
}
