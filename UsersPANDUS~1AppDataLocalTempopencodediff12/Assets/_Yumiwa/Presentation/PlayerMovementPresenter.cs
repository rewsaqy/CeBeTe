using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>Thin presenter mapping input and state to momentum-based CharacterController motion.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovementPresenter : MonoBehaviour
    {
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform orbitPivot;
        [SerializeField] private Camera playerCamera;

        private CharacterController controller;
        private IMovementStateMachine machine;
        private IStaminaService stamina;
        private IPlayerInput input;
        private IWaterProvider water;
        private IEventBus bus;
        private System.Action<MovementConfigReloadedEvent> configReloadHandler;
        private System.Action<InspectionStartedEvent> inspectionStartedHandler;
        private System.Action<InspectionEndedEvent> inspectionEndedHandler;
        private System.Action<PlayerWoundedEvent> woundedHandler;
        private bool controlsLocked;
        private IHealthService health;
        private bool sprinting;
        private float cameraPitch;
        private bool wounded;

        // Momentum / buffering state (presentation-side motion, never stamina rules).
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float airMaxSpeed;
        private float fallPeak;
        private bool wasGrounded = true;

        // Camera juice state (all magnitudes from MovementConfigData).
        private float baseFov = 75f;
        private float bobPhase;
        private float dipOffset;
        private float dipVelocity;
        private float shakeTimer;
        private float landPunch;

        [Inject]
        public void Construct(
            IMovementStateMachine machine,
            IStaminaService stamina,
            IPlayerInput input,
            IWaterProvider water,
            IEventBus bus,
            IHealthService health,
            MovementConfigData movement,
            StaminaConfigData staminaCfg)
        {
            this.machine = machine;
            this.stamina = stamina;
            this.input = input;
            this.water = water;
            this.bus = bus;
            this.health = health;
            this.movement = movement;
            this.staminaCfg = staminaCfg;
            if (health != null)
            {
                woundedHandler = _ => wounded = true;
                health.OnWounded += woundedHandler;
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                baseFov = playerCamera.fieldOfView;
        }

        /// <summary>Editor scene wiring; called by MovementTestSceneSetup (direct assignment).</summary>
        public void WireCamera(Transform target, Transform pivot)
        {
            cameraTarget = target;
            orbitPivot = pivot;
        }

        /// <summary>Dev-time hot-reload: swap the tuning snapshot without re-entering Play Mode.</summary>
        public void ApplyMovementConfig(MovementConfigData data)
        {
            movement = data;
        }

        private void Start()
        {
            LockCursor();
            input.Enable();
            input.JumpPressed += OnJumpPressed;
            input.JumpReleased += OnJumpReleased;
            configReloadHandler = OnConfigReloaded;
            bus.Subscribe(configReloadHandler);
            inspectionStartedHandler = _ => controlsLocked = true;
            inspectionEndedHandler = _ => controlsLocked = false;
            bus.Subscribe(inspectionStartedHandler);
            bus.Subscribe(inspectionEndedHandler);
            if (woundedHandler != null)
                health.OnWounded += woundedHandler;
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera != null)
                    baseFov = playerCamera.fieldOfView;
            }
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (input == null)
                return;
            input.JumpPressed -= OnJumpPressed;
            input.JumpReleased -= OnJumpReleased;
            if (bus != null && configReloadHandler != null)
                bus.Unsubscribe(configReloadHandler);
            if (bus != null)
            {
            if (inspectionStartedHandler != null) bus.Unsubscribe(inspectionStartedHandler);
            if (inspectionEndedHandler != null) bus.Unsubscribe(inspectionEndedHandler);
            if (woundedHandler != null && health != null)
                health.OnWounded -= woundedHandler;
            input.Disable();
        }

        private void OnConfigReloaded(MovementConfigReloadedEvent evt) =>
            ApplyMovementConfig(evt.Movement);

        private void OnJumpPressed()
        {
            jumpBufferTimer = movement.JumpBufferSeconds;
        }

        private void OnJumpReleased()
        {
            // Variable jump height: early release cuts upward velocity (tap = hop, hold = full).
            if (verticalVelocity > 0f)
                verticalVelocity *= movement.JumpCutMultiplier;
        }

        private void Update()
        {
            if (machine == null)
            {
                UnityEngine.Debug.LogWarning("[Yumiwa] PRESENTER_UNINJECTED");
                enabled = false;
                return;
            }
            // ItemInspectionState owns movement, look, and cursor while active.
            if (controlsLocked)
                return;
            if (Cursor.lockState != CursorLockMode.Locked && IsClickPressed())
                LockCursor();
            float dt = Time.deltaTime;
            bool grounded = controller.isGrounded;
            // Slope stick: re-attach to walkable ground lost to downhill separation
            // BEFORE logic runs, so slopes never flicker sprint/coyote state.
            if (!grounded && wasGrounded && TrySlopeSnap())
                grounded = true;

            HandleLandingTransition(grounded, dt);
            machine.SetGrounded(grounded);

            if (orbitPivot != null)
                orbitPivot.position = transform.position;
            ApplyLook(dt);
            machine.Tick(dt);
            UpdateSprint(dt, grounded);
            UpdateJump(dt, grounded);
            ApplyMovement(dt, grounded);
            ApplyCameraPose(dt, grounded);

            wasGrounded = grounded;
        }

        private void HandleLandingTransition(bool grounded, float dt)
        {
            if (grounded)
            {
                if (!wasGrounded)
                {
                    // Landing dip scaled by fall speed + FOV punch: heavy falls feel heavy.
                    // Gated so slope micro-separation never thuds the camera.
                    if (fallPeak < -movement.LandDipMinFallSpeed)
                    {
                        float impact = Mathf.Clamp01(-fallPeak / 15f);
                        dipVelocity = -Mathf.Max(1.5f, impact * 7f);
                        landPunch = 2f + impact * 6f;
                        shakeTimer = Mathf.Max(shakeTimer, 0.05f + impact * 0.12f);
                    }
                }
                if (verticalVelocity <= 0f)
                    verticalVelocity = -movement.GroundStickForce;
                jumpShotFired = false;
                fallPeak = 0f;
                coyoteTimer = movement.CoyoteSeconds;
            }
            else
            {
                if (wasGrounded && verticalVelocity <= 0.01f)
                {
                    // Walked off a ledge: preserve run momentum with a sprint-or-faster cap.
                    airMaxSpeed = Mathf.Max(SprintSpeed(), planarVelocity.magnitude);
                }
                coyoteTimer -= dt;
                if (verticalVelocity < fallPeak)
                    fallPeak = verticalVelocity;
            }
            if (jumpBufferTimer > 0f)
                jumpBufferTimer -= dt;
            if (landPunch > 0f)
                landPunch = Mathf.Max(0f, landPunch - dt * 24f);
            if (shakeTimer > 0f)
                shakeTimer -= dt;
        }

        private void UpdateJump(float dt, bool grounded)
        {
            if (jumpBufferTimer <= 0f)
                return;
            if (!(grounded || coyoteTimer > 0f))
                return;
            float jumpScale = water != null && water.IsInWater ? movement.WaterJumpMultiplier : 1f;
            verticalVelocity = AirborneMomentum.JumpSpeed(movement.Gravity, movement.JumpHeight) * jumpScale;
            // Real-world rule: horizontal momentum is fully preserved into the jump.
            airMaxSpeed = Mathf.Max(SprintSpeed(), planarVelocity.magnitude);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        private void UpdateSprint(float dt, bool grounded)
        {
            bool wantsSprint = grounded
                && input.IsSprintHeld
                && input.MoveDirection.HasDirection
                && (machine.Current == MovementState.Walk || machine.Current == MovementState.Idle);

            if (wantsSprint && machine.Current != MovementState.Sprint)
            {
                machine.RequestEnter(MovementState.Sprint);
            }
            else if (!wantsSprint && machine.Current == MovementState.Sprint)
            {
                machine.RequestEnter(MovementState.Walk);
            }

            sprinting = machine.Current == MovementState.Sprint;
            if (!sprinting)
                return;
            float cost = staminaCfg.SprintDrainPerSecond * dt;
            if (!stamina.CanAfford(cost))
                machine.RequestEnter(MovementState.Walk);
            else
                stamina.TrySpend(cost);
        }

        private float SprintSpeed() => movement.WalkSpeed * movement.SprintMultiplier;

        private float WoundedModifier() => wounded ? 0.75f : 1f;

        private float WaterMoveScale() =>
            water != null && water.IsInWater ? movement.WaterMoveMultiplier : 1f;

        private void ApplyMovement(float dt, bool grounded)
        {
            Vector3 wishDir = ToWorld(input.MoveDirection);
            if (grounded)
            {
                float target = movement.WalkSpeed
                    * (sprinting ? movement.SprintMultiplier : 1f)
                    * WoundedModifier()
                    * WaterMoveScale();
                Vector3 goal = wishDir * target;
                float rate = wishDir.sqrMagnitude > 0.001f
                    ? movement.GroundAccel
                    : movement.GroundDecel;
                planarVelocity = Vector3.MoveTowards(planarVelocity, goal, rate * dt);
            }
            else
            {
                // Airborne: steer with accel + tiny drag, cap total at preserved max
                // so sprint momentum survives the whole jump arc.
                float px = planarVelocity.x;
                float pz = planarVelocity.z;
                AirborneMomentum.ApplyAirControl(
                    ref px, ref pz, wishDir.x, wishDir.z,
                    movement.AirAccel, movement.AirDrag, dt);
                float cap = Mathf.Max(airMaxSpeed, SprintSpeed() * WoundedModifier() * WaterMoveScale());
                float mag = Mathf.Sqrt(px * px + pz * pz);
                if (mag > cap && mag > 0.001f)
                {
                    float scale = cap / mag;
                    px *= scale;
                    pz *= scale;
                }
                planarVelocity = new Vector3(px, 0f, pz);
            }

            if (grounded)
            {
                verticalVelocity = Mathf.Max(verticalVelocity, -movement.GroundStickForce);
            }
            else
            {
                float gravScale = water != null && water.IsInWater
                    ? movement.WaterGravityMultiplier
                    : 1f;
                verticalVelocity = Mathf.Max(
                    verticalVelocity + movement.Gravity * gravScale * dt,
                    movement.TerminalVelocity);
            }

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);

            // Third-person only: face travel direction. In FPS mode (no pivot) the
            // transform yaw IS the look yaw, so never rotate toward movement input.
            if (orbitPivot == null)
                return;
            Vector3 face = planarVelocity;
            if (face.sqrMagnitude > 0.01f)
            {
                var target = Quaternion.LookRotation(new Vector3(face.x, 0f, face.z));
                float t = 1f - Mathf.Exp(-movement.FaceTurnSpeed * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static bool IsClickPressed()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        private void ApplyLook(float dt)
        {
            float yawStep = Mathf.Clamp(
                input.LookDeltaX * movement.TurnSpeed * dt,
                -movement.LookMaxDegreesPerFrame, movement.LookMaxDegreesPerFrame);
            if (orbitPivot != null)
                orbitPivot.Rotate(0f, yawStep, 0f);
            else
                transform.Rotate(0f, yawStep, 0f);
            if (cameraTarget != null)
            {
                float pitchStep = Mathf.Clamp(
                    input.LookDeltaY * movement.TurnSpeed * dt,
                    -movement.LookMaxDegreesPerFrame, movement.LookMaxDegreesPerFrame);
                cameraPitch = Mathf.Clamp(cameraPitch - pitchStep, -80f, 80f);
                cameraTarget.localEulerAngles = new Vector3(cameraPitch, 0f, StrafeRoll());
            }
        }

        private float StrafeRoll()
        {
            // Subtle FPS strafe lean.
            float strafe = input != null ? input.MoveDirection.X : 0f;
            return -strafe * movement.StrafeTiltDegrees;
        }

        private void ApplyCameraPose(float dt, bool grounded)
        {
            if (cameraTarget == null)
                return;
            // Head-bob on the ground, scaled by actual speed (walk vs sprint cadence).
            float speed = planarVelocity.magnitude;
            if (grounded && speed > 0.5f)
            {
                float freq = sprinting ? movement.HeadBobFreqSprint : movement.HeadBobFreqWalk;
                bobPhase += dt * freq * (0.6f + 0.4f * Mathf.Clamp01(speed / SprintSpeed()));
            }
            float bobY = Mathf.Sin(bobPhase * Mathf.PI * 2f) * movement.HeadBobAmplitude
                * Mathf.Clamp01(speed / movement.WalkSpeed);
            float bobX = Mathf.Cos(bobPhase * Mathf.PI) * movement.HeadBobAmplitude * 0.5f
                * Mathf.Clamp01(speed / movement.WalkSpeed);

            // Landing dip spring.
            dipVelocity += (-dipOffset * 90f - dipVelocity * 12f) * dt;
            dipOffset += dipVelocity * dt;

            var pos = cameraTarget.localPosition;
            pos.x = Mathf.MoveTowards(pos.x, bobX, dt * 2f);
            pos.y = Mathf.MoveTowards(pos.y, movement.StandEyeHeight + bobY + dipOffset, dt * movement.EyeHeightSpeed)
                + dipOffset * dt * 8f;
            cameraTarget.localPosition = pos;

            ApplyFov(dt, grounded);
            if (wounded)
            {
                float bp = Time.time * 1.5f;
                cameraTarget.localPosition += new Vector3(
                    Mathf.Sin(bp) * 0.002f,
                    Mathf.Cos(bp * 1.3f) * 0.003f,
                    0f);
            }
            ApplyShake();
        }

        private void ApplyFov(float dt, bool grounded)
        {
            if (playerCamera == null)
                return;
            float target = baseFov + landPunch;
            if (sprinting)
                target += movement.SprintFovBoost;
            if (!grounded)
                target += movement.AirFovBoost;
            float t = 1f - Mathf.Exp(-movement.FovLerpSpeed * dt);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, target, t);
        }

        private void ApplyShake()
        {
            if (playerCamera == null || cameraTarget == null)
                return;
            if (shakeTimer <= 0f)
            {
                playerCamera.transform.localPosition = Vector3.zero;
                return;
            }
            float s = shakeTimer * 0.12f;
            float t = Time.time * 61f;
            playerCamera.transform.localPosition = new Vector3(
                Mathf.Sin(t * 1.1f) * s, Mathf.Cos(t * 1.7f) * s, 0f);
        }

        /// <summary>
        /// Stick to descending walkable ground (ramps, not ledges or walls).
        /// Jump rises (vertical &gt; 0) and real drops never qualify by construction.
        /// </summary>
        private bool TrySlopeSnap()
        {
            if (verticalVelocity > 0.01f || movement.SlopeSnapDistance <= 0f)
                return false;
            float feetY = transform.position.y + controller.center.y - controller.height * 0.5f;
            Vector3 origin = new Vector3(transform.position.x, feetY + 0.25f, transform.position.z);
            if (!Physics.Raycast(
                    origin, Vector3.down, out RaycastHit hit,
                    0.25f + movement.SlopeSnapDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (!SlopeSnapModel.TryComputeSnap(
                    feetY, hit.point.y, slopeAngle, controller.slopeLimit,
                    verticalVelocity, movement.SlopeSnapDistance, out float deltaY))
                return false;
            controller.Move(new Vector3(0f, deltaY, 0f));
            verticalVelocity = -movement.GroundStickForce;
            return true;
        }

        private Vector3 ToWorld(PlanarDirection dir)
        {
            float yaw = (orbitPivot != null ? orbitPivot.eulerAngles.y : transform.eulerAngles.y) * Mathf.Deg2Rad;
            float sin = Mathf.Sin(yaw);
            float cos = Mathf.Cos(yaw);
            return new Vector3(dir.X * cos + dir.Y * sin, 0f, -dir.X * sin + dir.Y * cos);
        }
    }
}
