using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class DefModExtension_DominionFissure : DefModExtension
    {
        public string sheetTexPath;
        public int frameCount = 8;
        public int frameDurationTicks = 10;
        public float drawSizeX = 8f;
        public float drawSizeZ = 3f;
        public float altitudeOffset = 0.055f;
        public bool usePostLightShader = true;
        public float colorR = 1f;
        public float colorG = 1f;
        public float colorB = 1f;
        public float colorA = 0.92f;

        public Vector2 DrawSize
        {
            get { return new Vector2(Mathf.Max(0.1f, drawSizeX), Mathf.Max(0.1f, drawSizeZ)); }
        }

        public Color DrawColor
        {
            get { return new Color(Mathf.Clamp01(colorR), Mathf.Clamp01(colorG), Mathf.Clamp01(colorB), Mathf.Clamp01(colorA)); }
        }
    }
}
