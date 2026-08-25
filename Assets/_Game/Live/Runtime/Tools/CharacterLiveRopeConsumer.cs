using System.Collections.Generic;
using StarNight.Character.Equipment;
using StarNight.Character.Live.Run;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Character.Traversal;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 로프 라이브 소비자. 설치 판정·소모·세그먼트 생성 전부 캐릭터 계약에
    /// 위임한다: CharacterRopePlacementPolicy(설치+소모 요청 쌍) →
    /// CharacterRunInventoryPolicy.ApplyRopeSpend(인벤토리 반영 — 유일 경로)
    /// → CharacterRopeSegmentPolicy(경계·고체·최대 길이 제한 수직 세그먼트)
    /// → 로프 명령 sink. 프리팹 생성·씬 배치를 하지 않는다.
    /// 거부·중복 요청은 인벤토리·큐 어떤 것도 변조하지 않는다.
    /// </summary>
    public sealed class CharacterLiveRopeConsumer
    {
        private readonly CharacterLiveRunSession session;
        private readonly ICharacterLiveRopeCommandSink ropeSink;
        private readonly ICharacterMapWorldQuery worldQuery;
        private readonly CharacterRopeSettings settings;
        private readonly CharacterLiveToolRequestLedger ledger;

        private int lastRopeId;

        public CharacterLiveRopeConsumer(
            CharacterLiveRunSession session,
            ICharacterLiveRopeCommandSink ropeSink,
            ICharacterMapWorldQuery worldQuery,
            CharacterRopeSettings settings,
            CharacterLiveToolRequestLedger ledger)
        {
            this.session = session;
            this.ropeSink = ropeSink;
            this.worldQuery = worldQuery;
            this.settings = settings;
            this.ledger = ledger;
        }

        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public CharacterLiveToolDiagnosticKind LastDiagnostic { get; private set; }
        public CharacterRopePlacementRequest LastPlacementRequest { get; private set; }
        public int LastSegmentCount { get; private set; }

        /// <summary>
        /// 설치 소비. 수락 시 인벤토리 소모 1회 + 로프 명령 1건 enqueue가
        /// 정확히 한 번 일어난다. 재고 없음/무효 원점(범위 밖·미생성)/막힌
        /// 원점(고체)/sink 부재/중복은 무변조 거부.
        /// </summary>
        public CharacterLiveToolUseResult TryConsumeRope(
            long requestId,
            Vector2 originWorldPosition)
        {
            if (ledger.IsConsumed(CharacterLiveToolChannel.Rope, requestId))
            {
                return Reject(CharacterLiveToolDiagnosticKind.DuplicateRequest);
            }

            if (ropeSink == null)
            {
                return Reject(CharacterLiveToolDiagnosticKind.MissingRopeSink);
            }

            CharacterRunInventoryState inventory = session.RunState.Inventory;

            if (inventory.RopeCount <= 0)
            {
                return Reject(CharacterLiveToolDiagnosticKind.NoRopeStock);
            }

            WorldTileCoord originCell;
            if (!CharacterMapCoordinateBridge.TryGetTileCoordinate(
                originWorldPosition, out originCell))
            {
                return Reject(CharacterLiveToolDiagnosticKind.InvalidRopeAnchor);
            }

            CharacterMapCellState originState;
            if (worldQuery == null
                || !worldQuery.TryGetCellState(originCell, out originState))
            {
                // 미생성 셀은 앵커 가능 공간으로 취급하지 않는다(어댑터 의미 일치).
                return Reject(CharacterLiveToolDiagnosticKind.InvalidRopeAnchor);
            }

            if (originState.IsSolid)
            {
                return Reject(CharacterLiveToolDiagnosticKind.BlockedRopeAnchor);
            }

            var input = new CharacterRopePlacementInput(
                session.ActorId, true, originCell, inventory.RopeCount, true);

            CharacterRopePlacementRequest placementRequest;
            CharacterRopeSpendRequest spendRequest;
            if (!CharacterRopePlacementPolicy.TryCreatePlacement(
                in input, out placementRequest, out spendRequest))
            {
                return Reject(CharacterLiveToolDiagnosticKind.InvalidRopeAnchor);
            }

            CharacterRunInventoryApplyResult spendResult =
                CharacterRunInventoryPolicy.ApplyRopeSpend(
                    in inventory, in spendRequest);

            if (!spendResult.Changed)
            {
                return Reject(CharacterLiveToolDiagnosticKind.NoRopeStock);
            }

            session.UpdateRunState(
                session.RunState.WithInventory(spendResult.NewState));

            lastRopeId++;
            List<CharacterRopeSegmentRequest> segments =
                CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                    lastRopeId, placementRequest.OriginCell, in settings, worldQuery);
            ropeSink.Enqueue(new CharacterLiveRopeCommand(placementRequest, segments));

            LastPlacementRequest = placementRequest;
            LastSegmentCount = segments.Count;
            ledger.TryMarkConsumed(CharacterLiveToolChannel.Rope, requestId);
            return Accept();
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
