using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// Inspection-map input behind a sampled boundary, mirroring NewInputAdapter.
    /// Enabled only while ItemInspectionState is active; gameplay input stays
    /// disabled for the whole session so buttons cannot leak into the world.
    /// </summary>
    public sealed class InspectionInputHandler : IDisposable
    {
        private readonly InputActionMap map;
        private readonly InputAction rotate;
        private readonly InputAction zoom;
        private readonly InputAction take;
        private readonly InputAction cancel;
        private readonly InputAction resetRotation;

        public event Action TakePressed;
        public event Action CancelPressed;
        public event Action ResetRotationPressed;

        public InspectionInputHandler(InputActionAsset asset)
        {
            map = asset != null ? asset.FindActionMap("Inspection") : null;
            if (map == null)
                throw new InvalidOperationException(
                    "Missing InputActionMap 'Inspection'. Add the Inspection map to the InputActionAsset (see ItemInspectionSetup).");
            rotate = Require("InspectRotate");
            zoom = Require("InspectZoom");
            take = Require("InspectTake");
            cancel = Require("InspectCancel");
            resetRotation = Require("InspectResetRotation");

            take.performed += _ => TakePressed?.Invoke();
            cancel.performed += _ => CancelPressed?.Invoke();
            resetRotation.performed += _ => ResetRotationPressed?.Invoke();
        }

        /// <summary>Mouse delta or right-stick deflection, per event poll.</summary>
        public Vector2 RotateDelta => rotate.ReadValue<Vector2>();

        /// <summary>Scroll-wheel Y (positive zooms in); gamepad d-pad maps here too.</summary>
        public float ZoomDelta => zoom.ReadValue<Vector2>().y;

        public bool IsRotateFromGamepad() => IsGamepad(rotate);
        public bool IsZoomFromGamepad() => IsGamepad(zoom);

        public void Enable() => map.Enable();
        public void Disable() => map.Disable();

        public void Dispose()
        {
            TakePressed = null;
            CancelPressed = null;
            ResetRotationPressed = null;
        }

        private InputAction Require(string actionName)
        {
            var action = map.FindAction(actionName);
            if (action == null)
                throw new InvalidOperationException($"Missing InputAction 'Inspection/{actionName}'.");
            return action;
        }

        private static bool IsGamepad(InputAction action)
        {
            var control = action.activeControl;
            return control != null && control.device is Gamepad;
        }
    }
}
