using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GVisionWpf.UIs.Overlays
{
    public abstract class TutorStep
    {
        protected TutorStep(
            string key,
            string title,
            string body,
            string anchorAutomationId,
            string? cardAnchorAutomationId = null,
            string? cardImageUri = null,
            bool useSpotlight = true,
            int transitionDelayMs = 0)
        {
            Key = key;
            Title = title;
            Body = body;
            AnchorAutomationId = anchorAutomationId;
            CardAnchorAutomationId = cardAnchorAutomationId;
            CardImageUri = cardImageUri;
            UseSpotlight = useSpotlight;
            TransitionDelayMs = Math.Max(0, transitionDelayMs);
        }

        public string Key { get; }
        public string Title { get; }
        public string Body { get; }
        public string AnchorAutomationId { get; }
        public string? CardAnchorAutomationId { get; }
        public string? CardImageUri { get; }
        public bool UseSpotlight { get; }
        public int TransitionDelayMs { get; }
    }

    public sealed class StepExecutionResult
    {
        private StepExecutionResult(string? targetStepKey)
        {
            TargetStepKey = targetStepKey;
        }

        public string? TargetStepKey { get; }

        public bool HasJumpTarget => !string.IsNullOrWhiteSpace(TargetStepKey);

        public static StepExecutionResult Continue { get; } = new(null);

        public static StepExecutionResult JumpTo(string targetStepKey)
        {
            if (string.IsNullOrWhiteSpace(targetStepKey))
            {
                throw new ArgumentException("Target step key is required.", nameof(targetStepKey));
            }

            return new StepExecutionResult(targetStepKey);
        }
    }

    public sealed class InfoStep : TutorStep
    {
        public InfoStep(
            string key,
            string title,
            string body,
            string anchorAutomationId,
            string? cardAnchorAutomationId = null,
            string? cardImageUri = null,
            bool useSpotlight = true,
            int transitionDelayMs = 0)
            : base(key, title, body, anchorAutomationId, cardAnchorAutomationId, cardImageUri, useSpotlight, transitionDelayMs)
        {
        }
    }

    public sealed class WaitClickStep : TutorStep
    {
        public WaitClickStep(
            string key,
            string title,
            string body,
            string anchorAutomationId,
            IEnumerable<string> clickAutomationIds,
            bool allowSkip = false,
            bool consumeMatchingClick = true,
            string? cardAnchorAutomationId = null,
            string? cardImageUri = null,
            bool useSpotlight = true,
            int transitionDelayMs = 0)
            : base(key, title, body, anchorAutomationId, cardAnchorAutomationId, cardImageUri, useSpotlight, transitionDelayMs)
        {
            ClickAutomationIds = clickAutomationIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                ?? throw new ArgumentNullException(nameof(clickAutomationIds));
            AllowSkip = allowSkip;
            ConsumeMatchingClick = consumeMatchingClick;
        }

        public IReadOnlyList<string> ClickAutomationIds { get; }
        public bool AllowSkip { get; }
        public bool ConsumeMatchingClick { get; }
    }

    public sealed class WaitConditionStep : TutorStep
    {
        public WaitConditionStep(
            string key,
            string title,
            string body,
            string anchorAutomationId,
            Func<Window, bool> condition,
            int pollIntervalMs = 100,
            string? cardAnchorAutomationId = null,
            string? cardImageUri = null,
            bool useSpotlight = true,
            int transitionDelayMs = 0)
            : base(key, title, body, anchorAutomationId, cardAnchorAutomationId, cardImageUri, useSpotlight, transitionDelayMs)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            PollIntervalMs = Math.Max(30, pollIntervalMs);
        }

        public Func<Window, bool> Condition { get; }
        public int PollIntervalMs { get; }
    }

    public sealed class WaitValueStep : TutorStep
    {
        public WaitValueStep(
            string key,
            string title,
            string body,
            string anchorAutomationId,
            Func<Window, bool> isChanged,
            int pollIntervalMs = 100,
            string? cardAnchorAutomationId = null,
            string? cardImageUri = null,
            bool useSpotlight = true,
            int transitionDelayMs = 0)
            : base(key, title, body, anchorAutomationId, cardAnchorAutomationId, cardImageUri, useSpotlight, transitionDelayMs)
        {
            IsChanged = isChanged ?? throw new ArgumentNullException(nameof(isChanged));
            PollIntervalMs = Math.Max(30, pollIntervalMs);
        }

        public Func<Window, bool> IsChanged { get; }
        public int PollIntervalMs { get; }
    }

    public sealed class ActionStep : TutorStep
    {
        public ActionStep(
            string key,
            string title,
            string body,
            string anchorAutomationId,
            Func<Window, CancellationToken, Task> action,
            string? cardAnchorAutomationId = null,
            string? cardImageUri = null,
            bool useSpotlight = false,
            int transitionDelayMs = 0)
            : base(key, title, body, anchorAutomationId, cardAnchorAutomationId, cardImageUri, useSpotlight, transitionDelayMs)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public Func<Window, CancellationToken, Task> Action { get; }
    }

    public sealed class BranchStep : TutorStep
    {
        public BranchStep(
            string key,
            string title,
            string body,
            string anchorAutomationId,
            Func<Window, CancellationToken, Task<StepExecutionResult>> branch,
            string? cardAnchorAutomationId = null,
            string? cardImageUri = null,
            bool useSpotlight = false,
            int transitionDelayMs = 0)
            : base(key, title, body, anchorAutomationId, cardAnchorAutomationId, cardImageUri, useSpotlight, transitionDelayMs)
        {
            Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        }

        public Func<Window, CancellationToken, Task<StepExecutionResult>> Branch { get; }
    }
}
