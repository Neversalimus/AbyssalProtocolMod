using System;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_LetterUtility
    {
        public static bool TryReceiveLetter(TaggedString label, TaggedString text, LetterDef letterDef, LookTargets lookTargets = null)
        {
            try
            {
                if (Find.LetterStack == null || letterDef == null)
                {
                    return false;
                }

                ABY_LetterUtility.TryReceiveLetter(label, text, letterDef, lookTargets);
                return true;
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "letter-send-failed:" + (label.ToString() ?? "unknown"),
                    "[Abyssal Protocol] Letter delivery failed and was suppressed: " + ex.GetType().Name + ": " + ex.Message,
                    3600);
                return false;
            }
        }
    }
}
