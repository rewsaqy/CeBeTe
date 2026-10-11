using System;
using UnityEngine.InputSystem;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>New Input System adapter behind the IPlayerInput boundary.</summary>
    public sealed class NewInputAdapter : IPlayerInput
    {
        private readonly InputActionAsset asset;
        private readonly InputActionMap playerMap;
        private readonly InputActionMap uiMap;
        private readonly InputAction move;
        private readonly InputAction look;
        private readonly InputAction sprint;
        private readonly InputAction jump;
        private readonly InputAction fire;
        private readonly InputAction crouch;

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action FirePressed;
        public event Action CrouchPressed;

        public NewInputAdapter(InputActionAsset asset)
        {
            this.asset = asset;
            playerMap = asset.FindActionMap("Player");
            uiMap = asset.FindActionMap("UI");
            move = Require(asset, "Player/Move");
            look = Require(asset, "Player/Look");
            sprint = Require(asset, "Player/Sprint");
            jump = Require(asset, "Player/Jump");
            fire = Require(asset, "Player/Attack");
            crouch = Require(asset, "Player/Crouch");

            jump.performed += _ => JumpPressed?.Invoke();
            jump.canceled += _ => JumpReleased?.Invoke();
            fire.performed += _ => FirePressed?.Invoke();
            crouch.performed += _ => CrouchPressed?.Invoke();
        }

        public PlanarDirection MoveDirection
        {
            get
            {
                var v = move.ReadValue<UnityEngine.Vector2>();
                return new PlanarDirection(v.x, v.y);
            }
        }

        public float LookDeltaX => look.ReadValue<UnityEngine.Vector2>().x;
        public float LookDeltaY => look.ReadValue<UnityEngine.Vector2>().y;
        public bool IsSprintHeld => sprint.IsPressed();

        /// <summary>
        /// Hanya map Player (+UI untuk klik tombol) yang dinyalakan. Map
        /// Inspection TIDAK BOLEH ikut nyala di sini — itu milik eksklusif
        /// ItemInspectionState (lihat ItemInspectionController). asset.Enable()
        /// menyalakan SEMUA map, yang dulu bikin tombol E terpicu ganda:
        /// sekali sebagai Interact, sekali lagi sebagai InspectTake.
        /// </summary>
        public void Enable()
        {
            playerMap?.Enable();
            uiMap?.Enable();
        }

        public void Disable()
        {
            playerMap?.Disable();
            uiMap?.Disable();
        }

        private static InputAction Require(InputActionAsset asset, string path)
        {
            var action = asset.FindAction(path);
            if (action == null)
                throw new InvalidOperationException($"Missing InputAction '{path}' in {asset.name}.");
            return action;
        }
    }
}
