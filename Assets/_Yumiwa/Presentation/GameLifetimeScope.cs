using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Yumiwa.Application;
using Yumiwa.Domain;
using Yumiwa.Infrastructure;

namespace Yumiwa.Presentation
{
    /// <summary>Composition root wiring one gameplay scene's movement and stamina graph. ADR-0018: now owns ItemData mapping for IConfigProvider decoupling.</summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private MovementConfig movementConfig;
        [SerializeField] private StaminaConfig staminaConfig;
        [SerializeField] private CensorEffectConfig censorConfig;
        [SerializeField] private InspectionConfig inspectionConfig;
        [SerializeField] private InputActionAsset inputAsset;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private FlashlightConfig flashlightConfig;
        [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

        // Explicit presenter references — replaces forbidden FindObject* runtime discovery (PATTERN-001).
        [SerializeField] private List<GameObject> presenterRoots = new List<GameObject>();
        [SerializeField] private MissionSequenceAsset missionSequence;
        // P2 data-driven puzzle/trap (DATA-001) — assign via inspector; fallback hardcode only if list empty
        [SerializeField] private List<PuzzleDefinitionAsset> puzzleAssets = new List<PuzzleDefinitionAsset>();
        [SerializeField] private List<TrapDefinitionAsset> trapAssets = new List<TrapDefinitionAsset>();
        // Dialogue/subtitle data (plan.json) — assign via inspector; empty lists mean a silent service
        [SerializeField] private List<DialogueLineAsset> dialogueLineAssets = new List<DialogueLineAsset>();
        // Act 1 data (B1-03/B1-04) — assign Act1 assets via inspector; fallback B1 consts only if unassigned
        [SerializeField] private List<ReconstructionCaseAsset> reconstructionAssets = new List<ReconstructionCaseAsset>();
        [SerializeField] private Act1DefinitionAsset act1Definition;
        [SerializeField] private List<SpeakerAsset> speakerAssets = new List<SpeakerAsset>();
        [SerializeField] private List<DialogueSequenceAsset> dialogueSequenceAssets = new List<DialogueSequenceAsset>();

        protected override void Awake()
        {
            UnityEngine.Application.runInBackground = true;
            if (presenterRoots != null && presenterRoots.Count > 0)
            {
                autoInjectGameObjects ??= new List<GameObject>();
                foreach (var go in presenterRoots)
                    if (go != null && !autoInjectGameObjects.Contains(go))
                        autoInjectGameObjects.Add(go);
            }
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (movementConfig == null || staminaConfig == null || inputAsset == null || playerRoot == null)
                throw new System.InvalidOperationException(
                    "[Yumiwa] GameLifetimeScope references unassigned. Run Yumiwa/Create Movement Test Scene.");
            builder.RegisterInstance(movementConfig);
            builder.RegisterInstance(staminaConfig);
            if (censorConfig != null)
                builder.RegisterInstance(censorConfig);
            builder.RegisterInstance(inputAsset);
            builder.RegisterInstance(itemDatabase ?? new List<ItemData>());

            builder.Register<IConfigProvider>(c =>
            {
                var items = c.Resolve<List<ItemData>>();
                return new DirectConfigProvider(movementConfig, staminaConfig, censorConfig, inspectionConfig, items);
            }, Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetMovement(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetStamina(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetCensor(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetInspection(), Lifetime.Singleton);

            // Core buses first (needed by everything)
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<ITimeSource, UnityTimeSource>(Lifetime.Singleton);
            builder.Register(c =>
            {
                var s = c.Resolve<StaminaConfigData>();
                return new StaminaPool(s.MaxStamina, s.RegenPerSecond, s.RegenDelaySeconds);
            }, Lifetime.Singleton);
            builder.Register<IStaminaService, StaminaService>(Lifetime.Singleton);
            builder.Register<ICensorService, CensorService>(Lifetime.Singleton);
            builder.Register<PlayerMovementState>(Lifetime.Singleton);
            builder.Register<WeaponState>(_ => new WeaponState(7), Lifetime.Singleton); // <!-- TBD#6 -->
            builder.Register<IAmmoReadModel>(c => c.Resolve<WeaponState>(), Lifetime.Singleton);
            builder.Register<CombatService>(Lifetime.Singleton);
            builder.Register<IMovementStateMachine>(c => new MovementStateMachine(
                c.Resolve<PlayerMovementState>(),
                c.Resolve<IStaminaService>(),
                c.Resolve<IAmmoReadModel>(),
                c.Resolve<IEventBus>(),
                c.Resolve<MovementConfigData>()), Lifetime.Singleton);
            builder.Register<ITargetingService, TargetingService>(Lifetime.Singleton);
            builder.Register<IPlayerInput>(_ => new NewInputAdapter(inputAsset), Lifetime.Singleton);
            builder.Register(_ => new InspectionInputHandler(inputAsset), Lifetime.Singleton);
            var waterSensor = new WaterSensor();
            builder.RegisterInstance(waterSensor);
            builder.RegisterInstance<IWaterProvider>(waterSensor);
            builder.Register<IHealthService>(c => new HealthService(bus: c.Resolve<IEventBus>()), Lifetime.Singleton);
            builder.Register<IFlashlightService>(c =>
            {
                if (flashlightConfig != null) return flashlightConfig.CreateService(c.Resolve<IEventBus>());
                return new FlashlightService(c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            builder.RegisterInstance(new InspectionProgress());
            // Player settings (plan.json persistence): versioned local file, fallback Defaults; independent from CheckpointData
            builder.Register<ISettingsRepository>(_ => new JsonFileSettingsRepository(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<ISettingsRepository>().LoadOrDefaults(), Lifetime.Singleton);

            // ADR-0012: anti soft-lock recovery point
            builder.Register<IRecoveryPoint>(c => new AutoReturnService(c.Resolve<IEventBus>()), Lifetime.Singleton);
            builder.Register<IInventoryService>(c => new PhysicalInventoryService(c.Resolve<IEventBus>(), c.Resolve<IRecoveryPoint>()), Lifetime.Singleton);
            // Evidence-aware inspection needs IEvidenceLog — register before session
            builder.Register<IEvidenceLog, EvidenceLogService>(Lifetime.Singleton);
            builder.Register<InspectionSessionService>(c => new InspectionSessionService(c.Resolve<IInventoryService>(), c.Resolve<IEventBus>(), c.Resolve<IEvidenceLog>()), Lifetime.Singleton);
            // Save split: chapter vs house milestones (GDD §52) — use factory to avoid string param DI
            builder.Register<ISaveRepository>(_ => new JsonFileSaveRepository(), Lifetime.Singleton);
            builder.Register<IObjectiveService, ObjectiveService>(Lifetime.Singleton);
            // Mission sequence data-driven (ADR-0003) — register only if assigned, else empty sequence
            if (missionSequence != null)
                builder.RegisterInstance(missionSequence);
            builder.Register<MissionFlowService>(c =>
            {
                var seq = missionSequence != null ? missionSequence.GetDefinitions() : System.Array.Empty<MissionDefinition>();
                return new MissionFlowService(seq, c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            // Puzzle + Trap prototype — data-driven via ScriptableObject (P2 DATA-001), fallback hardcode only if no assets assigned
            builder.Register<PuzzleService>(c =>
            {
                PuzzleDefinition[] defs;
                if (puzzleAssets != null && puzzleAssets.Count > 0)
                {
                    var list = new List<PuzzleDefinition>();
                    foreach (var a in puzzleAssets) if (a != null) list.Add(a.ToDefinition());
                    defs = list.ToArray();
                }
                else
                {
                    // Fallback vertical-slice single puzzle — designer should create PuzzleDefinitionAsset in Configs/
                    Debug.LogWarning("[Yumiwa] puzzleAssets empty — using fallback PuzzleDefinition (2347). Create PuzzleDefinitionAsset to make data-driven per docs/09:32.");
                    defs = new[] { new PuzzleDefinition("puzzle_alley_code","Alley Code","Buka gembok gang","watch_bloody","crowbar","2347") };
                }
                return new PuzzleService(defs, c.Resolve<IEvidenceLog>(), c.Resolve<IInventoryService>(), c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            builder.Register<ICheckpointService>(c => new CheckpointService(c.Resolve<IEventBus>(), c.Resolve<ISaveRepository>(), c.Resolve<IRecoveryPoint>(), c.Resolve<InspectionSessionService>(), c.Resolve<IInventoryService>(), c.Resolve<IEvidenceLog>(), c.Resolve<PuzzleService>(), c.Resolve<MissionFlowService>()), Lifetime.Singleton);
            builder.Register<TrapService>(c =>
            {
                TrapDefinition[] defs;
                if (trapAssets != null && trapAssets.Count > 0)
                {
                    var list = new List<TrapDefinition>();
                    foreach (var a in trapAssets) if (a != null) list.Add(a.ToDefinition());
                    defs = list.ToArray();
                }
                else
                {
                    defs = new[] { new TrapDefinition("trap_board_01","Papan Berpaku",25f,"trap_click","trap_board",true) };
                }
                return new TrapService(defs, c.Resolve<IHealthService>(), c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            // Dialogue/subtitle queue (plan.json) — data-driven via assets, transient across reloads (SAVE-001 untouched)
            builder.Register<DialogueQueueService>(c =>
            {
                var lineDefs = new List<DialogueLine>();
                if (dialogueLineAssets != null)
                    foreach (var a in dialogueLineAssets)
                        if (a != null)
                        {
                            var d = a.ToDefinition();
                            if (d.IsValid) lineDefs.Add(d);
                        }
                var speakerDefs = new List<Speaker>();
                if (speakerAssets != null)
                    foreach (var a in speakerAssets)
                        if (a != null)
                        {
                            var d = a.ToDefinition();
                            if (d.IsValid) speakerDefs.Add(d);
                        }
                var seqDefs = new List<DialogueSequence>();
                if (dialogueSequenceAssets != null)
                    foreach (var a in dialogueSequenceAssets)
                        if (a != null)
                        {
                            var d = a.ToDefinition();
                            if (d.IsValid) seqDefs.Add(d);
                        }
                var svc = new DialogueQueueService(lineDefs, speakerDefs, seqDefs, c.Resolve<IEventBus>(), c.Resolve<ITimeSource>());
                svc.SetReadingSpeedMultiplier(c.Resolve<SettingsData>().ReadingSpeed.ToMultiplier());
                return svc;
            }, Lifetime.Singleton);
            // Door scene portals (docs/05 Bab 4-5 stage changes, docs/09 stage-based):
            // guarded Single-scene loads; fade overlay itself lives per-scene (ScreenFader).
            builder.Register<ISceneLoader>(_ => new UnitySceneLoader(), Lifetime.Singleton);
            builder.Register<ISceneTransitionService, SceneTransitionService>(Lifetime.Singleton);
        }

        [ContextMenu("Yumiwa/Apply Movement Config Now")]
        public void ApplyMovementConfigNow()
        {
            if (Container == null)
            {
                UnityEngine.Debug.LogWarning("[Yumiwa] APPLY_BEFORE_CONTAINER_READY");
                return;
            }
            var movement = Container.Resolve<IConfigProvider>().GetMovement();
            Container.Resolve<IEventBus>().Publish(new MovementConfigReloadedEvent(movement));
            UnityEngine.Debug.Log(
                $"[Yumiwa] Movement config reloaded (walk {movement.WalkSpeed}, " +
                $"sprint x{movement.SprintMultiplier}).");
        }

        public void WireSceneReferences(
            MovementConfig movement, StaminaConfig stamina, InputActionAsset asset, Transform player)
        {
            movementConfig = movement;
            staminaConfig = stamina;
            inputAsset = asset;
            playerRoot = player;
        }

        public void WireCensorConfig(CensorEffectConfig censor)
        {
            censorConfig = censor;
        }

        public void WireInspectionConfig(InspectionConfig inspection)
        {
            inspectionConfig = inspection;
        }

        public void WireItemDatabase(List<ItemData> items) => itemDatabase = items;
    }
}
