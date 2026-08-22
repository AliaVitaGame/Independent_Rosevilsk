using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.UI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private static readonly Color AccentRed = new(0.72f, 0.08f, 0.08f, 1f);
        private static readonly Color TextLight = new(0.86f, 0.86f, 0.84f, 1f);

        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button npcsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button discordButton;

        [SerializeField] private string discordUrl = "https://discord.gg/";

        public event Action ContinueClicked;
        public event Action NewGameClicked;
        public event Action LoadGameClicked;
        public event Action NpcsClicked;
        public event Action SettingsClicked;

        private Button[] _menuButtons;
        private Button _selectedButton;
        private bool _menuInteractable = true;
        private bool _continueAllowed;
        private bool _loadAllowed = true;

        private void Awake()
        {
            EnsureEventSystem();
            DisableLegacyMenuUi();
            AutoWireIfNeeded();

            _menuButtons = new[]
            {
                continueButton,
                newGameButton,
                loadGameButton,
                npcsButton,
                settingsButton,
                exitButton
            };

            BindMenuButton(continueButton, OnContinue);
            BindMenuButton(newGameButton, OnNewGame);
            BindMenuButton(loadGameButton, OnLoadGame);
            BindMenuButton(npcsButton, OnNpcs);
            BindMenuButton(settingsButton, OnSettings);
            BindMenuButton(exitButton, OnExit);

            if (discordButton != null)
                discordButton.onClick.AddListener(OnDiscord);

            // Continue stays off until MenuEntryPoint confirms a save exists.
            SetContinueInteractable(false);
            SetLoadInteractable(true);
            SelectButton(newGameButton);
        }

        private void Start()
        {
            if (NewGameClicked == null)
            {
                Debug.LogError(
                    "[MainMenu] MenuEntryPoint is not running. Start Play from Bootstrap scene " +
                    "so MenuInstaller can wire New/Load/Continue.");
            }
        }

        private void OnDestroy()
        {
            UnbindMenuButton(continueButton, OnContinue);
            UnbindMenuButton(newGameButton, OnNewGame);
            UnbindMenuButton(loadGameButton, OnLoadGame);
            UnbindMenuButton(npcsButton, OnNpcs);
            UnbindMenuButton(settingsButton, OnSettings);
            UnbindMenuButton(exitButton, OnExit);

            if (discordButton != null)
                discordButton.onClick.RemoveListener(OnDiscord);
        }

        public void SetInteractable(bool interactable)
        {
            _menuInteractable = interactable;
            ApplyInteractableState();
        }

        public void SetContinueInteractable(bool interactable)
        {
            _continueAllowed = interactable;
            if (continueButton != null)
                continueButton.interactable = _menuInteractable && _continueAllowed;
        }

        public void SetLoadInteractable(bool interactable)
        {
            _loadAllowed = interactable;
            if (loadGameButton != null)
                loadGameButton.interactable = _menuInteractable && _loadAllowed;
        }

        private void ApplyInteractableState()
        {
            if (continueButton != null)
                continueButton.interactable = _menuInteractable && _continueAllowed;
            if (newGameButton != null)
                newGameButton.interactable = _menuInteractable;
            if (loadGameButton != null)
                loadGameButton.interactable = _menuInteractable && _loadAllowed;
            if (npcsButton != null)
                npcsButton.interactable = _menuInteractable;
            if (settingsButton != null)
                settingsButton.interactable = _menuInteractable;
            if (exitButton != null)
                exitButton.interactable = _menuInteractable;
            if (discordButton != null)
                discordButton.interactable = _menuInteractable;
        }

        private void AutoWireIfNeeded()
        {
            continueButton ??= FindButton("CONTINUEButton");
            newGameButton ??= FindButton("NEWGAMEButton");
            loadGameButton ??= FindButton("LOADGAMEButton");
            npcsButton ??= FindButton("NPCSButton");
            settingsButton ??= FindButton("SETTINGSButton");
            exitButton ??= FindButton("EXITButton");
            discordButton ??= FindButton("Discord") ?? EnsureDiscordButton();

            EnsureButtonRaycast(continueButton);
            EnsureButtonRaycast(newGameButton);
            EnsureButtonRaycast(loadGameButton);
            EnsureButtonRaycast(npcsButton);
            EnsureButtonRaycast(settingsButton);
            EnsureButtonRaycast(exitButton);
        }

        private static Button FindButton(string objectName)
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var button in buttons)
            {
                if (button.name == objectName)
                    return button;
            }

            return null;
        }

        private static void EnsureButtonRaycast(Button button)
        {
            if (button == null)
                return;

            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = true;
                return;
            }

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                image = button.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.001f);
            }

            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        private Button EnsureDiscordButton()
        {
            var discord = GameObject.Find("Discord");
            if (discord == null)
                return null;

            var button = discord.GetComponent<Button>();
            if (button != null)
                return button;

            var image = discord.GetComponent<Image>();
            if (image == null)
            {
                image = discord.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.001f);
            }

            button = discord.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                return;
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        private static void DisableLegacyMenuUi()
        {
            var legacy = GameObject.Find("MainMenuUI");
            if (legacy != null && legacy.activeSelf)
                legacy.SetActive(false);
        }

        private void BindMenuButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null)
            {
                Debug.LogWarning("[MainMenu] Button reference is missing — check object names in the scene.");
                return;
            }

            button.onClick.AddListener(handler);
            EnsureHoverHandlers(button);
        }

        private static void UnbindMenuButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button != null)
                button.onClick.RemoveListener(handler);
        }

        private void EnsureHoverHandlers(Button button)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>()
                          ?? button.gameObject.AddComponent<EventTrigger>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => SelectButton(button));
            AddTrigger(trigger, EventTriggerType.Select, _ => SelectButton(button));
        }

        private static void AddTrigger(
            EventTrigger trigger,
            EventTriggerType type,
            Action<BaseEventData> callback)
        {
            foreach (var existing in trigger.triggers)
            {
                if (existing.eventID == type)
                    return;
            }

            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

        private void SelectButton(Button button)
        {
            if (button == null || _selectedButton == button)
                return;

            _selectedButton = button;

            foreach (var menuButton in _menuButtons)
            {
                if (menuButton == null)
                    continue;

                SetSelectedVisual(menuButton, menuButton == button);
            }
        }

        private static void SetSelectedVisual(Button button, bool selected)
        {
            var frame = button.transform.Find("Frame");
            if (frame != null)
            {
                var frameImage = frame.GetComponent<Image>();
                if (frameImage != null)
                    frameImage.color = selected ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            var label = button.transform.Find("Label");
            if (label != null)
            {
                var tmp = label.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.color = selected ? AccentRed : TextLight;
            }

            var arrow = button.transform.Find("Arrow");
            if (arrow != null)
                arrow.gameObject.SetActive(selected);
        }

        private void OnContinue() => ContinueClicked?.Invoke();
        private void OnNewGame() => NewGameClicked?.Invoke();
        private void OnLoadGame() => LoadGameClicked?.Invoke();

        private void OnNpcs()
        {
            NpcsClicked?.Invoke();
            Debug.Log("[MainMenu] NPCS — экран персонажей пока не подключён.");
        }

        private void OnSettings()
        {
            SettingsClicked?.Invoke();

            var managers = FindObjectsByType<HEAVYART.TopDownShooter.Netcode.MainMenuUIManager>(
                FindObjectsInactive.Include);
            HEAVYART.TopDownShooter.Netcode.MainMenuUIManager manager = null;
            if (managers != null && managers.Length > 0)
                manager = managers[0];

            if (manager != null)
            {
                if (!manager.gameObject.activeSelf)
                    manager.gameObject.SetActive(true);

                HideLegacyPanelsExceptSettings(manager);
                manager.ShowSettingsPopup();
                return;
            }

            Debug.Log("[MainMenu] SETTINGS — popup не найден.");
        }

        private static void HideLegacyPanelsExceptSettings(
            HEAVYART.TopDownShooter.Netcode.MainMenuUIManager manager)
        {
            if (manager.mainGamePanel != null)
                manager.mainGamePanel.gameObject.SetActive(false);
            if (manager.joinGamePopup != null)
                manager.joinGamePopup.gameObject.SetActive(false);
            if (manager.startGamePopup != null)
                manager.startGamePopup.gameObject.SetActive(false);
            if (manager.errorPopup != null)
                manager.errorPopup.gameObject.SetActive(false);
            if (manager.waitForPublicGamePopup != null)
                manager.waitForPublicGamePopup.gameObject.SetActive(false);
            if (manager.waitForPrivateGamePopup != null)
                manager.waitForPrivateGamePopup.gameObject.SetActive(false);

            var settingsButton = manager.transform.Find("SettingsButton");
            if (settingsButton != null)
                settingsButton.gameObject.SetActive(false);
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDiscord()
        {
            if (string.IsNullOrWhiteSpace(discordUrl))
                return;

            Application.OpenURL(discordUrl);
        }
    }
}
