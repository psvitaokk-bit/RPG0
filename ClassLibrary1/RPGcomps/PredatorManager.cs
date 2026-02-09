using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace ClassLibrary1.RPGcomps
{
    public class PredatorManager : MapComponent
    {
        private HashSet<Pawn> predators = new HashSet<Pawn>();
        private const int CheckInterval = 200;
        private string predatorAlertSound = "LA_Predator";

        public PredatorManager(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame % CheckInterval == 0)
            {
                UpdatePredatorList();
                CheckForPredators();
            }
        }

        // ▼ 洗い替え：野生(Faction==null)の肉食も対象にする
        private void UpdatePredatorList()
        {
            // ★ 除外条件を「存在/状態のみ」に縮小（派閥での除外はしない）
            predators.RemoveWhere(p =>
                p == null || !p.Spawned || p.Dead || p.Downed ||
                p.RaceProps?.Animal != true || !p.RaceProps.predator);

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn?.RaceProps?.Animal != true) continue;
                if (!pawn.RaceProps.predator) continue;

                // ★ ここが変更点：
                // - 野生（Faction==null）も含める
                // - 敵対派閥の肉食も含める（自派閥は除外）
                bool isWildPredator = pawn.Faction == null;
                bool isHostileFaction = pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.Faction.HostileTo(Faction.OfPlayer);

                if (isWildPredator || isHostileFaction)
                {
                    predators.Add(pawn);
                }
            }
        }

        private void CheckForPredators()
        {
            foreach (Pawn predator in predators)
            {
                if (predator == null || !predator.Spawned || predator.Dead || predator.Downed) continue;

                // ★ 派閥によるスキップを廃止（野生でも判定する）
                // すでに発狂中(Manhunter/ManhunterPermanent)なら何もしない
                if (predator.MentalState != null &&
                    (predator.MentalState.def == MentalStateDefOf.Manhunter ||
                     predator.MentalState.def == MentalStateDefOf.ManhunterPermanent))
                    continue;

                Pawn prey = FindPreyFor(predator);
                if (prey != null)
                {
                    // ★ 野生でも Manhunter を開始
                    predator.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, forceWake: true);
                    PlayPredatorAlertSound(predator.Position);
                }
            }
        }

        private Pawn FindPreyFor(Pawn predator)
        {
            float detectRadius = 10f;
            foreach (Pawn prey in map.mapPawns.FreeColonistsSpawned)
            {
                if (prey.Position.InHorDistOf(predator.Position, detectRadius))
                {
                    return prey;
                }
            }
            return null;
        }

        private void PlayPredatorAlertSound(IntVec3 position)
        {
            SoundDef soundDef = SoundDef.Named(predatorAlertSound);
            if (soundDef != null)
            {
                soundDef.PlayOneShot(new TargetInfo(position, map));
            }
        }
    }

    [HarmonyPatch(typeof(Map), "FinalizeInit")]
    public static class Map_FinalizeInit_Patch
    {
        static void Postfix(Map __instance)
        {
            if (!__instance.components.OfType<PredatorManager>().Any())
            {
                __instance.components.Add(new PredatorManager(__instance));
            }
        }
    }
}


