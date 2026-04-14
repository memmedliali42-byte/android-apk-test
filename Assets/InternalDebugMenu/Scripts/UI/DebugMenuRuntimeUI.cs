using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InternalDebugMenu
{
    /// <summary>
    /// Builds a lightweight in-scene mobile panel. No external overlays are used.
    /// </summary>
    public sealed class DebugMenuRuntimeUI : MonoBehaviour
    {
        private const int MaxConsoleLines = 14;

        private static Font cachedFont;
        private static Sprite cachedSprite;

        [SerializeField] private DebugManager debugManager;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080.0f, 1920.0f);

        private readonly List<string> consoleLines = new List<string>(MaxConsoleLines);
        private readonly List<CanvasGroup> gatedSections = new List<CanvasGroup>();

        private CanvasGroup rootCanvasGroup;
        private GameObject uiRoot;
        private GameObject accessGateRow;
        private GameObject playerSectionRoot;
        private GameObject weaponSectionRoot;
        private GameObject enemySectionRoot;
        private GameObject visualSectionRoot;
        private GameObject networkSectionRoot;
        private Text statusText;
        private Text speedValueText;
        private Text difficultyValueText;
        private Text latencyValueText;
        private Text packetLossValueText;
        private Text weaponIdsText;
        private Text consoleOutputText;
        private InputField accessCodeInput;
        private InputField weaponIdInput;
        private InputField enemyCountInput;
        private InputField consoleInput;
        private Toggle safeModeToggle;
        private Toggle godModeToggle;
        private Toggle infiniteAmmoToggle;
        private Toggle instantReloadToggle;
        private Toggle aiFreezeToggle;
        private Toggle hitboxesToggle;
        private Toggle raycastsToggle;
        private Toggle outlineToggle;
        private Toggle syncLogsToggle;
        private Slider speedSlider;
        private Slider difficultySlider;
        private Slider latencySlider;
        private Slider packetLossSlider;
        private bool suppressCallbacks;

        private void Awake()
        {
            if (debugManager == null)
            {
                debugManager = GetComponent<DebugManager>();
            }

            if (debugManager == null || !debugManager.IsSupportedBuild)
            {
                return;
            }

            EnsureEventSystem();
            BuildUi();
            RebuildConsoleFromAudit();
            RefreshView();
        }

        private void OnEnable()
        {
            if (debugManager == null)
            {
                return;
            }

            debugManager.StateChanged += RefreshView;

            if (debugManager.AuditLogger != null)
            {
                debugManager.AuditLogger.EntryLogged += HandleAuditEntry;
            }

            RefreshView();
        }

        private void OnDisable()
        {
            if (debugManager == null)
            {
                return;
            }

            debugManager.StateChanged -= RefreshView;

            if (debugManager.AuditLogger != null)
            {
                debugManager.AuditLogger.EntryLogged -= HandleAuditEntry;
            }
        }

        private void HandleAuditEntry(DebugAuditEntry entry)
        {
            AppendConsoleLine(entry.ToString());
        }

        private void BuildUi()
        {
            if (uiRoot != null)
            {
                return;
            }

            uiRoot = CreateUiElement("InternalDebugCanvas", transform).gameObject;
            var canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            uiRoot.AddComponent<GraphicRaycaster>();

            var scaler = uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 1.0f;

            rootCanvasGroup = uiRoot.AddComponent<CanvasGroup>();

            var backdrop = CreateImage("Backdrop", uiRoot.transform, new Color(0.02f, 0.03f, 0.05f, 0.65f));
            Stretch(backdrop.rectTransform);

            var panel = CreateImage("Panel", uiRoot.transform, new Color(0.08f, 0.10f, 0.14f, 0.98f));
            Anchor(panel.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(28, 28, 28, 28);
            panelLayout.spacing = 18.0f;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            BuildHeader(panel.transform);
            BuildAccessGate(panel.transform);
            BuildSafeModeRow(panel.transform);
            var contentRoot = BuildScrollView(panel.transform);

            playerSectionRoot = BuildPlayerSection(contentRoot);
            weaponSectionRoot = BuildWeaponSection(contentRoot);
            enemySectionRoot = BuildEnemySection(contentRoot);
            visualSectionRoot = BuildVisualSection(contentRoot);
            networkSectionRoot = BuildNetworkSection(contentRoot);
            BuildConsoleSection(contentRoot);
        }

        private void BuildHeader(Transform parent)
        {
            var header = CreateRow("Header", parent, 18.0f);
            CreateText("Title", header.transform, "Internal Debug Menu", 34, FontStyle.Bold, TextAnchor.MiddleLeft);

            var spacer = CreateUiElement("Spacer", header.transform);
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

            var closeButton = CreateButton("Close", header.transform, "Close");
            closeButton.onClick.AddListener(() => debugManager.HideMenu());

            statusText = CreateText("Status", parent, string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            statusText.color = new Color(0.85f, 0.90f, 0.97f, 1.0f);
        }

        private void BuildAccessGate(Transform parent)
        {
            accessGateRow = CreateRow("AccessGate", parent, 12.0f).gameObject;
            CreateText("AccessLabel", accessGateRow.transform, debugManager.Settings.AccessCodeHint, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            accessCodeInput = CreateInputField("AccessCodeInput", accessGateRow.transform, "Enter secret code");
            accessCodeInput.contentType = InputField.ContentType.Alphanumeric;
            accessCodeInput.lineType = InputField.LineType.SingleLine;
            accessCodeInput.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

            var unlockButton = CreateButton("Unlock", accessGateRow.transform, "Unlock");
            unlockButton.onClick.AddListener(HandleUnlockRequested);
        }

        private void BuildSafeModeRow(Transform parent)
        {
            var safeModeRow = CreateRow("SafeMode", parent, 12.0f);
            CreateText("SafeModeLabel", safeModeRow.transform, "SAFE MODE", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            safeModeToggle = CreateToggle("SafeModeToggle", safeModeRow.transform);
            safeModeToggle.onValueChanged.AddListener(value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetSafeMode(value);
                }
            });
        }

        private Transform BuildScrollView(Transform parent)
        {
            var scrollRoot = CreateImage("ScrollRoot", parent, new Color(0.11f, 0.14f, 0.18f, 1.0f));
            scrollRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1.0f;

            var viewport = CreateImage("Viewport", scrollRoot.transform, new Color(0.00f, 0.00f, 0.00f, 0.0f));
            Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = CreateUiElement("Content", viewport.transform);
            var contentRect = content;
            contentRect.anchorMin = new Vector2(0.0f, 1.0f);
            contentRect.anchorMax = new Vector2(1.0f, 1.0f);
            contentRect.pivot = new Vector2(0.5f, 1.0f);
            contentRect.offsetMin = new Vector2(0.0f, 0.0f);
            contentRect.offsetMax = new Vector2(0.0f, 0.0f);

            var verticalLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(18, 18, 18, 18);
            verticalLayout.spacing = 18.0f;
            verticalLayout.childControlHeight = true;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28.0f;

            return content;
        }

        private GameObject BuildPlayerSection(Transform parent)
        {
            var section = CreateSection("Player", parent);
            godModeToggle = CreateToggleRow(section.Content, "God Mode", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetGodMode(value);
                }
            });
            speedSlider = CreateSliderRow(section.Content, "Movement Speed", debugManager.Settings.MinimumMovementSpeedMultiplier, debugManager.Settings.MaximumMovementSpeedMultiplier, out speedValueText);
            speedSlider.onValueChanged.AddListener(value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetMovementSpeedMultiplier(value);
                }
            });

            var healButton = CreateButtonRow(section.Content, "Heal Player");
            healButton.onClick.AddListener(() => debugManager.HealPlayer());
            gatedSections.Add(section.CanvasGroup);
            return section.Root;
        }

        private GameObject BuildWeaponSection(Transform parent)
        {
            var section = CreateSection("Weapons", parent);
            infiniteAmmoToggle = CreateToggleRow(section.Content, "Infinite Ammo", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetInfiniteAmmo(value);
                }
            });
            instantReloadToggle = CreateToggleRow(section.Content, "Instant Reload", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetInstantReload(value);
                }
            });

            weaponIdInput = CreateInputRow(section.Content, "Weapon Id", "weapon_ak");
            weaponIdsText = CreateText("WeaponIds", section.Content, string.Empty, 18, FontStyle.Normal, TextAnchor.MiddleLeft);
            weaponIdsText.color = new Color(0.70f, 0.76f, 0.83f, 1.0f);

            var spawnButton = CreateButtonRow(section.Content, "Spawn Weapon");
            spawnButton.onClick.AddListener(() =>
            {
                debugManager.TrySpawnWeapon(weaponIdInput.text.Trim(), out _);
            });

            gatedSections.Add(section.CanvasGroup);
            return section.Root;
        }

        private GameObject BuildEnemySection(Transform parent)
        {
            var section = CreateSection("AI / Enemies", parent);
            enemyCountInput = CreateInputRow(section.Content, "Enemy Count", "5");

            var spawnEnemyButton = CreateButtonRow(section.Content, "Spawn Enemies");
            spawnEnemyButton.onClick.AddListener(() =>
            {
                if (!int.TryParse(enemyCountInput.text, out var count))
                {
                    AppendConsoleLine("Enemy count must be a valid integer.");
                    return;
                }

                debugManager.TrySpawnEnemies(count, out _);
            });

            aiFreezeToggle = CreateToggleRow(section.Content, "Freeze AI", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetAiFrozen(value);
                }
            });

            difficultySlider = CreateSliderRow(section.Content, "Difficulty", debugManager.Settings.MinimumDifficulty, debugManager.Settings.MaximumDifficulty, out difficultyValueText);
            difficultySlider.onValueChanged.AddListener(value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetDifficulty(value);
                }
            });

            gatedSections.Add(section.CanvasGroup);
            return section.Root;
        }

        private GameObject BuildVisualSection(Transform parent)
        {
            var section = CreateSection("Visual Debug", parent);
            hitboxesToggle = CreateToggleRow(section.Content, "Show Hitboxes", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetHitboxesVisible(value);
                }
            });
            raycastsToggle = CreateToggleRow(section.Content, "Draw Raycasts", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetRaycastsVisible(value);
                }
            });
            outlineToggle = CreateToggleRow(section.Content, "Enemy Outline", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetEnemyOutlineVisible(value);
                }
            });
            gatedSections.Add(section.CanvasGroup);
            return section.Root;
        }

        private GameObject BuildNetworkSection(Transform parent)
        {
            var section = CreateSection("Network Testing", parent);
            latencySlider = CreateSliderRow(section.Content, "Latency (ms)", debugManager.Settings.MinimumLatencyMs, debugManager.Settings.MaximumLatencyMs, out latencyValueText, wholeNumbers: true);
            latencySlider.onValueChanged.AddListener(value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetLatencyMs(Mathf.RoundToInt(value));
                }
            });

            packetLossSlider = CreateSliderRow(section.Content, "Packet Loss (%)", 0.0f, debugManager.Settings.MaximumPacketLossPercent, out packetLossValueText, wholeNumbers: true);
            packetLossSlider.onValueChanged.AddListener(value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetPacketLossPercent(Mathf.RoundToInt(value));
                }
            });

            syncLogsToggle = CreateToggleRow(section.Content, "Sync Debug Logs", value =>
            {
                if (!suppressCallbacks)
                {
                    debugManager.SetSyncDebugLogs(value);
                }
            });

            gatedSections.Add(section.CanvasGroup);
            return section.Root;
        }

        private void BuildConsoleSection(Transform parent)
        {
            var section = CreateSection("Debug Console", parent);
            consoleInput = CreateInputRow(section.Content, "Command", "help");

            var executeButton = CreateButtonRow(section.Content, "Run Command");
            executeButton.onClick.AddListener(() =>
            {
                debugManager.ExecuteConsoleCommand(consoleInput.text);
                consoleInput.text = string.Empty;
            });

            var consoleBox = CreateImage("ConsoleOutput", section.Content, new Color(0.05f, 0.06f, 0.08f, 1.0f));
            consoleBox.gameObject.AddComponent<LayoutElement>().preferredHeight = 260.0f;
            consoleOutputText = CreateText("ConsoleText", consoleBox.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft);
            consoleOutputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            consoleOutputText.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(consoleOutputText.rectTransform, 12.0f, 12.0f, 12.0f, 12.0f);
            gatedSections.Add(section.CanvasGroup);
        }

        private void HandleUnlockRequested()
        {
            if (debugManager.TryUnlockAccessCode(accessCodeInput.text))
            {
                accessCodeInput.text = string.Empty;
            }
        }

        private void RefreshView()
        {
            if (uiRoot == null || debugManager == null)
            {
                return;
            }

            suppressCallbacks = true;

            var shouldShow = debugManager.IsSupportedBuild && debugManager.IsMenuVisible;
            rootCanvasGroup.alpha = shouldShow ? 1.0f : 0.0f;
            rootCanvasGroup.interactable = shouldShow;
            rootCanvasGroup.blocksRaycasts = shouldShow;

            if (!shouldShow)
            {
                suppressCallbacks = false;
                return;
            }

            statusText.text = debugManager.GetMenuStatusLine();
            accessGateRow.SetActive(debugManager.Settings.RequiresSecretCode && !debugManager.IsAccessCodeUnlocked);

            safeModeToggle.SetIsOnWithoutNotify(debugManager.IsSafeModeEnabled);

            SetSectionVisible(playerSectionRoot, debugManager.HasPlayerTools);
            SetSectionVisible(weaponSectionRoot, debugManager.HasWeaponTools);
            SetSectionVisible(enemySectionRoot, debugManager.HasEnemyTools);
            SetSectionVisible(visualSectionRoot, debugManager.HasVisualTools);
            SetSectionVisible(networkSectionRoot, debugManager.HasNetworkTools);

            godModeToggle.SetIsOnWithoutNotify(debugManager.GodModeEnabled);
            infiniteAmmoToggle.SetIsOnWithoutNotify(debugManager.InfiniteAmmoEnabled);
            instantReloadToggle.SetIsOnWithoutNotify(debugManager.InstantReloadEnabled);
            aiFreezeToggle.SetIsOnWithoutNotify(debugManager.AiFrozen);
            hitboxesToggle.SetIsOnWithoutNotify(debugManager.HitboxesVisible);
            raycastsToggle.SetIsOnWithoutNotify(debugManager.RaycastsVisible);
            outlineToggle.SetIsOnWithoutNotify(debugManager.EnemyOutlineVisible);
            syncLogsToggle.SetIsOnWithoutNotify(debugManager.SyncLogsEnabled);

            speedSlider.SetValueWithoutNotify(debugManager.MovementSpeedMultiplier);
            difficultySlider.SetValueWithoutNotify(debugManager.DifficultyMultiplier);
            latencySlider.SetValueWithoutNotify(debugManager.LatencyMs);
            packetLossSlider.SetValueWithoutNotify(debugManager.PacketLossPercent);

            speedValueText.text = debugManager.MovementSpeedMultiplier.ToString("0.00");
            difficultyValueText.text = debugManager.DifficultyMultiplier.ToString("0.00");
            latencyValueText.text = debugManager.LatencyMs.ToString();
            packetLossValueText.text = $"{debugManager.PacketLossPercent}%";
            weaponIdsText.text = debugManager.AvailableWeaponIds.Count > 0
                ? $"Available: {string.Join(", ", debugManager.AvailableWeaponIds)}"
                : "Available: provide IDs through your weapon adapter.";

            var actionsEnabled = debugManager.CanExecuteActions;
            for (var index = 0; index < gatedSections.Count; index++)
            {
                SetSectionInteractable(gatedSections[index], actionsEnabled);
            }

            suppressCallbacks = false;
        }

        private void RebuildConsoleFromAudit()
        {
            if (debugManager == null || debugManager.AuditLogger == null)
            {
                return;
            }

            consoleLines.Clear();
            var snapshot = debugManager.AuditLogger.Snapshot();
            var startIndex = Mathf.Max(0, snapshot.Length - MaxConsoleLines);

            for (var index = startIndex; index < snapshot.Length; index++)
            {
                consoleLines.Add(snapshot[index].ToString());
            }

            UpdateConsoleText();
        }

        private void AppendConsoleLine(string line)
        {
            if (consoleOutputText == null || string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (consoleLines.Count >= MaxConsoleLines)
            {
                consoleLines.RemoveAt(0);
            }

            consoleLines.Add(line.Trim());
            UpdateConsoleText();
        }

        private void UpdateConsoleText()
        {
            if (consoleOutputText != null)
            {
                consoleOutputText.text = string.Join("\n", consoleLines);
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static SectionHandle CreateSection(string title, Transform parent)
        {
            var root = CreateImage(title.Replace(" ", string.Empty), parent, new Color(0.13f, 0.16f, 0.21f, 1.0f));
            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 14.0f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            CreateText("SectionTitle", root.transform, title, 26, FontStyle.Bold, TextAnchor.MiddleLeft);

            return new SectionHandle
            {
                Root = root.gameObject,
                Content = root.rectTransform,
                CanvasGroup = canvasGroup
            };
        }

        private static Toggle CreateToggleRow(Transform parent, string label, UnityEngine.Events.UnityAction<bool> onValueChanged)
        {
            var row = CreateRow(label.Replace(" ", string.Empty), parent, 12.0f);
            CreateText("Label", row.transform, label, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            var toggle = CreateToggle("Toggle", row.transform);
            toggle.onValueChanged.AddListener(onValueChanged);
            return toggle;
        }

        private static Slider CreateSliderRow(Transform parent, string label, float minValue, float maxValue, out Text valueText, bool wholeNumbers = false)
        {
            var container = CreateImage(label.Replace(" ", string.Empty) + "Container", parent, new Color(0.09f, 0.11f, 0.15f, 1.0f));
            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10.0f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var header = CreateRow("Header", container.transform, 8.0f);
            CreateText("Label", header.transform, label, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            valueText = CreateText("Value", header.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleRight);
            valueText.gameObject.AddComponent<LayoutElement>().minWidth = 90.0f;

            var slider = CreateSlider("Slider", container.transform, minValue, maxValue, wholeNumbers);
            return slider;
        }

        private static InputField CreateInputRow(Transform parent, string label, string placeholder)
        {
            var container = CreateImage(label.Replace(" ", string.Empty) + "InputContainer", parent, new Color(0.09f, 0.11f, 0.15f, 1.0f));
            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10.0f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateText("Label", container.transform, label, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            return CreateInputField(label.Replace(" ", string.Empty) + "Input", container.transform, placeholder);
        }

        private static Button CreateButtonRow(Transform parent, string label)
        {
            return CreateButton(label.Replace(" ", string.Empty) + "Button", parent, label);
        }

        private static RectTransform CreateRow(string name, Transform parent, float spacing)
        {
            var row = CreateUiElement(name, parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 56.0f;
            return row;
        }

        private static Button CreateButton(string name, Transform parent, string text)
        {
            var buttonRoot = CreateImage(name, parent, new Color(0.23f, 0.54f, 0.86f, 1.0f));
            var layoutElement = buttonRoot.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 58.0f;
            layoutElement.minWidth = 180.0f;

            var button = buttonRoot.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.23f, 0.54f, 0.86f, 1.0f);
            colors.highlightedColor = new Color(0.28f, 0.61f, 0.92f, 1.0f);
            colors.pressedColor = new Color(0.18f, 0.45f, 0.75f, 1.0f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.18f, 0.22f, 0.28f, 1.0f);
            button.colors = colors;

            var label = CreateText("Label", buttonRoot.transform, text, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 10.0f, 10.0f, 8.0f, 8.0f);
            return button;
        }

        private static InputField CreateInputField(string name, Transform parent, string placeholder)
        {
            var background = CreateImage(name, parent, new Color(0.05f, 0.06f, 0.09f, 1.0f));
            var layout = background.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 58.0f;

            var text = CreateText("Text", background.transform, string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, 16.0f, 16.0f, 10.0f, 10.0f);

            var placeholderText = CreateText("Placeholder", background.transform, placeholder, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            placeholderText.color = new Color(0.52f, 0.58f, 0.66f, 1.0f);
            Stretch(placeholderText.rectTransform, 16.0f, 16.0f, 10.0f, 10.0f);

            var inputField = background.gameObject.AddComponent<InputField>();
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.targetGraphic = background;
            return inputField;
        }

        private static Toggle CreateToggle(string name, Transform parent)
        {
            var root = CreateUiElement(name, parent);
            root.gameObject.AddComponent<LayoutElement>().preferredWidth = 90.0f;

            var background = CreateImage("Background", root, new Color(0.07f, 0.08f, 0.10f, 1.0f));
            Anchor(background.rectTransform, new Vector2(1.0f, 0.5f), new Vector2(1.0f, 0.5f), new Vector2(-56.0f, 0.0f), new Vector2(58.0f, 34.0f));

            var checkmark = CreateImage("Checkmark", background.transform, new Color(0.23f, 0.84f, 0.50f, 1.0f));
            Stretch(checkmark.rectTransform, 6.0f, 6.0f, 6.0f, 6.0f);

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;

            return toggle;
        }

        private static Slider CreateSlider(string name, Transform parent, float minValue, float maxValue, bool wholeNumbers)
        {
            var sliderRoot = CreateUiElement(name, parent);
            sliderRoot.gameObject.AddComponent<LayoutElement>().minHeight = 42.0f;

            var background = CreateImage("Background", sliderRoot, new Color(0.18f, 0.22f, 0.27f, 1.0f));
            Anchor(background.rectTransform, new Vector2(0.0f, 0.5f), new Vector2(1.0f, 0.5f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 14.0f));

            var fillArea = CreateUiElement("FillArea", sliderRoot);
            Stretch(fillArea, 0.0f, 20.0f, 0.0f, 0.0f);

            var fill = CreateImage("Fill", fillArea, new Color(0.23f, 0.54f, 0.86f, 1.0f));
            Stretch(fill.rectTransform);

            var handleSlideArea = CreateUiElement("HandleArea", sliderRoot);
            Stretch(handleSlideArea, 0.0f, 0.0f, 0.0f, 0.0f);

            var handle = CreateImage("Handle", handleSlideArea, new Color(0.96f, 0.98f, 1.0f, 1.0f));
            Anchor(handle.rectTransform, new Vector2(0.0f, 0.5f), new Vector2(0.0f, 0.5f), new Vector2(0.0f, 0.0f), new Vector2(28.0f, 28.0f));

            var slider = sliderRoot.gameObject.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = wholeNumbers;
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = CreateUiElement(name, parent);
            var text = textObject.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = new Color(0.96f, 0.97f, 0.99f, 1.0f);
            text.alignment = alignment;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var element = CreateUiElement(name, parent);
            var image = element.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static RectTransform CreateUiElement(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            Stretch(rectTransform, 0.0f, 0.0f, 0.0f, 0.0f);
        }

        private static void Stretch(RectTransform rectTransform, float left, float right, float top, float bottom)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void Anchor(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetSectionInteractable(CanvasGroup canvasGroup, bool interactable)
        {
            canvasGroup.alpha = interactable ? 1.0f : 0.45f;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        private static void SetSectionVisible(GameObject root, bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private static Font Font
        {
            get
            {
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return cachedFont;
            }
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (cachedSprite == null)
                {
                    cachedSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f));
                }

                return cachedSprite;
            }
        }

        private sealed class SectionHandle
        {
            public GameObject Root;
            public RectTransform Content;
            public CanvasGroup CanvasGroup;
        }
    }
}
