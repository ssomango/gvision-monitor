using System.Collections.Generic;
using System.Runtime.Serialization;
using GVisionWpf.Types;

namespace GVisionWpf.GlobalStates
{
    public class ControllerInfo
    {
        public string TaskName { get; set; } = string.Empty;
        public bool InvertXOffset { get; set; } = false;
        public bool InvertYOffset { get; set; } = false;
        public bool? InvertTOffset { get; set; } = false;
        public HashSet<ERunningMode> AllowedMode { get; set; }
        public string NotAllowedMessage { get; set; } = "Not Allowed In This Mode.";

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            AllowedMode ??= new HashSet<ERunningMode>() { ERunningMode.SetUp, ERunningMode.Run };
        }
    }
}