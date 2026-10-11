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
    /// <summary>Composition root wiring one gameplay scene's movement and stamina graph.</summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private MovementConfig movementConfig;
        [SerializeField] private StaminaConfig staminaConfig;
        [SerializeField] private CensorEffectConfig censorConfig;
        [SerializeField] private InspectionConfig inspectionConfig;
        [SerializeField] private InputActionAsset inputAsset;
        [SerializeField] private Transform playerRoot;

        protected override void Awake()
        {
            autoInjectGameObjects ??= new List<GameObject>();
            var player = FindFirstObjectByType<PlayerMovementPresenter>();
            if (player != null && !autoInjectGameObjects.Contains(player.gameObject))
                autoInjectGameObjects.Add(player.gameObject);
            var hud = FindFirstObjectByType<StaminaHudPresenter>();
            if (hud != null && !autoInjectGameObjects.Contains(hud.gameObject))
                autoInjectGameObjects.Add(hud.gameObject);
            var censor = FindFirstObjectByType<CensorEffectPresenter>();
            if (censor != null && !autoInjectGameObjects.Contains(censor.gameObject))
                autoInjectGameObjects.Add(censor.gameObject);
            foreach (var water in FindObjectsByType<WaterZone>(FindObjectsSortMode.None))
            {
                if (!autoInjectGameObjects.Contains(water.gameObject))
                    autoInjectGameObjects.Add(water.gameObject);
            }
            var inspection = FindFirstObjectByType<ItemInspectionController>();
            if (inspection != null && !autoInjectGameObjects.Contains(inspection.gameObject))
                autoInjectGameObjects.Add(inspection.gameObject);
            var interactor = FindFirstObjectByType<PlayerInteractor>();
            if (interactor != null && !autoInjectGameObjects.Contains(interactor.gameObject))
                autoInjectGameObjects.Add(interactor.gameObject);
            foreach (var behaviour in FindObjectsByType<ItemInspectionPresenter>(FindObjectsSortMode.None))
            {
                if (!autoInjectGameObjects.Contains(behaviour.gameObject))
                    autoInjectGameObjects.Add(behaviour.gameObject);
            }
            foreach (var behaviour in FindObjectsByType<ItemInspectionRotator>(FindObjectsSortMode.None))
            {
                if (!autoInjectGameObjects.Contains(behaviour.gameObject))
                    autoInjectGameObjects.Add(behaviour.gameObject);
            }
            foreach (var behaviour in FindObjectsByType<ItemInspectionUI>(FindObjectsSortMode.None))
            {
                if (!autoInjectGameObjects.Contains(behaviour.gameObject))
                    autoInjectGameObjects.Add(behaviour.gameObject);
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

            builder.Register<IConfigProvider, DirectConfigProvider>(Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetMovement(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetStamina(), Lifetime.Singleton);
            builder.RegisterInstance(inspectionConfig != null
                ? inspectionConfig.ToData()
                : new InspectionTuningData());
            builder.Register<IInventoryService>(_ => new PhysicalInventoryService(), Lifetime.Singleton);
            builder.Register<InspectionSessionService>(Lifetime.Singleton);
            builder.Register<IEvidenceLog, EvidenceLogService>(Lifetime.Singleton);
            builder.Register<ICheckpointService, CheckpointService>(Lifetime.Singleton);
            builder.Register<IObjectiveService, ObjectiveService>(Lifetime.Singleton);

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
            builder.Register<IMovementStateMachine>(c => new MovementStateMachine(
                c.Resolve<PlayerMovementState>(),
                c.Resolve<IStaminaService>(),
                c.Resolve<IAmmoReadModel>(),
                c.Resolve<IEventBus>()), Lifetime.Singleton);
            builder.Register<ITargetingService, TargetingService>(Lifetime.Singleton);
            builder.Register<IAmmoReadModel, PlaceholderAmmoReadModel>(Lifetime.Singleton);
            builder.Register<IPlayerInput>(_ => new NewInputAdapter(inputAsset), Lifetime.Singleton);
            builder.Register(_ => new InspectionInputHandler(inputAsset), Lifetime.Singleton);
            var waterSensor = new WaterSensor();
            builder.RegisterInstance(waterSensor);
            builder.RegisterInstance<IWaterProvider>(waterSensor);

            builder.Register<IHealthService, HealthService>(Lifetime.Singleton);
        }

        /// <summary>
        /// Dev-time tuning: publishes the current SO values to all consumers.
        /// Play Mode -&gt; select GameScope -&gt; gear icon -&gt; "Yumiwa/Apply Movement Config Now".
        /// </summary>
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

        /// <summary>Editor scene wiring; called by MovementTestSceneSetup (direct assignment).</summary>
        public void WireSceneReferences(
            MovementConfig movement, StaminaConfig stamina, InputActionAsset asset, Transform player)
        {
            movementConfig = movement;
            staminaConfig = stamina;
            inputAsset = asset;
            playerRoot = player;
        }

        /// <summary>Optional censor config wiring; presenter falls back to defaults when null.</summary>
        public void WireCensorConfig(CensorEffectConfig censor)
        {
            censorConfig = censor;
        }

        /// <summary>Optional inspection config wiring; tuning falls back to defaults when null.</summary>
        public void WireInspectionConfig(InspectionConfig inspection)
        {
            inspectionConfig = inspection;
        }
    }
}
