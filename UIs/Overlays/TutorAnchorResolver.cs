using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using GVisionWpf.UIs.Frames.Pages;

namespace GVisionWpf.UIs.Overlays
{
    public static class TutorAnchorResolver
    {
        // -----------------------------
        // 1) 그룹(Expander) 헤더 anchor -> Expander Header(TabName) 매핑
        // -----------------------------
        private static readonly Dictionary<string, string> SidebarHeaderAnchorToTabHeader =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "MAIN_SIDEBAR_TEACHING_HEADER", "TEACHING" },
                { "MAIN_SIDEBAR_SPC_HEADER", "SPC" },
                { "MAIN_SIDEBAR_SETUP_HEADER", "SETUP" },
                { "MAIN_SIDEBAR_SYSTEM_HEADER", "SYSTEM" },
                { "MENU_TEACHING_TAB_HEADER", "TEACHING" },
                { "MENU_SPC_TAB_HEADER", "SPC" },
                { "MENU_SETUP_TAB_HEADER", "SETUP" },
                { "MENU_SYSTEM_TAB_HEADER", "SYSTEM" },

                // legacy anchors
                { "MENU_TEACHING",     "TEACHING" },
                { "MENU_SPC",          "SPC" },
                { "MENU_SETUP_GROUP",  "SETUP" },
                { "MENU_SYSTEM",       "SYSTEM" },
            };

        // -----------------------------
        // 2) Walkthrough용 사이드바 내부 항목 anchor -> 버튼 AutomationId
        // -----------------------------
        private static readonly Dictionary<string, string> SidebarItemAnchorToButtonAutomationId =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "MAIN_SIDEBAR_TEACHING_FIRSTITEM", "MAPPING" },
                { "MAIN_SIDEBAR_SPC_FIRSTITEM", "History" },
                { "MAIN_SIDEBAR_SETUP_FIRSTITEM", "Settings" },
                { "MAIN_SIDEBAR_SYSTEM_FIRSTITEM", "Monitor" },
            };

        // -----------------------------
        // 3) 기존 "개념 anchor" -> 실제 버튼 AutomationId(=sidebar.json의 Name) 매핑
        // -----------------------------
        private static readonly Dictionary<string, string> AnchorToButtonAutomationId =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // TEACHING
                { "MENU_MAPPING", "MAPPING" },
                { "MENU_BGA", "BGA" },
                { "MENU_LGA", "LGA" },
                { "MENU_QFN", "QFN" },
                { "MENU_STRIP", "STRIP" },
                { "MENU_SIDE", "SIDE" },
                { "SMART_ALIGN", "SMART ALIGN" },     // 네 json에 "SMART ALIGN"

                // SPC
                { "MENU_HISTORY", "History" },
                { "MENU_LOT_DATA", "Lot Data" },       // 네 json에 "Lot Data" (공백 포함)

                // SETUP
                { "MENU_SETTINGS", "Settings" },
                { "MENU_CALIBRATION", "Calibration" },
                { "MENU_LIGHT", "Light" },

                // SYSTEM
                { "MENU_MONITOR", "Monitor" },
                { "MENU_AS", "A/S" },
                { "MENU_EXIT", "Exit" },
            };

        // -----------------------------
        // 4) MAIN_* anchor -> MainWindow x:Name 매핑
        // -----------------------------
        private static readonly Dictionary<string, string> MainAnchorToElementName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "MAIN_TITLE_BAR", "xModeInfoPanel" },
                { "MAIN_LOGO_BUTTON", "xLogoButton" },
                { "MAIN_OPERATION_BUTTON", "xOperationButton" },
                { "MAIN_SIDEBAR_ITEMS", "xSidebarItemsControl" },
                { "MAIN_SIDEBAR_TEACHING_HEADER", "xSidebarItemsControl" },
                { "MAIN_SIDEBAR_SPC_HEADER", "xSidebarItemsControl" },
                { "MAIN_SIDEBAR_SETUP_HEADER", "xSidebarItemsControl" },
                { "MAIN_SIDEBAR_SYSTEM_HEADER", "xSidebarItemsControl" },
                { "MENU_TEACHING_TAB_HEADER", "xSidebarItemsControl" },
                { "MENU_SPC_TAB_HEADER", "xSidebarItemsControl" },
                { "MENU_SETUP_TAB_HEADER", "xSidebarItemsControl" },
                { "MENU_SYSTEM_TAB_HEADER", "xSidebarItemsControl" },
                { "MAIN_CONNECTION_BUTTON", "xConnectionBtn" },
                { "MAIN_LOT_INFO_PANEL", "xLotInfoPanel" },
                { "MAIN_STATISTICS_PANEL", "xStatisticsPanel" },
                { "MAIN_SYSTEM_INFORMATION_PANEL", "systemInformationPanel" },
                { "MAIN_MAINFRAME", "xMainFrame" },
                { "MAIN_BOTTOM_AREA", "xBottomArea" },
                // 기존 개념 anchor도 유지
                { "MAIN_TOP_INFO", "xModeInfoPanel" },
                { "MAIN_BOTTOM_INFO", "xLotInfoPanel" },
            };

        private static readonly Dictionary<string, string> SetupAnchorToElementName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "SETUP_PAGE_ROOT", "xSetupRoot" },
                { "SETUP_PAGE_SURFACE", "xSetupRoot" },
            };

        private static readonly Dictionary<string, string> RunAnchorToElementName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "RUN_PAGE_ROOT", "xRunRoot" },
                { "RUN_PAGE_SURFACE", "xRunRoot" },
                { "RUN_PANEL_ROOT", "xRunRoot" },
                { "RUN_PANEL_SURFACE", "xRunSurface" },
            };

        private static readonly Dictionary<string, (string WindowTitle, string? ElementName)> RunFloatingPanelAnchorToWindow =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "RUN_PANEL_PRS_INSPECTION", ("PRS Inspection", "xVisionWindow") },
                { "RUN_PANEL_MAPPING_INSPECTION", ("Mapping Inspection", "xVisionWindow") },
                { "RUN_PANEL_TOP_STRIP_INSPECTION", ("Top Strip Inspection", "xVisionWindow") },
                { "RUN_PANEL_PRS_DEVICE_VIEW", ("PRS Device View", null) },
                { "RUN_PANEL_MAPPING_DEVICE_VIEW", ("Mapping Device View", null) },
                { "RUN_PANEL_X1_PICKER_DEVICE_VIEW", ("X1 Picker Device View", null) },
                { "RUN_PANEL_X2_PICKER_DEVICE_VIEW", ("X2 Picker Device View", null) },
                { "RUN_PANEL_PRS_RESULT", ("PRS Result View", "xResultViewWindow") },
                { "RUN_PANEL_MAPPING_RESULT", ("Mapping Result View", "xResultViewWindow") },
            };

        /// <summary>
        /// anchor_key -> 실제 FrameworkElement 찾기 (Spotlight target)
        /// </summary>
        public static FrameworkElement? Resolve(Window rootWindow, string anchorKey)
        {
            return TryResolveWithHost(rootWindow, anchorKey, out _, out var target)
                ? target
                : null;
        }

        public static bool TryResolveWithHost(
            Window rootWindow,
            string anchorKey,
            out Window hostWindow,
            out FrameworkElement? target)
        {
            hostWindow = rootWindow;
            target = null;

            if (rootWindow == null || string.IsNullOrWhiteSpace(anchorKey))
                return false;

            rootWindow.UpdateLayout();

            // ---------------------------------------------------
            // (0) MAIN_* 명시적 Name 매핑
            // ---------------------------------------------------
            if (MainAnchorToElementName.TryGetValue(anchorKey, out var elementName))
            {
                var named = rootWindow.FindName(elementName) as FrameworkElement;
                if (named != null)
                {
                    target = named;
                    return true;
                }
            }

            if (anchorKey.StartsWith("SETUP_", StringComparison.OrdinalIgnoreCase))
            {
                var setupPage = GetCurrentMainFrameContent<SetupPage>(rootWindow);
                if (setupPage == null) return false;
                target = ResolveInPage(setupPage, anchorKey, SetupAnchorToElementName);
                return target != null;
            }

            if (anchorKey.StartsWith("RUN_", StringComparison.OrdinalIgnoreCase))
            {
                var runPage = GetCurrentMainFrameContent<RunPage>(rootWindow);
                if (runPage != null)
                {
                    target = ResolveInPage(runPage, anchorKey, RunAnchorToElementName);
                    if (target != null) return true;
                }

                if (RunFloatingPanelAnchorToWindow.TryGetValue(anchorKey, out var floatingPanelInfo))
                {
                    var floatingWindow = FindOwnedWindowByTitle(rootWindow, floatingPanelInfo.WindowTitle);
                    if (floatingWindow != null)
                    {
                        // Overlay는 MainWindow에 그려서 sidebar/title 포함 전체 딤을 유지한다.
                        hostWindow = rootWindow;
                        target = ResolveInWindow(floatingWindow, anchorKey, floatingPanelInfo.ElementName);
                        if (target != null)
                        {
                            Debug.WriteLine($"[TutorAnchorResolver] RUN floating target resolved. anchor={anchorKey} host={hostWindow.Title} targetWindow={floatingWindow.Title} targetType={target.GetType().Name}");
                        }
                        return target != null;
                    }
                }

                return false;
            }

            // ---------------------------------------------------
            // (A) RUN/SETUP 토글: x:Name이 있으니 이게 가장 확실
            // ---------------------------------------------------
            if (anchorKey.Equals("RUN_SETUP_TOGGLE", StringComparison.OrdinalIgnoreCase))
            {
                var opBtn = rootWindow.FindName("xOperationButton") as FrameworkElement;
                if (opBtn != null)
                {
                    target = opBtn;
                    return true;
                }

                // fallback: content RUN/SETUP 텍스트로 찾기
                var byContent = FindDescendant<Button>(rootWindow, b =>
                {
                    var content = (b.Content?.ToString() ?? "").Trim();
                    return content.Equals("RUN", StringComparison.OrdinalIgnoreCase)
                        || content.Equals("SETUP", StringComparison.OrdinalIgnoreCase);
                });
                target = byContent;
                return target != null;
            }

            // ---------------------------------------------------
            // (B) 그룹(Expander) 헤더 anchor
            if (SidebarHeaderAnchorToTabHeader.TryGetValue(anchorKey, out var header))
            {
                target = ResolveSidebarHeaderAnchor(rootWindow, header);
                return target != null;
            }

            // ---------------------------------------------------
            // (C) Walkthrough용 사이드바 내부 항목 anchor -> AutomationId로 찾기
            // ---------------------------------------------------
            if (SidebarItemAnchorToButtonAutomationId.TryGetValue(anchorKey, out var walkthroughButtonId))
            {
                target = ResolveSidebarButton(rootWindow, walkthroughButtonId);
                return target != null;
            }

            // ---------------------------------------------------
            // (D) 기존 하위 메뉴 버튼 anchor -> (그룹 먼저 열고) AutomationId로 찾기
            // ---------------------------------------------------
            if (AnchorToButtonAutomationId.TryGetValue(anchorKey, out var buttonId))
            {
                // 1) 어느 그룹인지 추정해서 먼저 펼친다 (중요)
                var groupHeader = GuessGroupHeaderByAnchor(anchorKey);
                if (!string.IsNullOrWhiteSpace(groupHeader))
                {
                    var exp = FindExpanderByExactHeader(rootWindow, groupHeader);
                    if (exp != null) ExpandExpander(exp);
                }

                target = ResolveSidebarButton(rootWindow, buttonId);
                return target != null;
            }

            return false;
        }

        public static bool IsSidebarGroupExpanded(Window rootWindow, string headerAnchorKey)
        {
            if (rootWindow == null || string.IsNullOrWhiteSpace(headerAnchorKey))
                return false;

            if (!SidebarHeaderAnchorToTabHeader.TryGetValue(headerAnchorKey, out var header))
                return false;

            rootWindow.UpdateLayout();
            var exp = FindExpanderByExactHeader(rootWindow, header);
            if (exp == null) return false;

            if (exp.IsExpanded) return true;

            var expandSite = FindDescendant<FrameworkElement>(exp, fe =>
                string.Equals(fe.Name, "ExpandSite", StringComparison.Ordinal));

            return expandSite?.Visibility == Visibility.Visible;
        }

        // =============================
        // Helpers
        // =============================

        private static FrameworkElement? ResolveSidebarHeaderAnchor(Window rootWindow, string header)
        {
            var exp = FindExpanderByExactHeader(rootWindow, header);
            if (exp == null) return null;

            var headerToggle = FindExpanderHeaderToggle(exp, header);
            if (headerToggle != null)
            {
                BringIntoViewIfPossible(headerToggle);
                return headerToggle;
            }

            var headerText = FindDescendant<System.Windows.Controls.TextBlock>(exp, t =>
                (t.Text ?? "").Trim().Equals(header, StringComparison.OrdinalIgnoreCase));

            if (headerText != null)
            {
                BringIntoViewIfPossible(headerText);
                return headerText;
            }
            BringIntoViewIfPossible(exp);
            return exp;
        }

        private static FrameworkElement? ResolveSidebarButton(Window rootWindow, string buttonId)
        {
            var btn = FindDescendant<Button>(rootWindow, b =>
            {
                var id = AutomationProperties.GetAutomationId(b);
                if (!string.IsNullOrWhiteSpace(id) &&
                    id.Equals(buttonId, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[TutorAnchorResolver] MATCH by AutomationId: {id}");
                    return true;
                }

                var label = FindDescendant<Label>(b, _ => true);
                var labelText = label?.Content?.ToString() ?? "";
                if (labelText.Equals(buttonId, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[TutorAnchorResolver] MATCH by Label: {labelText}, AutomationId={id}");
                    return true;
                }

                return false;
            });

            if (btn == null) return null;

            btn.UpdateLayout();
            BringIntoViewIfPossible(btn);

            if (btn.Content is FrameworkElement fe)
                return fe;

            var icon = FindDescendant<Image>(btn, img => img.ActualWidth > 0 && img.ActualWidth <= 60);
            if (icon != null) return icon;

            var label = FindDescendant<Label>(btn, _ => true);
            if (label != null) return label;

            return btn;
        }

        private static void ExpandExpander(Expander exp)
        {
            // 너 템플릿은 ContentPresenter(ExpandSite)가 IsExpanded에 따라 Visible이 되므로
            // "펼치고 -> 레이아웃 갱신"이 필수.
            if (!exp.IsExpanded)
                exp.IsExpanded = true;

            exp.UpdateLayout();

            // 가끔 한번 더 갱신해줘야 내부 ItemsControl이 생성되는 경우가 있음
            var win = Window.GetWindow(exp);
            win?.UpdateLayout();
        }

        private static FrameworkElement? FindExpanderHeaderToggle(Expander exp, string headerText)
        {
            exp.ApplyTemplate();
            exp.UpdateLayout();

            var byAutomationId = FindDescendant<System.Windows.Controls.Primitives.ToggleButton>(exp, tb =>
            {
                var automationId = AutomationProperties.GetAutomationId(tb);
                return !string.IsNullOrWhiteSpace(automationId)
                    && automationId.Equals(headerText, StringComparison.OrdinalIgnoreCase);
            });
            if (byAutomationId != null) return byAutomationId;

            var byName = FindDescendant<System.Windows.Controls.Primitives.ToggleButton>(exp, tb =>
                string.Equals(tb.Name, "HeaderSite", StringComparison.Ordinal));
            if (byName != null) return byName;

            var byContent = FindDescendant<System.Windows.Controls.Primitives.ToggleButton>(exp, tb =>
            {
                var content = tb.Content?.ToString()?.Trim() ?? "";
                return content.Equals(headerText, StringComparison.OrdinalIgnoreCase);
            });
            return byContent;
        }

        private static void BringIntoViewIfPossible(FrameworkElement element)
        {
            try
            {
                element.BringIntoView();
                element.UpdateLayout();
            }
            catch
            {
                // ignore
            }
        }

        private static TPage? GetCurrentMainFrameContent<TPage>(Window rootWindow)
            where TPage : Page
        {
            var frame = rootWindow.FindName("xMainFrame") as Frame
                ?? FindDescendant<Frame>(rootWindow, _ => true);
            if (frame == null) return null;
            frame.UpdateLayout();
            return frame.Content as TPage;
        }

        private static FrameworkElement? ResolveInPage(
            Page page,
            string anchorKey,
            IReadOnlyDictionary<string, string> anchorToElementName)
        {
            page.UpdateLayout();

            if (anchorToElementName.TryGetValue(anchorKey, out var elementName))
            {
                var named = page.FindName(elementName) as FrameworkElement;
                if (named != null) return named;
            }

            var byName = FindDescendant<FrameworkElement>(page, fe =>
                fe.Name.Equals(anchorKey, StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName;

            var byAutomationId = FindDescendant<FrameworkElement>(page, fe =>
            {
                var automationId = AutomationProperties.GetAutomationId(fe);
                return !string.IsNullOrWhiteSpace(automationId)
                    && automationId.Equals(anchorKey, StringComparison.OrdinalIgnoreCase);
            });
            if (byAutomationId != null) return byAutomationId;

            var byAutomationName = FindDescendant<FrameworkElement>(page, fe =>
            {
                var automationName = AutomationProperties.GetName(fe);
                return !string.IsNullOrWhiteSpace(automationName)
                    && automationName.Equals(anchorKey, StringComparison.OrdinalIgnoreCase);
            });
            return byAutomationName;
        }

        private static FrameworkElement? ResolveInWindow(Window window, string anchorKey, string? elementName)
        {
            window.UpdateLayout();

            if (!string.IsNullOrWhiteSpace(elementName))
            {
                if (window.FindName(elementName) is FrameworkElement named)
                    return named;

                var byName = FindDescendant<FrameworkElement>(window, fe =>
                    fe.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase));
                if (byName != null) return byName;
            }

            var byAnchorName = FindDescendant<FrameworkElement>(window, fe =>
                fe.Name.Equals(anchorKey, StringComparison.OrdinalIgnoreCase));
            if (byAnchorName != null) return byAnchorName;

            return window;
        }

        private static Window? FindOwnedWindowByTitle(Window rootWindow, string windowTitle)
        {
            if (string.IsNullOrWhiteSpace(windowTitle))
                return null;

            return Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w =>
                    string.Equals(w.Title, windowTitle, StringComparison.OrdinalIgnoreCase) &&
                    (ReferenceEquals(w.Owner, rootWindow) || ReferenceEquals(w, rootWindow)));
        }

        private static string? GuessGroupHeaderByAnchor(string anchorKey)
        {
            // TEACHING 하위
            if (anchorKey is "MENU_MAPPING" or "MENU_BGA" or "MENU_LGA" or "MENU_QFN" or "MENU_STRIP" or "MENU_SIDE" or "SMART_ALIGN")
                return "TEACHING";

            // SPC 하위
            if (anchorKey is "MENU_HISTORY" or "MENU_LOT_DATA")
                return "SPC";

            // SETUP 하위
            if (anchorKey is "MENU_SETTINGS" or "MENU_CALIBRATION" or "MENU_LIGHT")
                return "SETUP";

            // SYSTEM 하위
            if (anchorKey is "MENU_MONITOR" or "MENU_AS" or "MENU_EXIT")
                return "SYSTEM";

            return null;
        }

        private static Expander? FindExpanderByExactHeader(DependencyObject root, string headerText)
        {
            return FindDescendant<Expander>(root, ex =>
            {
                var header = (ex.Header?.ToString() ?? "").Trim();
                return header.Equals(headerText, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static T? FindDescendant<T>(DependencyObject root, Func<T, bool>? predicate = null)
            where T : DependencyObject
        {
            if (root == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is T t && (predicate == null || predicate(t)))
                    return t;

                var found = FindDescendant<T>(child, predicate);
                if (found != null) return found;
            }
            return null;
        }
    }
}
