using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_ProtocolResearchProgressGameComponent : GameComponent
    {
        private List<string> decodedProjectDefNames = new List<string>();
        private HashSet<string> decodedSet;

        public static ABY_ProtocolResearchProgressGameComponent Current => Verse.Current.Game?.GetComponent<ABY_ProtocolResearchProgressGameComponent>();

        public ABY_ProtocolResearchProgressGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref decodedProjectDefNames, "ABY_decodedProtocolProjectDefNames", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (decodedProjectDefNames == null)
                {
                    decodedProjectDefNames = new List<string>();
                }
                decodedSet = null;
                RebuildSet();
            }
        }

        public bool IsDecoded(string projectDefName)
        {
            if (projectDefName.NullOrEmpty())
            {
                return true;
            }

            RebuildSet();
            return decodedSet.Contains(projectDefName);
        }

        public void MarkDecoded(string projectDefName)
        {
            if (projectDefName.NullOrEmpty())
            {
                return;
            }

            RebuildSet();
            if (decodedSet.Add(projectDefName))
            {
                decodedProjectDefNames.Add(projectDefName);
            }
        }

        public void ForgetDecodedForDebug(string projectDefName)
        {
            if (projectDefName.NullOrEmpty())
            {
                return;
            }

            RebuildSet();
            if (decodedSet.Remove(projectDefName))
            {
                decodedProjectDefNames.RemoveAll(name => name == projectDefName);
            }
        }

        private void RebuildSet()
        {
            if (decodedProjectDefNames == null)
            {
                decodedProjectDefNames = new List<string>();
            }

            if (decodedSet != null)
            {
                return;
            }

            decodedSet = new HashSet<string>();
            for (int i = 0; i < decodedProjectDefNames.Count; i++)
            {
                string name = decodedProjectDefNames[i];
                if (!name.NullOrEmpty())
                {
                    decodedSet.Add(name);
                }
            }
        }
    }
}
