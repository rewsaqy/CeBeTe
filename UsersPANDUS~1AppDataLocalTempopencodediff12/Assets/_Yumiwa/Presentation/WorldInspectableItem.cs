using UnityEngine;
using Yumiwa.Infrastructure;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// World representation of an inspectable item. Never enters the inventory
    /// directly; the interactor hands it to the inspection controller, and it is
    /// consumed only after the player confirms Take.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WorldInspectableItem : MonoBehaviour, IInspectable
    {
        [SerializeField] private ItemData data;
        [Tooltip("When true the GameObject is destroyed on Take; otherwise it is deactivated.")]
        [SerializeField] private bool destroyOnTake = true;

        private bool taken;

        public ItemData Data => data;
        public bool IsAvailable => !taken && data != null && gameObject.activeInHierarchy;

        public void Consume()
        {
            if (taken)
                return;
            taken = true;
            if (destroyOnTake)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (data == null)
                Debug.LogWarning($"[Yumiwa] WorldInspectableItem '{name}' has no ItemData assigned.", this);
        }
#endif
    }
}
