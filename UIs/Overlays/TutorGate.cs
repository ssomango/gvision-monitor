using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GVisionWpf.UIs.Overlays
{
    public static class TutorGate
    {
        private static readonly object Sync = new();
        private static readonly Dictionary<string, Guid> ArmedTokens = new(StringComparer.OrdinalIgnoreCase);

        public static Guid Arm(string scope)
        {
            var token = Guid.NewGuid();
            lock (Sync)
            {
                ArmedTokens[scope] = token;
            }

            Debug.WriteLine($"[TutorGate] scope={scope} phase=Arm token={token}");
            return token;
        }

        public static bool TryConsume(string scope, Guid token)
        {
            lock (Sync)
            {
                if (!ArmedTokens.TryGetValue(scope, out var armed))
                {
                    Debug.WriteLine($"[TutorGate] scope={scope} phase=ConsumeFail reason=NotArmed token={token}");
                    return false;
                }

                if (armed != token)
                {
                    Debug.WriteLine($"[TutorGate] scope={scope} phase=ConsumeFail reason=TokenMismatch armed={armed} token={token}");
                    return false;
                }

                ArmedTokens.Remove(scope);
            }

            Debug.WriteLine($"[TutorGate] scope={scope} phase=ConsumeOk token={token}");
            return true;
        }

        public static void Disarm(string scope, string reason)
        {
            lock (Sync)
            {
                ArmedTokens.Remove(scope);
            }

            Debug.WriteLine($"[TutorGate] scope={scope} phase=Disarm reason={reason}");
        }
    }
}
