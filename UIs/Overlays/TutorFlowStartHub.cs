using System;
using System.Diagnostics;

namespace GVisionWpf.UIs.Overlays
{
    public static class TutorFlowStartHub
    {
        public static void Start(TutorFlowRunner runner, string runId, string source)
        {
            Debug.WriteLine($"[TutorFlow] runId={runId} stepIndex=- stepKey=- phase=StartDispatch reason={source} clickedAutomationId=-");
            Debug.WriteLine($"[TutorFlow] runId={runId} phase=StartStack trace={Environment.StackTrace}");
            runner.Start();
        }
    }
}
