using UnityEngine;
using Yumiwa.Infrastructure;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Spawns the dedicated inspection prefab under a designer-placed root in
    /// front of the inspection camera. The real world item is never moved or
    /// touched; it stays exactly where it was until Take is confirmed.
    /// </summary>
    public sealed class ItemInspectionPresenter : MonoBehaviour
    {
        [SerializeField] private Transform inspectionRoot;
        [Tooltip("Optional dedicated light, enabled only while an item is shown.")]
        [SerializeField] private Light inspectionLight;
        [SerializeField] private string inspectionLayerName = "Inspection";

        private GameObject instance;
        private bool lightWasOn;

        private void Awake()
        {
            if (inspectionRoot == null)
                inspectionRoot = transform;
            // Paksa light inspeksi MATI saat scene mulai (alasan sama seperti
            // kamera: menimpa serialized lama). Show() menyalakan lagi.
            if (inspectionLight != null)
                inspectionLight.enabled = false;
        }

        /// <summary>Shows the item; returns false when no preview prefab exists.</summary>
        public bool Show(ItemData data)
        {
            Hide();
            if (data == null || data.InspectionPrefab == null)
            {
                Debug.LogWarning("[Yumiwa] Inspection preview missing; showing text only.", this);
                return false;
            }
            instance = Instantiate(data.InspectionPrefab, inspectionRoot);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            CenterBoundsOnRoot(instance.transform);
            ApplyInspectionLayer(instance);
            if (inspectionLight != null)
            {
                lightWasOn = inspectionLight.enabled;
                inspectionLight.enabled = true;
            }
            return true;
        }

        public void Hide()
        {
            if (instance != null)
            {
                Destroy(instance);
                instance = null;
            }
            if (inspectionLight != null)
                inspectionLight.enabled = lightWasOn;
        }

        private void OnDisable() => Hide();

        private void CenterBoundsOnRoot(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            Vector3 offset = root.position - bounds.center;
            root.position += offset;
        }

        private void ApplyInspectionLayer(GameObject target)
        {
            int layer = LayerMask.NameToLayer(inspectionLayerName);
            if (layer < 0)
                return;
            SetLayerRecursive(target.transform, layer);
        }

        private static void SetLayerRecursive(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            for (int i = 0; i < node.childCount; i++)
                SetLayerRecursive(node.GetChild(i), layer);
        }
    }
}
