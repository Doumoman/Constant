#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Interaction.Carry;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using StarNight.Map.Placement;
using StarNight.Tools.HookLauncher;
using StarNight.Tools.Rope;
using StarNight.Tools.Watering;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    internal static class MapElementPrefabAssembler
    {
        public static GameObject CreateSourceHierarchy(MapElementDefinition definition)
        {
            var root = new GameObject($"{definition.ElementId}_Source");
            root.transform.localScale = Vector3.one;

            var visualRoot = CreateChild(root.transform, "VisualRoot");
            var physicsRoot = CreateChild(root.transform, "PhysicsRoot");
            var triggerRoot = CreateChild(root.transform, "TriggerRoot");
            var pathRoot = CreateChild(root.transform, "PathRoot");
            CreateChild(root.transform, "SignalPortRoot");
            CreateChild(root.transform, "AudioRoot");
            CreateChild(root.transform, "DebugRoot");

            BuildVisual(definition.VisualProfile, visualRoot);
            BuildColliders(definition.CollisionProfile, physicsRoot, triggerRoot);
            BuildPath(definition.BehaviorProfile?.Path, pathRoot);
            return root;
        }

        public static void AddRuntimeContract(
            GameObject root,
            MapElementDefinition bakedDefinition)
        {
            root.name = bakedDefinition.ElementId;
            root.transform.localScale = Vector3.one;

            var occupier = GetOrAdd<GridOccupier>(root);
            occupier.Configure(
                Vector2Int.zero,
                CloneFootprint(bakedDefinition.Footprint),
                GetOccupancyLayer(bakedDefinition.Category));

            var runtimeId = GetOrAdd<ElementRuntimeId>(root);
            var stateMachine = GetOrAdd<ElementStateMachine>(root);
            var instance = GetOrAdd<MapElementInstance>(root);
            GetOrAdd<MapElementResettable>(root);
            CommonElementDriver commonDriver = null;
            MaruElementDriver maruDriver = null;
            MoonElementDriver moonDriver = null;
            BridgeElementDriver bridgeDriver = null;
            PalaceElementDriver palaceDriver = null;
            PostElementDriver postDriver = null;
            SunElementDriver sunDriver = null;
            PolarisElementDriver polarisDriver = null;
            ToolReactionReceiver toolReceiver = null;
            if (bakedDefinition.CommonProfile != null &&
                bakedDefinition.CommonProfile.Kind != CommonElementKind.None)
            {
                commonDriver = GetOrAdd<CommonElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }
            if (bakedDefinition.MaruProfile != null &&
                bakedDefinition.MaruProfile.Kind != MaruElementKind.None)
            {
                maruDriver = GetOrAdd<MaruElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }
            if (bakedDefinition.MoonProfile != null &&
                bakedDefinition.MoonProfile.Kind != MoonElementKind.None)
            {
                moonDriver = GetOrAdd<MoonElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }
            if (bakedDefinition.BridgeProfile != null &&
                bakedDefinition.BridgeProfile.Kind != BridgeElementKind.None)
            {
                bridgeDriver = GetOrAdd<BridgeElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }
            if (bakedDefinition.PalaceProfile != null &&
                bakedDefinition.PalaceProfile.Kind != PalaceElementKind.None)
            {
                palaceDriver = GetOrAdd<PalaceElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }
            if (bakedDefinition.PostProfile != null &&
                bakedDefinition.PostProfile.Kind != PostElementKind.None)
            {
                postDriver = GetOrAdd<PostElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }
            if (bakedDefinition.SunProfile != null &&
                bakedDefinition.SunProfile.Kind != SunElementKind.None)
            {
                sunDriver = GetOrAdd<SunElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }
            if (bakedDefinition.PolarisProfile != null &&
                bakedDefinition.PolarisProfile.Kind != PolarisElementKind.None)
            {
                polarisDriver = GetOrAdd<PolarisElementDriver>(root);
                toolReceiver = GetOrAdd<ToolReactionReceiver>(root);
            }

            var visualRoot = FindOrCreate(root.transform, "VisualRoot");
            var physicsRoot = FindOrCreate(root.transform, "PhysicsRoot");
            var triggerRoot = FindOrCreate(root.transform, "TriggerRoot");
            FindOrCreate(root.transform, "PathRoot");
            FindOrCreate(root.transform, "SignalPortRoot");
            FindOrCreate(root.transform, "AudioRoot");
            FindOrCreate(root.transform, "DebugRoot");

            var path = bakedDefinition.BehaviorProfile?.Path;
            if (path != null && path.Nodes != null && path.Nodes.Count > 1)
            {
                var body = GetOrAdd<Rigidbody2D>(root);
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (bakedDefinition.CommonProfile != null &&
                bakedDefinition.CommonProfile.Kind == CommonElementKind.FallingStone)
            {
                var fallingBody = GetOrAdd<Rigidbody2D>(root);
                fallingBody.bodyType = RigidbodyType2D.Kinematic;
                fallingBody.gravityScale = 0f;
                fallingBody.freezeRotation = true;
                fallingBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            if (bakedDefinition.CommonProfile != null &&
                (bakedDefinition.CommonProfile.Kind == CommonElementKind.PendulumBall ||
                 bakedDefinition.CommonProfile.Kind == CommonElementKind.RollingBoulder))
            {
                var dynamicBody = GetOrAdd<Rigidbody2D>(root);
                dynamicBody.bodyType = RigidbodyType2D.Kinematic;
                dynamicBody.gravityScale = 0f;
                dynamicBody.freezeRotation =
                    bakedDefinition.CommonProfile.Kind != CommonElementKind.RollingBoulder;
                dynamicBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                dynamicBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (bakedDefinition.CommonProfile != null &&
                bakedDefinition.CommonProfile.Kind == CommonElementKind.OneWayPlatform)
            {
                ConfigureOneWayEffectors(physicsRoot);
            }

            if (bakedDefinition.MaruProfile != null &&
                bakedDefinition.MaruProfile.Kind == MaruElementKind.CollarFragment)
            {
                var carryBody = GetOrAdd<Rigidbody2D>(root);
                carryBody.bodyType = RigidbodyType2D.Dynamic;
                carryBody.gravityScale = 2f;
                carryBody.mass = 2f;
                carryBody.freezeRotation = true;
                carryBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            if (bakedDefinition.MoonProfile != null)
            {
                var moonKind = bakedDefinition.MoonProfile.Kind;
                if (moonKind == MoonElementKind.MoonIronBall ||
                    moonKind == MoonElementKind.FallingMortar ||
                    moonKind == MoonElementKind.CraterSlab ||
                    moonKind == MoonElementKind.MillShaft)
                {
                    var moonBody = GetOrAdd<Rigidbody2D>(root);
                    moonBody.bodyType = RigidbodyType2D.Kinematic;
                    moonBody.gravityScale = 0f;
                    moonBody.freezeRotation = moonKind != MoonElementKind.CraterSlab;
                    moonBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    moonBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
            }

            if (bakedDefinition.BridgeProfile != null)
            {
                var bridgeKind = bakedDefinition.BridgeProfile.Kind;
                if (bridgeKind == BridgeElementKind.KnotPulley ||
                    bridgeKind == BridgeElementKind.ThreadBlade ||
                    bridgeKind == BridgeElementKind.MagpiePlatform)
                {
                    var bridgeBody = GetOrAdd<Rigidbody2D>(root);
                    bridgeBody.bodyType = RigidbodyType2D.Kinematic;
                    bridgeBody.gravityScale = 0f;
                    bridgeBody.freezeRotation = true;
                    bridgeBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    bridgeBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
            }

            if (bakedDefinition.PalaceProfile != null)
            {
                var palaceKind = bakedDefinition.PalaceProfile.Kind;
                if (palaceKind == PalaceElementKind.SluiceGate ||
                    palaceKind == PalaceElementKind.TurtlePlatform ||
                    palaceKind == PalaceElementKind.ClamBounce)
                {
                    var palaceBody = GetOrAdd<Rigidbody2D>(root);
                    palaceBody.bodyType = RigidbodyType2D.Kinematic;
                    palaceBody.gravityScale = 0f;
                    palaceBody.freezeRotation = true;
                    palaceBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    palaceBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
            }

            if (bakedDefinition.PostProfile != null)
            {
                var postKind = bakedDefinition.PostProfile.Kind;
                if (postKind == PostElementKind.ReturnStamp ||
                    postKind == PostElementKind.SortingArm)
                {
                    var postBody = GetOrAdd<Rigidbody2D>(root);
                    postBody.bodyType = RigidbodyType2D.Kinematic;
                    postBody.gravityScale = 0f;
                    postBody.freezeRotation = true;
                    postBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    postBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
            }

            if (bakedDefinition.SunProfile != null &&
                bakedDefinition.SunProfile.Kind == SunElementKind.SunflowerPlatform)
            {
                var sunBody = GetOrAdd<Rigidbody2D>(root);
                sunBody.bodyType = RigidbodyType2D.Kinematic;
                sunBody.gravityScale = 0f;
                sunBody.freezeRotation = true;
                sunBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                sunBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (bakedDefinition.PolarisProfile != null)
            {
                var polarisKind = bakedDefinition.PolarisProfile.Kind;
                if (polarisKind == PolarisElementKind.OrbitPlatform)
                {
                    var orbitBody = GetOrAdd<Rigidbody2D>(root);
                    orbitBody.bodyType = RigidbodyType2D.Kinematic;
                    orbitBody.gravityScale = 0f;
                    orbitBody.freezeRotation = true;
                    orbitBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    orbitBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
                else if (polarisKind == PolarisElementKind.StarWeight)
                {
                    var starBody = GetOrAdd<Rigidbody2D>(root);
                    starBody.bodyType = RigidbodyType2D.Dynamic;
                    starBody.gravityScale = 0f;
                    starBody.mass = Mathf.Max(1f, bakedDefinition.PolarisProfile.Mass);
                    starBody.freezeRotation = true;
                    starBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    starBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
            }

            instance.BindAuthoringRoots(visualRoot, physicsRoot, triggerRoot);
            instance.Configure(bakedDefinition, null, "__BAKED_TEMPLATE__");

            if (commonDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<CommonElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(commonDriver);
                    EditorUtility.SetDirty(relay);
                }

                commonDriver.Rebind();
                EditorUtility.SetDirty(commonDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            ConfigureCommonToolContracts(root, bakedDefinition);


            if (maruDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<MaruElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(maruDriver);
                    EditorUtility.SetDirty(relay);
                }

                maruDriver.Rebind();
                EditorUtility.SetDirty(maruDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            if (moonDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<MoonElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(moonDriver);
                    EditorUtility.SetDirty(relay);
                }

                moonDriver.Rebind();
                EditorUtility.SetDirty(moonDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            if (bridgeDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<BridgeElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(bridgeDriver);
                    EditorUtility.SetDirty(relay);
                }

                bridgeDriver.Rebind();
                EditorUtility.SetDirty(bridgeDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            if (palaceDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<PalaceElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(palaceDriver);
                    EditorUtility.SetDirty(relay);
                }

                palaceDriver.Rebind();
                EditorUtility.SetDirty(palaceDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            if (postDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<PostElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(postDriver);
                    EditorUtility.SetDirty(relay);
                }

                postDriver.Rebind();
                EditorUtility.SetDirty(postDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            if (sunDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<SunElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(sunDriver);
                    EditorUtility.SetDirty(relay);
                }

                sunDriver.Rebind();
                EditorUtility.SetDirty(sunDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            if (polarisDriver != null)
            {
                var runtimeColliders = root.GetComponentsInChildren<Collider2D>(true);
                for (var index = 0; index < runtimeColliders.Length; index++)
                {
                    var relay = GetOrAdd<PolarisElementPhysicsRelay>(runtimeColliders[index].gameObject);
                    relay.Configure(polarisDriver);
                    EditorUtility.SetDirty(relay);
                }

                polarisDriver.Rebind();
                EditorUtility.SetDirty(polarisDriver);
                EditorUtility.SetDirty(toolReceiver);
            }

            ConfigureRegionalToolContracts(root, bakedDefinition);

            var runtimeIdData = new SerializedObject(runtimeId);
            runtimeIdData.FindProperty("value").stringValue = string.Empty;
            runtimeIdData.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(occupier);
            EditorUtility.SetDirty(runtimeId);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(instance);
        }

        private static void ConfigureCommonToolContracts(
            GameObject root,
            MapElementDefinition definition)
        {
            var profile = definition.CommonProfile;
            if (profile == null || profile.Kind == CommonElementKind.None)
            {
                return;
            }

            var body = root.GetComponent<Rigidbody2D>();
            var hookResponse = HookResponse.Reject;
            switch (profile.Kind)
            {
                case CommonElementKind.MovingPlatform:
                case CommonElementKind.HookAnchor:
                    hookResponse = HookResponse.PullPlayerToTarget;
                    break;
                case CommonElementKind.PendulumBall:
                case CommonElementKind.RollingBoulder:
                    hookResponse = HookResponse.PullToPlayer;
                    break;
                case CommonElementKind.Lever:
                case CommonElementKind.Crusher:
                case CommonElementKind.PulleyLift:
                    hookResponse = HookResponse.Trigger;
                    break;
            }

            if (hookResponse != HookResponse.Reject)
            {
                var hookTarget = GetOrAdd<HookTarget>(root);
                hookTarget.ConfigureForTests(hookResponse, body);
                EditorUtility.SetDirty(hookTarget);
            }

            if (profile.Kind == CommonElementKind.RopeAnchor)
            {
                EditorUtility.SetDirty(GetOrAdd<RopeAnchorMarker>(root));
            }

            if (profile.Kind != CommonElementKind.Lever &&
                profile.Kind != CommonElementKind.ExitGuideLantern &&
                profile.Kind != CommonElementKind.WaterVent)
            {
                return;
            }

            var interactionRoot = FindOrCreate(root.transform, "InteractionRoot");
            var interactionLayer = LayerMask.NameToLayer("Interaction");
            if (interactionLayer >= 0)
            {
                interactionRoot.gameObject.layer = interactionLayer;
            }

            var trigger = GetOrAdd<BoxCollider2D>(interactionRoot.gameObject);
            trigger.isTrigger = true;
            trigger.size = new Vector2(
                Mathf.Max(1f, definition.Footprint.BoundsSize.x) + 0.2f,
                Mathf.Max(1f, definition.Footprint.BoundsSize.y) + 0.2f);
            trigger.offset = definition.VisualProfile.VisualOffsetCells;

            var candidate = GetOrAdd<InteractionCandidate>(interactionRoot.gameObject);
            candidate.ConfigureForTests(
                profile.Kind == CommonElementKind.WaterVent
                    ? InteractionTargetKind.RequiredHandSlotReceiver
                    : InteractionTargetKind.Mechanism,
                definition.ElementId.GetHashCode() & int.MaxValue);

            if (profile.Kind == CommonElementKind.WaterVent)
            {
                EditorUtility.SetDirty(GetOrAdd<ToolRechargeReceiver>(interactionRoot.gameObject));
            }
            else
            {
                EditorUtility.SetDirty(GetOrAdd<MapElementWorldInteractionReceiver>(
                    interactionRoot.gameObject));
            }

            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(candidate);
        }

        private static void ConfigureRegionalToolContracts(
            GameObject root,
            MapElementDefinition definition)
        {
            var hookResponse = ResolveRegionalHookResponse(definition);
            if (hookResponse != HookResponse.Reject)
            {
                var hookTarget = GetOrAdd<HookTarget>(root);
                hookTarget.ConfigureForTests(hookResponse, root.GetComponent<Rigidbody2D>());
                EditorUtility.SetDirty(hookTarget);
            }

            var interactionMode = ResolveRegionalInteractionMode(definition);
            if (interactionMode == RegionalInteractionMode.None)
            {
                return;
            }

            var interactionRoot = FindOrCreate(root.transform, "InteractionRoot");
            var interactionLayer = LayerMask.NameToLayer("Interaction");
            if (interactionLayer >= 0)
            {
                interactionRoot.gameObject.layer = interactionLayer;
            }

            var trigger = GetOrAdd<BoxCollider2D>(interactionRoot.gameObject);
            trigger.isTrigger = true;
            trigger.size = new Vector2(
                Mathf.Max(1f, definition.Footprint.BoundsSize.x) + 0.2f,
                Mathf.Max(1f, definition.Footprint.BoundsSize.y) + 0.2f);
            trigger.offset = definition.VisualProfile.VisualOffsetCells;

            var candidate = GetOrAdd<InteractionCandidate>(interactionRoot.gameObject);
            candidate.ConfigureForTests(
                interactionMode == RegionalInteractionMode.World
                    ? InteractionTargetKind.Mechanism
                    : InteractionTargetKind.RequiredHandSlotReceiver,
                definition.ElementId.GetHashCode() & int.MaxValue);

            switch (interactionMode)
            {
                case RegionalInteractionMode.Context:
                    var contextReceiver = GetOrAdd<MapElementContextReceiver>(interactionRoot.gameObject);
                    contextReceiver.ConfigureForTests(
                        definition.PalaceProfile != null &&
                        definition.PalaceProfile.Kind == PalaceElementKind.WaterMirrorWall);
                    EditorUtility.SetDirty(contextReceiver);
                    break;
                case RegionalInteractionMode.World:
                    EditorUtility.SetDirty(GetOrAdd<MapElementWorldInteractionReceiver>(interactionRoot.gameObject));
                    break;
                case RegionalInteractionMode.Recharge:
                    EditorUtility.SetDirty(GetOrAdd<ToolRechargeReceiver>(interactionRoot.gameObject));
                    break;
            }

            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(candidate);
        }

        private static HookResponse ResolveRegionalHookResponse(MapElementDefinition definition)
        {
            if (definition.MoonProfile != null)
            {
                switch (definition.MoonProfile.Kind)
                {
                    case MoonElementKind.MoonIronBall:
                        return HookResponse.PullToPlayer;
                    case MoonElementKind.CassiaRoot:
                    case MoonElementKind.MillShaft:
                        return HookResponse.Trigger;
                }
            }

            if (definition.BridgeProfile != null &&
                definition.BridgeProfile.Kind == BridgeElementKind.KnotPulley)
            {
                return HookResponse.Trigger;
            }

            if (definition.PalaceProfile != null &&
                (definition.PalaceProfile.Kind == PalaceElementKind.SluiceGate ||
                 definition.PalaceProfile.Kind == PalaceElementKind.DrainGrate))
            {
                return HookResponse.Trigger;
            }

            if (definition.PostProfile != null &&
                definition.PostProfile.Kind == PostElementKind.ReturnStamp)
            {
                return HookResponse.Trigger;
            }

            if (definition.SunProfile != null &&
                definition.SunProfile.Kind == SunElementKind.GrowthVine)
            {
                return HookResponse.Trigger;
            }

            if (definition.PolarisProfile != null)
            {
                switch (definition.PolarisProfile.Kind)
                {
                    case PolarisElementKind.StarWeight:
                        return HookResponse.PullToPlayer;
                    case PolarisElementKind.GravityDial:
                        return HookResponse.Trigger;
                }
            }

            return HookResponse.Reject;
        }

        private static RegionalInteractionMode ResolveRegionalInteractionMode(
            MapElementDefinition definition)
        {
            if (definition.MoonProfile != null &&
                definition.MoonProfile.Kind == MoonElementKind.MedicineMortar)
            {
                return RegionalInteractionMode.Context;
            }

            if (definition.BridgeProfile != null &&
                definition.BridgeProfile.Kind == BridgeElementKind.Nest)
            {
                return RegionalInteractionMode.Context;
            }

            if (definition.PalaceProfile != null)
            {
                if (definition.PalaceProfile.Kind == PalaceElementKind.WaterMirrorWall)
                {
                    return RegionalInteractionMode.Context;
                }
                if (definition.PalaceProfile.Kind == PalaceElementKind.DragonGateWaterfall)
                {
                    return RegionalInteractionMode.Recharge;
                }
            }

            if (definition.PostProfile != null &&
                (definition.PostProfile.Kind == PostElementKind.ParcelLauncher ||
                 definition.PostProfile.Kind == PostElementKind.MailTube ||
                 definition.PostProfile.Kind == PostElementKind.ExpressTube))
            {
                return RegionalInteractionMode.Context;
            }

            if (definition.SunProfile != null)
            {
                if (definition.SunProfile.Kind == SunElementKind.CrowPerch)
                {
                    return RegionalInteractionMode.Context;
                }
                if (definition.SunProfile.Kind == SunElementKind.DewDrop)
                {
                    return RegionalInteractionMode.Recharge;
                }
            }

            if (definition.PolarisProfile != null)
            {
                switch (definition.PolarisProfile.Kind)
                {
                    case PolarisElementKind.ConstellationBridge:
                        return RegionalInteractionMode.Context;
                    case PolarisElementKind.StarWeight:
                    case PolarisElementKind.GravityDial:
                    case PolarisElementKind.MemoryBell:
                        return RegionalInteractionMode.World;
                }
            }

            return RegionalInteractionMode.None;
        }

        private enum RegionalInteractionMode
        {
            None,
            Context,
            World,
            Recharge,
        }

        private static void ConfigureOneWayEffectors(Transform physicsRoot)
        {
            var colliders = physicsRoot.GetComponentsInChildren<Collider2D>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] == null || colliders[index].isTrigger)
                {
                    continue;
                }

                colliders[index].usedByEffector = true;
                var effector = GetOrAdd<PlatformEffector2D>(colliders[index].gameObject);
                effector.useOneWay = true;
                effector.surfaceArc = 165f;
                EditorUtility.SetDirty(colliders[index]);
                EditorUtility.SetDirty(effector);
            }
        }

        private static void BuildVisual(ElementVisualProfile profile, Transform visualRoot)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.RenderMode == ElementVisualRenderMode.AnimatorPrefab &&
                profile.AnimatorPrefab != null)
            {
                var animatorObject = PrefabUtility.InstantiatePrefab(profile.AnimatorPrefab) as GameObject;
                if (animatorObject != null)
                {
                    animatorObject.name = "AnimatorVisual";
                    animatorObject.transform.SetParent(visualRoot, false);
                    animatorObject.transform.localPosition = profile.VisualOffsetCells;
                    animatorObject.transform.localScale = Vector3.one;
                }
                return;
            }

            var visual = new GameObject("SpriteVisual");
            visual.transform.SetParent(visualRoot, false);
            visual.transform.localPosition = profile.VisualOffsetCells;
            visual.transform.localScale = Vector3.one;
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolveSprite(profile);
            renderer.color = profile.Tint;
            renderer.flipX = profile.FlipX;
            renderer.flipY = profile.FlipY;
            renderer.sortingLayerName = profile.SortingLayerName;
            renderer.sortingOrder = profile.SortingOrder;
            renderer.sharedMaterial = profile.MaterialOverride;

            if (profile.RenderMode == ElementVisualRenderMode.TiledSprite)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = MaxSize(profile.VisualSizeCells);
            }
            else if (profile.RenderMode == ElementVisualRenderMode.SegmentedSprite)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = MaxSize(profile.VisualSizeCells);
            }
        }

        private static void BuildColliders(
            ElementCollisionProfile profile,
            Transform physicsRoot,
            Transform triggerRoot)
        {
            if (profile == null)
            {
                return;
            }

            BuildShapeList(profile.SolidShapes, physicsRoot, false, profile.PhysicsMaterial);
            BuildShapeList(profile.TriggerShapes, triggerRoot, true, null);
        }

        private static void BuildShapeList(
            IReadOnlyList<SerializedColliderShape> shapes,
            Transform root,
            bool isTrigger,
            PhysicsMaterial2D material)
        {
            if (shapes == null)
            {
                return;
            }

            for (var index = 0; index < shapes.Count; index++)
            {
                var shape = shapes[index];
                if (shape == null)
                {
                    continue;
                }

                var shapeObject = new GameObject($"{(isTrigger ? "Trigger" : "Solid")}_{index:00}");
                shapeObject.transform.SetParent(root, false);
                shapeObject.transform.localScale = Vector3.one;

                Collider2D collider;
                switch (shape.ShapeType)
                {
                    case SerializedColliderShapeType.Capsule:
                    {
                        var capsule = shapeObject.AddComponent<CapsuleCollider2D>();
                        capsule.offset = shape.OffsetCells;
                        capsule.size = MaxSize(shape.SizeCells);
                        capsule.direction = capsule.size.x >= capsule.size.y
                            ? CapsuleDirection2D.Horizontal
                            : CapsuleDirection2D.Vertical;
                        collider = capsule;
                        break;
                    }
                    case SerializedColliderShapeType.Polygon when shape.Points != null && shape.Points.Count >= 3:
                    {
                        var polygon = shapeObject.AddComponent<PolygonCollider2D>();
                        var points = new Vector2[shape.Points.Count];
                        for (var pointIndex = 0; pointIndex < points.Length; pointIndex++)
                        {
                            points[pointIndex] = shape.Points[pointIndex] + shape.OffsetCells;
                        }

                        polygon.points = points;
                        collider = polygon;
                        break;
                    }
                    default:
                    {
                        var box = shapeObject.AddComponent<BoxCollider2D>();
                        box.offset = shape.OffsetCells;
                        box.size = MaxSize(shape.SizeCells);
                        collider = box;
                        break;
                    }
                }

                collider.isTrigger = isTrigger;
                if (!isTrigger)
                {
                    collider.sharedMaterial = material;
                }
            }
        }

        private static void BuildPath(ElementPathDefinition path, Transform pathRoot)
        {
            if (path?.Nodes == null)
            {
                return;
            }

            for (var index = 0; index < path.Nodes.Count; index++)
            {
                var node = new GameObject($"Node_{index:00}").transform;
                node.SetParent(pathRoot, false);
                node.localPosition = path.Nodes[index];
                node.localScale = Vector3.one;
            }
        }

        private static Sprite ResolveSprite(ElementVisualProfile profile)
        {
            if (profile.SingleSprite != null)
            {
                return profile.SingleSprite;
            }
            if (profile.SegmentMiddle != null)
            {
                return profile.SegmentMiddle;
            }
            if (profile.SegmentStart != null)
            {
                return profile.SegmentStart;
            }
            return profile.SegmentEnd;
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            child.localScale = Vector3.one;
            return child;
        }

        private static Transform FindOrCreate(Transform parent, string childName)
        {
            return parent.Find(childName) ?? CreateChild(parent, childName);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static Vector2 MaxSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        }

        private static CellFootprint CloneFootprint(CellFootprint footprint)
        {
            return footprint == null
                ? new CellFootprint()
                : JsonUtility.FromJson<CellFootprint>(JsonUtility.ToJson(footprint));
        }

        private static OccupancyLayer GetOccupancyLayer(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Terrain:
                    return OccupancyLayer.Terrain;
                case ElementCategory.Hazard:
                    return OccupancyLayer.Hazard;
                case ElementCategory.Platform:
                    return OccupancyLayer.Dynamic;
                case ElementCategory.Decoration:
                    return OccupancyLayer.Decoration;
                case ElementCategory.Trigger:
                case ElementCategory.Control:
                case ElementCategory.Event:
                    return OccupancyLayer.Logic;
                default:
                    return OccupancyLayer.Fixture;
            }
        }
    }
}

#endif
