using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_BossMusicUtility
    {
        private static readonly HashSet<string> ReservedBossSongDefNames = new HashSet<string>
        {
            "ABY_ArchonBossBattleTheme",
            "ABY_RuptureBossBattleTheme",
            "ABY_ReactorSaintBossBattleTheme"
        };

        private static int authorizedDepth;
        private static string authorizedSongDefName;

        public static bool IsReservedBossSong(SongDef song)
        {
            return song != null && !song.defName.NullOrEmpty() && ReservedBossSongDefNames.Contains(song.defName);
        }

        public static bool IsReservedBossSongDefName(string defName)
        {
            return !defName.NullOrEmpty() && ReservedBossSongDefNames.Contains(defName);
        }

        public static bool ShouldBlockVanillaSelection(SongDef song)
        {
            return IsReservedBossSong(song);
        }

        public static bool ShouldAllowExplicitPlay(SongDef song)
        {
            if (!IsReservedBossSong(song))
            {
                return true;
            }

            if (authorizedDepth > 0 && authorizedSongDefName == song.defName)
            {
                return true;
            }

            return IsActiveBossSong(song);
        }

        public static IDisposable AuthorizeBossSongStart(SongDef song)
        {
            if (!IsReservedBossSong(song))
            {
                return EmptyAuthorization.Instance;
            }

            return new BossSongAuthorization(song.defName);
        }

        public static bool IsActiveBossSong(SongDef song)
        {
            if (!IsReservedBossSong(song) || Current.Game == null)
            {
                return false;
            }

            AbyssalBossScreenFXGameComponent bossFx = Current.Game.GetComponent<AbyssalBossScreenFXGameComponent>();
            if (bossFx == null)
            {
                return false;
            }

            Pawn boss = bossFx.ActiveBoss;
            if (boss == null || boss.Destroyed || boss.Dead)
            {
                return false;
            }

            ABY_BossBarProfileDef profile = bossFx.ActiveBossBarProfile;
            if (profile == null)
            {
                profile = AbyssalBossBarUtility.ResolveProfileFor(boss);
            }

            return profile != null && profile.bossSongDefName == song.defName;
        }

        private sealed class BossSongAuthorization : IDisposable
        {
            private readonly string previousSongDefName;
            private bool disposed;

            public BossSongAuthorization(string songDefName)
            {
                previousSongDefName = authorizedSongDefName;
                authorizedSongDefName = songDefName;
                authorizedDepth++;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                authorizedDepth = Math.Max(0, authorizedDepth - 1);
                if (authorizedDepth <= 0)
                {
                    authorizedSongDefName = null;
                    return;
                }

                authorizedSongDefName = previousSongDefName;
            }
        }

        private sealed class EmptyAuthorization : IDisposable
        {
            public static readonly EmptyAuthorization Instance = new EmptyAuthorization();

            private EmptyAuthorization()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
