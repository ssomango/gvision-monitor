using System;
using System.Windows;

namespace GVisionWpf.UIs.Overlays
{
    public sealed class TutorSpotlightStep
    {
        public string AnchorKey { get; }
        public string? Title { get; }
        public string? Body { get; }
        public int DurationMs { get; }
        public int GapAfterMs { get; }
        public bool IsGateStep { get; }
        public Func<Window, bool>? ContinueCondition { get; }
        public int PollIntervalMs { get; }

        public bool HasContinueCondition => ContinueCondition != null;

        public TutorSpotlightStep(
            string anchorKey,
            string? title,
            string? body,
            int durationMs = 4500,
            int gapAfterMs = 0,
            bool isGateStep = false,
            Func<Window, bool>? continueCondition = null,
            int pollIntervalMs = 80)
        {
            if (string.IsNullOrWhiteSpace(anchorKey))
            {
                throw new ArgumentException("Anchor key is required.", nameof(anchorKey));
            }

            AnchorKey = anchorKey;
            Title = title;
            Body = body;
            DurationMs = Math.Max(300, durationMs);
            GapAfterMs = Math.Max(0, gapAfterMs);
            IsGateStep = isGateStep;
            ContinueCondition = continueCondition;
            PollIntervalMs = Math.Max(30, pollIntervalMs);
        }
    }
}
