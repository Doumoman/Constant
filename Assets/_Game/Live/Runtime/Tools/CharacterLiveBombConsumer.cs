using System.Collections.Generic;
using StarNight.Character.Equipment;
using StarNight.Character.Live.Run;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 폭탄 라이브 소비자. 설치 판정·소모·폭발·지형 변경 전부 캐릭터 계약에
    /// 위임한다: CharacterBombPlacementPolicy(설치+소모 요청 쌍) →
    /// CharacterRunInventoryPolicy.ApplyBombSpend(인벤토리 반영 — 유일 경로)
    /// → CharacterBombFuse(주입 시간, 정확히 1회 폭발) →
    /// CharacterExplosionTerrainPolicy(파괴 가능 셀 한정 변경 요청) →
    /// 지형 명령 sink. MAP/Tilemap/씬을 직접 변조하지 않는다.
    /// 거부·중복 요청은 인벤토리·퓨즈·큐 어떤 것도 변조하지 않는다.
    /// </summary>
    public sealed class CharacterLiveBombConsumer
    {
        private readonly CharacterLiveRunSession session;
        private readonly ICharacterLiveTerrainCommandSink terrainSink;
        private readonly ICharacterMapWorldQuery worldQuery;
        private readonly CharacterBombSettings settings;
        private readonly CharacterLiveToolRequestLedger ledger;
        private readonly List<CharacterBombFuse> activeFuses;

        private int lastBombId;

        public CharacterLiveBombConsumer(
            CharacterLiveRunSession session,
            ICharacterLiveTerrainCommandSink terrainSink,
            ICharacterMapWorldQuery worldQuery,
            CharacterBombSettings settings,
            CharacterLiveToolRequestLedger ledger)
        {
            this.session = session;
            this.terrainSink = terrainSink;
            this.worldQuery = worldQuery;
            this.settings = settings;
            this.ledger = ledger;
            activeFuses = new List<CharacterBombFuse>();
        }

        public int ActiveFuseCount
        {
            get { return activeFuses.Count; }
        }

        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public int ExplosionCount { get; private set; }
        public CharacterLiveToolDiagnosticKind LastDiagnostic { get; private set; }
        public CharacterBombPlacementRequest LastPlacementRequest { get; private set; }

        /// <summary>
        /// 설치 소비. 수락 시 인벤토리 소모 1회 + 퓨즈 1개 점화가 정확히
        /// 한 번 일어난다. 재고 없음/무효 셀/sink 부재/중복은 무변조 거부.
        /// </summary>
        public CharacterLiveToolUseResult TryConsumeBomb(
            long requestId,
            Vector2 targetWorldPosition)
        {
            if (ledger.IsConsumed(CharacterLiveToolChannel.Bomb, requestId))
            {
                return Reject(CharacterLiveToolDiagnosticKind.DuplicateRequest);
            }

            if (terrainSink == null)
            {
                return Reject(CharacterLiveToolDiagnosticKind.MissingTerrainSink);
            }

            CharacterRunInventoryState inventory = session.RunState.Inventory;

            if (inventory.BombCount <= 0)
            {
                return Reject(CharacterLiveToolDiagnosticKind.NoBombStock);
            }

            WorldTileCoord targetCell;
            bool hasValidTargetCell = CharacterMapCoordinateBridge
                .TryGetTileCoordinate(targetWorldPosition, out targetCell);
            bool isPlaceable = hasValidTargetCell && IsCellPlaceable(targetCell);

            var input = new CharacterBombPlacementInput(
                session.ActorId,
                hasValidTargetCell,
                targetCell,
                inventory.BombCount,
                isPlaceable);

            CharacterBombPlacementRequest placementRequest;
            CharacterBombSpendRequest spendRequest;
            if (!CharacterBombPlacementPolicy.TryCreatePlacement(
                in input, out placementRequest, out spendRequest))
            {
                return Reject(CharacterLiveToolDiagnosticKind.InvalidBombPlacement);
            }

            CharacterRunInventoryApplyResult spendResult =
                CharacterRunInventoryPolicy.ApplyBombSpend(
                    in inventory, in spendRequest);

            if (!spendResult.Changed)
            {
                return Reject(CharacterLiveToolDiagnosticKind.NoBombStock);
            }

            session.UpdateRunState(
                session.RunState.WithInventory(spendResult.NewState));

            lastBombId++;
            activeFuses.Add(new CharacterBombFuse(
                lastBombId, session.ActorId, placementRequest.TargetCell, settings));

            LastPlacementRequest = placementRequest;
            ledger.TryMarkConsumed(CharacterLiveToolChannel.Bomb, requestId);
            return Accept();
        }

        /// <summary>
        /// 퓨즈 진행(주입 시간). 만료된 퓨즈마다 폭발 1회 — 파괴 가능 셀
        /// 한정 지형 변경 요청을 만들어 sink에 정확히 한 번 넣는다.
        /// 반환값은 이번 호출에서 발생한 폭발 수.
        /// </summary>
        public int TickFuses(float deltaSeconds)
        {
            int exploded = 0;

            for (int index = activeFuses.Count - 1; index >= 0; index--)
            {
                CharacterExplosionRequest explosion;
                if (!activeFuses[index].Tick(deltaSeconds, out explosion))
                {
                    continue;
                }

                List<CharacterTerrainMutationRequest> mutations =
                    CharacterExplosionTerrainPolicy.CreateTerrainMutationRequests(
                        in explosion, worldQuery);
                terrainSink.Enqueue(
                    new CharacterLiveTerrainCommand(explosion, mutations));

                activeFuses.RemoveAt(index);
                exploded++;
                ExplosionCount++;
            }

            return exploded;
        }

        private bool IsCellPlaceable(WorldTileCoord cell)
        {
            CharacterMapCellState state;
            if (worldQuery == null || !worldQuery.TryGetCellState(cell, out state))
            {
                // 미생성 셀은 설치 가능 공간으로 취급하지 않는다(어댑터 의미 일치).
                return false;
            }

            return !state.IsSolid;
        }

        private CharacterLiveToolUseResult Accept()
        {
            AcceptedCount++;
            LastDiagnostic = CharacterLiveToolDiagnosticKind.None;
            return CharacterLiveToolUseResult.Success();
        }

        private CharacterLiveToolUseResult Reject(
            CharacterLiveToolDiagnosticKind diagnostic)
        {
            RejectedCount++;
            LastDiagnostic = diagnostic;
            return CharacterLiveToolUseResult.Rejected(diagnostic);
        }
    }
}
