using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Pause menu (MVP-01, docs/07): Lanjutkan / Muat Checkpoint / pengaturan
    /// minimum / Mulai Baru. Deviation recorded: no title scene exists in the
    /// slice, so there is no separate Main Menu destination yet — Mulai Baru
    /// clears saves and reboots the chapter instead. Self-builds its UI.
    /// Settings persist via ISettingsStore and apply to movement + footsteps.
    /// </summary>
    public sealed class PauseMenuPresenter : MonoBehaviour
    {
        [Tooltip("Player transform used when loading a checkpoint pose.")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerMovementPresenter movement;
        [SerializeField] private FootstepPresenter footsteps;

        private ICheckpointService checkpoint;
        private ISettingsStore store;
        private IEventBus bus;
        private SettingsData settings = SettingsData.Defaults();

        private bool inspectionOpen;
        private bool inventoryOpen;
        private bool reconstructionOpen;
        private bool paused;

        private GameObject panel;
        private Slider sensitivitySlider;
        private Slider fovSlider;
        private Toggle headBobToggle;
        private Toggle shakeToggle;
        private Toggle fovBoostToggle;
        private Toggle subtitlesToggle;

        public bool IsPaused => paused;

        [Inject]
        public void Construct(ICheckpointService checkpoint, ISettingsStore store, IEventBus bus)
        {
            this.checkpoint = checkpoint;
            this.store = store;
            this.bus = bus;
        }

        private void Start()
        {
            // Apply persisted settings to the live presenters on boot.
            if (store != null) settings = store.Load();
            ApplyToPresenters();
        }

        private void OnEnable()
        {
            if (bus == null) return;
            bus.Subscribe<InspectionStartedEvent>(OnInspectionStarted);
            bus.Subscribe<InspectionEndedEvent>(OnInspectionEnded);
            bus.Subscribe<InventoryOpenedEvent>(OnInventoryOpened);
            bus.Subscribe<InventoryClosedEvent>(OnInventoryClosed);
            bus.Subscribe<ReconstructionOpenedEvent>(OnReconstructionOpened);
            bus.Subscribe<ReconstructionClosedEvent>(OnReconstructionClosed);
        }

        private void OnDisable()
        {
            if (bus == null) return;
            bus.Unsubscribe<InspectionStartedEvent>(OnInspectionStarted);
            bus.Unsubscribe<InspectionEndedEvent>(OnInspectionEnded);
            bus.Unsubscribe<InventoryOpenedEvent>(OnInventoryOpened);
            bus.Unsubscribe<InventoryClosedEvent>(OnInventoryClosed);
            bus.Unsubscribe<ReconstructionOpenedEvent>(OnReconstructionOpened);
            bus.Unsubscribe<ReconstructionClosedEvent>(OnReconstructionClosed);
            if (paused) SetPaused(false);
        }

        private void OnInspectionStarted(InspectionStartedEvent e) => inspectionOpen = true;
        private void OnInspectionEnded(InspectionEndedEvent e) => inspectionOpen = false;
        private void OnInventoryOpened(InventoryOpenedEvent e) => inventoryOpen = true;
        private void OnInventoryClosed(InventoryClosedEvent e) => inventoryOpen = false;
        private void OnReconstructionOpened(ReconstructionOpenedEvent e) => reconstructionOpen = true;
        private void OnReconstructionClosed(ReconstructionClosedEvent e) => reconstructionOpen = false;

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (!kb.pKey.wasPressedThisFrame) return;
            if (!paused && (inspectionOpen || inventoryOpen || reconstructionOpen)) return;
            SetPaused(!paused);
        }

        public void SetPaused(bool value)
        {
            paused = value;
            if (panel != null) panel.SetActive(paused);
            if (paused)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                bus?.Publish(new PauseOpenedEvent());
            }
            else
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                bus?.Publish(new PauseClosedEvent());
            }
        }

        private void OnResume() => SetPaused(false);

        private void OnLoadCheckpoint()
        {
            if (checkpoint == null) return;
            CheckpointData data = default;
            bool have = false;
            if (checkpoint.HasCheckpoint) { data = checkpoint.LoadCheckpoint(); have = true; }
            else if (checkpoint.HasChapterSave) { data = checkpoint.LoadChapter(); have = true; }
            if (!have || playerTransform == null) return;
            playerTransform.position = new Vector3(data.PosX, data.PosY, data.PosZ);
            if (data.RotW != 0 || data.RotX != 0)
                playerTransform.rotation = new Quaternion(data.RotX, data.RotY, data.RotZ, data.RotW);
            Physics.SyncTransforms();
            SetPaused(false);
            Debug.Log("[Yumiwa] Checkpoint dimuat dari menu jeda.", this);
        }

        private void OnNewGame()
        {
            if (checkpoint == null) return;
            checkpoint.ClearChapter();
            checkpoint.ClearCheckpoint();
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void ReadSettingsFromUi()
        {
            settings = new SettingsData(
                headBobToggle != null && headBobToggle.isOn,
                shakeToggle != null && shakeToggle.isOn,
                fovBoostToggle != null && fovBoostToggle.isOn,
                subtitlesToggle == null || subtitlesToggle.isOn,
                sensitivitySlider != null ? sensitivitySlider.value : 1f,
                fovSlider != null ? fovSlider.value : 75f);
            store?.Save(settings);
            ApplyToPresenters();
        }

        private void ApplyToPresenters()
        {
            if (movement != null) movement.ApplySettings(settings);
            if (footsteps != null) footsteps.ApplySubtitles(settings.SubtitlesEnabled);
        }

        private void Awake() => BuildUi();

        private void BuildUi()
        {
            var canvasGO = new GameObject("PauseCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var title = MakeText(panel.transform, "Title", new Vector2(0, 250), new Vector2(500, 44), 26);
            title.text = "Jeda — Act 1";

            MakeButton(panel.transform, "Resume", new Vector2(0, 170), "Lanjutkan (P)", OnResume);
            MakeButton(panel.transform, "Load", new Vector2(0, 115), "Muat Checkpoint", OnLoadCheckpoint);
            MakeButton(panel.transform, "New", new Vector2(0, 60), "Mulai Baru", OnNewGame);

            var st = MakeText(panel.transform, "SettingsTitle", new Vector2(0, 5), new Vector2(500, 34), 19);
            st.text = "Pengaturan";
            sensitivitySlider = MakeSlider(panel.transform, "Sens", new Vector2(0, -35), 0.2f, 3f, settings.MouseSensitivity, "Sensitivitas");
            fovSlider = MakeSlider(panel.transform, "Fov", new Vector2(0, -85), 60f, 90f, settings.Fov, "FOV");
            headBobToggle = MakeToggle(panel.transform, "Bob", new Vector2(-140, -130), "Head Bob", settings.HeadBobEnabled);
            shakeToggle = MakeToggle(panel.transform, "Shake", new Vector2(140, -130), "Shake", settings.CameraShakeEnabled);
            fovBoostToggle = MakeToggle(panel.transform, "Boost", new Vector2(-140, -170), "FOV Boost", settings.FovBoostEnabled);
            subtitlesToggle = MakeToggle(panel.transform, "Subs", new Vector2(140, -170), "Subtitle", settings.SubtitlesEnabled);
            panel.SetActive(false);
        }

        private void MakeButton(Transform parent, string name, Vector2 anchored, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(320, 44);
            crt.anchoredPosition = anchored;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.16f, 0.22f, 1f);
            go.AddComponent<Button>().onClick.AddListener(onClick);
            var text = MakeText(go.transform, "Label", Vector2.zero, new Vector2(300, 40), 16);
            text.text = label;
        }

        private Slider MakeSlider(Transform parent, string name, Vector2 anchored, float min, float max, float value, string caption)
        {
            var cap = MakeText(parent, name + "Cap", anchored + new Vector2(-190, 0), new Vector2(150, 30), 14);
            cap.alignment = TextAlignmentOptions.MidlineRight;
            cap.text = caption;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(280, 24);
            crt.anchoredPosition = anchored + new Vector2(60, 0);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.14f, 1f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(_ => ReadSettingsFromUi());
            var val = MakeText(parent, name + "Val", anchored + new Vector2(230, 0), new Vector2(70, 30), 14);
            val.alignment = TextAlignmentOptions.MidlineLeft;
            slider.onValueChanged.AddListener(v => val.text = v.ToString("0.0"));
            val.text = value.ToString("0.0");
            return slider;
        }

        private Toggle MakeToggle(Transform parent, string name, Vector2 anchored, string caption, bool value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(220, 30);
            crt.anchoredPosition = anchored;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.14f, 1f);
            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(_ => ReadSettingsFromUi());
            var cap = MakeText(go.transform, "Cap", new Vector2(20, 0), new Vector2(190, 28), 14);
            cap.alignment = TextAlignmentOptions.MidlineLeft;
            cap.text = caption;
            return toggle;
        }

        private static TMP_Text MakeText(Transform parent, string name, Vector2 anchored, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = size;
            crt.anchoredPosition = anchored;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }
    }
}
