using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;
using Yumiwa.Infrastructure;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Owns ItemInspectionState: opens inspection on interact, coordinates
    /// presenter/rotator/UI/input, confirms Take exactly once via the session
    /// service, and restores gameplay input on exit. World item stays untouched
    /// until Take succeeds; Cancel never mutates inventory or the world.
    /// </summary>
    public sealed class ItemInspectionController : MonoBehaviour
    {
        [SerializeField] private ItemInspectionPresenter presenter;
        [SerializeField] private ItemInspectionRotator rotator;
        [SerializeField] private ItemInspectionUI ui;

        private InspectionSessionService session;
        private InspectionInputHandler inspectionInput;
        private InputActionAsset asset;
        private IEventBus bus;
        private InputActionMap playerMap;

        private System.Action<InspectionTakeFailedEvent> takeFailedHandler;
        private IInspectable source;
        private bool active;
        private bool takeInFlight;

        public bool IsInspecting => active;

        [Inject]
        public void Construct(
            InspectionSessionService session,
            InspectionInputHandler inspectionInput,
            InputActionAsset asset,
            IEventBus bus)
        {
            this.session = session;
            this.inspectionInput = inspectionInput;
            this.asset = asset;
            this.bus = bus;
        }

        private void Start()
        {
            if (presenter == null || rotator == null || ui == null)
            {
                Debug.LogError("[Yumiwa] ItemInspectionController references unassigned.", this);
                enabled = false;
                return;
            }
            if (asset != null)
                playerMap = asset.FindActionMap("Player");
            // Paksa kamera inspeksi MATI saat scene mulai. Ini menimpa nilai
            // serialized lama (m_Enabled: 1) bila Unity masih memegang scene
            // versi lama di memori. Dinyalakan lagi di BeginInspection.
            rotator.SetCameraEnabled(false);
            takeFailedHandler = OnTakeFailed;
            bus.Subscribe(takeFailedHandler);
            ui.OnTakeClicked += OnTakePressed;
            ui.OnBackClicked += OnCancelPressed;
        }

        private void OnDestroy()
        {
            if (bus != null && takeFailedHandler != null)
                bus.Unsubscribe(takeFailedHandler);
            if (ui != null)
            {
                ui.OnTakeClicked -= OnTakePressed;
                ui.OnBackClicked -= OnCancelPressed;
            }
            if (active)
                Teardown();
        }

        /// <summary>Entry point from PlayerInteractor. Ignored while busy.</summary>
        public void BeginInspection(IInspectable inspectable)
        {
            if (active || session == null)
            {
                Debug.LogWarning($"[Yumiwa] BeginInspection ignored (active={active}, sessionNull={session == null}).", this);
                return;
            }
            if (!IsAlive(inspectable) || !inspectable.IsAvailable)
            {
                Debug.LogWarning("[Yumiwa] BeginInspection ignored: inspectable not available.", this);
                return;
            }
            ItemData data = inspectable.Data;
            if (data == null)
            {
                Debug.LogWarning("[Yumiwa] Inspectable has no ItemData; ignoring.", inspectable as Object);
                return;
            }
            ItemDefinition definition = data.ToDefinition();
            if (!ItemDefinition.IsValid(definition))
            {
                Debug.LogWarning($"[Yumiwa] ItemData '{data.name}' produced an invalid definition; ignoring.", data);
                return;
            }
            if (!session.Begin(definition))
            {
                Debug.LogWarning("[Yumiwa] BeginInspection ignored: session busy or item invalid.", this);
                return;
            }

            source = inspectable;
            active = true;
            takeInFlight = false;
            Debug.Log($"[Yumiwa] Inspecting '{definition.DisplayName}'.", this);

            if (playerMap != null)
                playerMap.Disable();
            inspectionInput.Enable();
            inspectionInput.TakePressed += OnTakePressed;
            inspectionInput.CancelPressed += OnCancelPressed;
            inspectionInput.ResetRotationPressed += OnResetPressed;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            bool shown = presenter.Show(data);
            rotator.ResetView();
            rotator.SetCameraEnabled(true);
            rotator.enabled = true;
            ui.Bind(definition);
            if (!shown)
                ui.ShowMessage("Preview unavailable for this item.");
            ui.Show();
        }

        private void OnTakePressed()
        {
            if (!active || takeInFlight)
                return;
            takeInFlight = true;
            InspectionTakeResult result = session.ConfirmTake();
            if (result == InspectionTakeResult.Success)
            {
                // World item disappears only now, after the inventory accepted it.
                IInspectable taken = source;
                source = null;
                if (IsAlive(taken))
                {
                    try
                    {
                        taken.Consume();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Yumiwa] Consume failed after successful Take: {e.Message}");
                    }
                }
                session.EndAfterTake();
                Teardown();
            }
            else if (result == InspectionTakeResult.RejectedByInventory)
            {
                // Reason arrives via InspectionTakeFailedEvent; session stays active.
                takeInFlight = false;
            }
            else
            {
                takeInFlight = false;
            }
        }

        private void OnCancelPressed()
        {
            if (!active)
                return;
            source = null;
            session.Cancel();
            Teardown();
        }

        private void OnResetPressed()
        {
            if (active && rotator != null)
                rotator.ResetView();
        }

        private void OnTakeFailed(InspectionTakeFailedEvent evt)
        {
            if (active && ui != null)
                ui.ShowMessage(evt.Reason);
        }

        private void Teardown()
        {
            inspectionInput.TakePressed -= OnTakePressed;
            inspectionInput.CancelPressed -= OnCancelPressed;
            inspectionInput.ResetRotationPressed -= OnResetPressed;
            inspectionInput.Disable();
            if (presenter != null)
                presenter.Hide();
            if (rotator != null)
            {
                rotator.enabled = false;
                rotator.SetCameraEnabled(false);
            }
            if (ui != null)
                ui.Hide();
            if (playerMap != null)
                playerMap.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            source = null;
            active = false;
            takeInFlight = false;
        }

        /// <summary>Unity-destroyed references are not C# null when typed as an interface.</summary>
        private static bool IsAlive(IInspectable inspectable)
        {
            if (inspectable == null)
                return false;
            if (inspectable is Object unityObject)
                return unityObject != null;
            return true;
        }
    }
}
