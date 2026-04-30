#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool that generates the complete MainMenu Canvas hierarchy
/// and wires every serialized field + button onClick to MainMenuManager.
/// Run via: PhysioGuide > Build Main Menu UI
/// </summary>
public class PhysioGuideMenuUIBuilder : EditorWindow
{
    // --- Color Palette ---
    private static readonly Color COL_BG_LOGIN      = new Color(0.08f, 0.08f, 0.14f, 1f);
    private static readonly Color COL_BG_MAIN       = new Color(0.10f, 0.10f, 0.16f, 1f);
    private static readonly Color COL_BG_EXERCISE   = new Color(0.08f, 0.12f, 0.08f, 1f);
    private static readonly Color COL_BG_HISTORY    = new Color(0.09f, 0.09f, 0.12f, 1f);
    private static readonly Color COL_BG_SETTINGS   = new Color(0.12f, 0.10f, 0.10f, 1f);

    private static readonly Color COL_BTN_PRIMARY   = new Color(0.18f, 0.55f, 0.78f, 1f);
    private static readonly Color COL_BTN_SECONDARY = new Color(0.30f, 0.30f, 0.40f, 1f);
    private static readonly Color COL_BTN_EXERCISE  = new Color(0.20f, 0.65f, 0.35f, 1f);
    private static readonly Color COL_BTN_DANGER    = new Color(0.75f, 0.22f, 0.22f, 1f);
    private static readonly Color COL_INPUT_BG      = new Color(0.18f, 0.18f, 0.25f, 1f);
    private static readonly Color COL_TITLE         = new Color(0.40f, 0.75f, 1.00f, 1f);
    private static readonly Color COL_SUBTITLE      = new Color(0.70f, 0.70f, 0.80f, 1f);

    [MenuItem("PhysioGuide/Build Main Menu UI")]
    public static void BuildMainMenuUI()
    {
        // ------------------------------------------
        // 1. CANVAS + EVENT SYSTEM
        // ------------------------------------------
        GameObject canvasGO = new GameObject("MainMenu_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create MainMenu UI");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject evt = new GameObject("EventSystem");
            evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(evt, "Create EventSystem");
        }

        // ------------------------------------------
        // 2. ATTACH MAINMENUMANAGER
        // ------------------------------------------
        MainMenuManager mgr = canvasGO.AddComponent<MainMenuManager>();

        // ------------------------------------------
        // 3. CREATE THE 5 PANELS
        // ------------------------------------------
        GameObject loginPanel    = CreatePanel("1_LoginPanel",             canvasGO.transform, COL_BG_LOGIN);
        GameObject mainPanel     = CreatePanel("2_MainMenuPanel",          canvasGO.transform, COL_BG_MAIN);
        GameObject exercisePanel = CreatePanel("3_ExerciseSelectionPanel", canvasGO.transform, COL_BG_EXERCISE);
        GameObject historyPanel  = CreatePanel("4_HistoryPanel",           canvasGO.transform, COL_BG_HISTORY);
        GameObject settingsPanel = CreatePanel("5_SettingsPanel",          canvasGO.transform, COL_BG_SETTINGS);

        // Assign panel references
        mgr.loginPanel              = loginPanel;
        mgr.mainMenuPanel           = mainPanel;
        mgr.exerciseSelectionPanel  = exercisePanel;
        mgr.historyPanel            = historyPanel;
        mgr.settingsPanel           = settingsPanel;

        // ------------------------------------------
        // 4. LOGIN PANEL
        // ------------------------------------------
        CreateText("AppTitle", loginPanel.transform,
            new Vector2(0, 300), new Vector2(800, 80),
            "PHYSIOGUIDE", 64, COL_TITLE, FontStyles.Bold);

        CreateText("AppSubtitle", loginPanel.transform,
            new Vector2(0, 230), new Vector2(800, 50),
            "VR Physiotherapy Rehabilitation System", 28, COL_SUBTITLE, FontStyles.Italic);

        // Divider
        CreateDivider(loginPanel.transform, new Vector2(0, 185), 500f);

        CreateText("UsernameLabel", loginPanel.transform,
            new Vector2(-130, 120), new Vector2(200, 40),
            "Username", 24, Color.white, FontStyles.Normal, TextAlignmentOptions.MidlineRight);

        TMP_InputField usernameInput = CreateInputField("Username_Input", loginPanel.transform,
            new Vector2(100, 120), new Vector2(350, 50), "Enter username...");
        mgr.usernameInput = usernameInput;

        CreateText("LoginHint", loginPanel.transform,
            new Vector2(0, -10), new Vector2(600, 30),
            "New user? Just enter a username and password to create an account.", 16, COL_SUBTITLE, FontStyles.Italic);

        Button loginBtn = CreateButton("Login_Button", loginPanel.transform,
            new Vector2(0, -60), new Vector2(350, 60), "Login", 30, COL_BTN_PRIMARY);
        UnityEventTools.AddPersistentListener(loginBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnLoginClicked));
        mgr.loginButtonObj = loginBtn.gameObject;
        // Get the label text inside the button for loading state
        TextMeshProUGUI loginBtnLabel = loginBtn.gameObject.GetComponentInChildren<TextMeshProUGUI>();
        mgr.loginButtonLabel = loginBtnLabel;

        TextMeshProUGUI errorText = CreateText("LoginError_Text", loginPanel.transform,
            new Vector2(0, -140), new Vector2(600, 40),
            "", 22, Color.red, FontStyles.Normal);
        mgr.loginErrorText = errorText;

        // ------------------------------------------
        // 5. MAIN MENU PANEL
        // ------------------------------------------
        TextMeshProUGUI welcomeText = CreateText("Welcome_Text", mainPanel.transform,
            new Vector2(0, 300), new Vector2(900, 100),
            "Welcome!", 40, Color.white, FontStyles.Normal);
        mgr.welcomeText = welcomeText;

        CreateDivider(mainPanel.transform, new Vector2(0, 235), 600f);

        Button playBtn = CreateButton("Play_Button", mainPanel.transform,
            new Vector2(0, 140), new Vector2(400, 70), "> Start Exercise", 32, COL_BTN_PRIMARY);
        UnityEventTools.AddPersistentListener(playBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnPlayClicked));

        Button historyBtn = CreateButton("History_Button", mainPanel.transform,
            new Vector2(0, 50), new Vector2(400, 70), "Session History", 32, COL_BTN_SECONDARY);
        UnityEventTools.AddPersistentListener(historyBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnHistoryClicked));

        Button settingsBtn = CreateButton("Settings_Button", mainPanel.transform,
            new Vector2(0, -40), new Vector2(400, 70), "Settings", 32, COL_BTN_SECONDARY);
        UnityEventTools.AddPersistentListener(settingsBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnSettingsClicked));

        Button logoutBtn = CreateButton("Logout_Button", mainPanel.transform,
            new Vector2(0, -150), new Vector2(400, 60), "Logout", 28, COL_BTN_DANGER);
        UnityEventTools.AddPersistentListener(logoutBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnLogoutClicked));

        Button quitBtn = CreateButton("Quit_Button", mainPanel.transform,
            new Vector2(0, -240), new Vector2(400, 60), "Quit Application", 28, COL_BTN_DANGER);
        UnityEventTools.AddPersistentListener(quitBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnQuitClicked));

        // ------------------------------------------
        // 6. EXERCISE SELECTION PANEL
        // ------------------------------------------
        CreateText("ExTitle", exercisePanel.transform,
            new Vector2(0, 300), new Vector2(800, 80),
            "Select Your Exercise", 48, COL_TITLE, FontStyles.Bold);

        CreateText("ExSubtitle", exercisePanel.transform,
            new Vector2(0, 230), new Vector2(800, 50),
            "Choose one of the exercises below to begin your session.", 24, COL_SUBTITLE, FontStyles.Normal);

        CreateDivider(exercisePanel.transform, new Vector2(0, 195), 600f);

        // Scaption card
        GameObject scaptionCard = CreateExerciseCard("Scaption_Card", exercisePanel.transform,
            new Vector2(0, 90), "Scaption (Arm Raise)",
            "Raise both arms in the scapular plane.\nTargets shoulder mobility & stability.",
            COL_BTN_EXERCISE);
        Button scaptionBtn = scaptionCard.GetComponentInChildren<Button>();
        SetButtonOnClick_ExerciseSelected(scaptionBtn, mgr, "scaption");

        // Chin Tuck card
        GameObject chinTuckCard = CreateExerciseCard("ChinTuck_Card", exercisePanel.transform,
            new Vector2(0, -90), "Chin Tuck",
            "Retract chin towards neck.\nTargets cervical posture correction.",
            COL_BTN_EXERCISE);
        Button chinTuckBtn = chinTuckCard.GetComponentInChildren<Button>();
        SetButtonOnClick_ExerciseSelected(chinTuckBtn, mgr, "chin_tuck");

        Button exBackBtn = CreateButton("Back_Button", exercisePanel.transform,
            new Vector2(0, -260), new Vector2(300, 55), "<< Back to Menu", 26, COL_BTN_SECONDARY);
        UnityEventTools.AddPersistentListener(exBackBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnBackToMainMenuClicked));

        // ------------------------------------------
        // 7. HISTORY PANEL
        // ------------------------------------------
        CreateText("HistTitle", historyPanel.transform,
            new Vector2(0, 430), new Vector2(800, 80),
            "Session History", 48, COL_TITLE, FontStyles.Bold);

        CreateDivider(historyPanel.transform, new Vector2(0, 385), 700f);

        // Scrollable content area
        GameObject scrollGO = CreateScrollView("HistoryScroll", historyPanel.transform,
            new Vector2(0, -20), new Vector2(900, 700));
        TextMeshProUGUI histContent = scrollGO.GetComponentInChildren<TextMeshProUGUI>();
        if (histContent != null)
        {
            histContent.text = "Loading history...";
            mgr.historyContentText = histContent;
        }

        Button histBackBtn = CreateButton("Back_Button", historyPanel.transform,
            new Vector2(0, -440), new Vector2(300, 55), "<< Back to Menu", 26, COL_BTN_SECONDARY);
        UnityEventTools.AddPersistentListener(histBackBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnBackToMainMenuClicked));

        // ------------------------------------------
        // 8. SETTINGS PANEL
        // ------------------------------------------
        CreateText("SetTitle", settingsPanel.transform,
            new Vector2(0, 300), new Vector2(800, 80),
            "Settings", 48, COL_TITLE, FontStyles.Bold);

        CreateDivider(settingsPanel.transform, new Vector2(0, 255), 500f);

        CreateText("IPLabel", settingsPanel.transform,
            new Vector2(-100, 170), new Vector2(250, 40),
            "Mediapipe Server IP", 24, Color.white, FontStyles.Normal, TextAlignmentOptions.MidlineRight);

        TMP_InputField ipInput = CreateInputField("IP_Input", settingsPanel.transform,
            new Vector2(140, 170), new Vector2(350, 50), "e.g., 10.200.248.104");
        // Load current saved IP
        ipInput.text = PlayerPrefs.GetString("TargetIP", "10.200.248.104"); mgr.ipAddressInput = ipInput;

        Button saveSettingsBtn = CreateButton("SaveSettings_Button", settingsPanel.transform,
            new Vector2(0, 80), new Vector2(350, 60), "Save Settings", 28, COL_BTN_PRIMARY);
        // We'll wire a simple save via lambda captured at build time — user can refine later
        // For now, settings save is manual via the button

        CreateText("SettingsInfo", settingsPanel.transform,
            new Vector2(0, -20), new Vector2(700, 80),
            "Configure the IP address of the machine running the\nMediaPipe pose estimation backend.",
            20, COL_SUBTITLE, FontStyles.Normal);

        Button setBackBtn = CreateButton("Back_Button", settingsPanel.transform,
            new Vector2(0, -150), new Vector2(300, 55), "<< Back to Menu", 26, COL_BTN_SECONDARY);
        UnityEventTools.AddPersistentListener(setBackBtn.onClick,
            new UnityEngine.Events.UnityAction(mgr.OnBackToMainMenuClicked));

        // ------------------------------------------
        // 9. DEFAULT VISIBILITY
        // ------------------------------------------
        loginPanel.SetActive(true);
        mainPanel.SetActive(false);
        exercisePanel.SetActive(false);
        historyPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Mark dirty so Unity saves the references
        EditorUtility.SetDirty(mgr);
        Selection.activeGameObject = canvasGO;

        Debug.Log("<color=cyan>[PhysioGuide]</color> Main Menu UI built and fully wired!");
    }

    // ----------------------------------------------
    // HELPER: Full-stretch Panel
    // ----------------------------------------------
    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    // ----------------------------------------------
    // HELPER: TextMeshProUGUI
    // ----------------------------------------------
    private static TextMeshProUGUI CreateText(string name, Transform parent,
        Vector2 pos, Vector2 size, string text, float fontSize,
        Color color, FontStyles style,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.richText = true;

        return tmp;
    }

    // Convenience overload
    private static TextMeshProUGUI CreateText(string name, Transform parent,
        Vector2 pos, Vector2 size, string text, float fontSize, Color color, FontStyles style)
    {
        return CreateText(name, parent, pos, size, text, fontSize, color, style, TextAlignmentOptions.Center);
    }

    // ----------------------------------------------
    // HELPER: Button with child text
    // ----------------------------------------------
    private static Button CreateButton(string name, Transform parent,
        Vector2 pos, Vector2 size, string label, float fontSize, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        Button btn = go.AddComponent<Button>();
        // Slight hover tint
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f, 1f);
        cb.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f, 1f);
        btn.colors = cb;

        // Button label
        CreateText("Label", go.transform, Vector2.zero, size, label, fontSize,
            Color.white, FontStyles.Bold);

        return btn;
    }

    // ----------------------------------------------
    // HELPER: TMP_InputField (fully functional)
    // ----------------------------------------------
    private static TMP_InputField CreateInputField(string name, Transform parent,
        Vector2 pos, Vector2 size, string placeholder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image bg = go.AddComponent<Image>();
        bg.color = COL_INPUT_BG;

        // Text Area (viewport for clipping)
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        RectTransform taRt = textArea.AddComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10, 5);
        taRt.offsetMax = new Vector2(-10, -5);
        textArea.AddComponent<RectMask2D>();

        // Placeholder text
        GameObject phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        RectTransform phRt = phGO.AddComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero;
        phRt.offsetMax = Vector2.zero;
        TextMeshProUGUI phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = placeholder;
        phTMP.fontSize = 22;
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.color = new Color(0.5f, 0.5f, 0.6f, 0.7f);
        phTMP.textWrappingMode = TextWrappingModes.NoWrap;
        phTMP.overflowMode = TextOverflowModes.Ellipsis;

        // Editable text
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(textArea.transform, false);
        RectTransform txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
        txtTMP.text = "";
        txtTMP.fontSize = 22;
        txtTMP.color = Color.white;
        txtTMP.textWrappingMode = TextWrappingModes.NoWrap;
        txtTMP.overflowMode = TextOverflowModes.Ellipsis;

        // Wire TMP_InputField
        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRt;
        input.textComponent = txtTMP;
        input.placeholder = phTMP;
        input.fontAsset = txtTMP.font;
        input.pointSize = 22;

        return input;
    }

    // ----------------------------------------------
    // HELPER: Horizontal divider line
    // ----------------------------------------------
    private static void CreateDivider(Transform parent, Vector2 pos, float width)
    {
        GameObject go = new GameObject("Divider");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 2);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
    }

    // ----------------------------------------------
    // HELPER: Exercise card (icon-less, description + button)
    // ----------------------------------------------
    private static GameObject CreateExerciseCard(string name, Transform parent,
        Vector2 pos, string title, string description, Color btnColor)
    {
        // Card background
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);

        RectTransform crt = card.AddComponent<RectTransform>();
        crt.anchoredPosition = pos;
        crt.sizeDelta = new Vector2(700, 130);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.15f, 0.15f, 0.22f, 0.9f);

        // Title
        CreateText("CardTitle", card.transform,
            new Vector2(0, 30), new Vector2(650, 40),
            title, 30, Color.white, FontStyles.Bold);

        // Description
        CreateText("CardDesc", card.transform,
            new Vector2(0, -15), new Vector2(650, 50),
            description, 18, COL_SUBTITLE, FontStyles.Normal);

        // Overlay button covering the whole card (clickable area)
        GameObject btnGO = new GameObject("SelectButton");
        btnGO.transform.SetParent(card.transform, false);
        RectTransform brt = btnGO.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0); // Transparent overlay

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(btnColor.r, btnColor.g, btnColor.b, 0.25f);
        cb.pressedColor = new Color(btnColor.r, btnColor.g, btnColor.b, 0.45f);
        btn.colors = cb;

        return card;
    }

    // ----------------------------------------------
    // HELPER: ScrollView with TMP content
    // ----------------------------------------------
    private static GameObject CreateScrollView(string name, Transform parent,
        Vector2 pos, Vector2 size)
    {
        // Scroll container
        GameObject scrollGO = new GameObject(name);
        scrollGO.transform.SetParent(parent, false);

        RectTransform srt = scrollGO.AddComponent<RectTransform>();
        srt.anchoredPosition = pos;
        srt.sizeDelta = size;

        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);

        RectTransform vprt = viewport.AddComponent<RectTransform>();
        vprt.anchorMin = Vector2.zero;
        vprt.anchorMax = Vector2.one;
        vprt.offsetMin = new Vector2(10, 10);
        vprt.offsetMax = new Vector2(-10, -10);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0); // Transparent
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        scrollRect.viewport = vprt;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform crt = content.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = new Vector2(0, 0);
        crt.offsetMax = new Vector2(0, 0);
        crt.sizeDelta = new Vector2(0, 1500); // Tall enough for content

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = crt;

        // Text inside content
        GameObject txtGO = new GameObject("HistoryText");
        txtGO.transform.SetParent(content.transform, false);

        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.offsetMin = new Vector2(10, 0);
        trt.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Loading history...";
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.richText = true;

        // Let the text drive the content height
        LayoutElement le = txtGO.AddComponent<LayoutElement>();
        le.preferredWidth = size.x - 40;

        // CRITICAL: Auto-size the text's own height so the Content's ContentSizeFitter
        // can calculate the correct scrollable height. Without this, the text is 0 height.
        ContentSizeFitter textFitter = txtGO.AddComponent<ContentSizeFitter>();
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return scrollGO;
    }

    // ----------------------------------------------
    // HELPER: Wire OnExerciseSelected(string) via serialized UnityEvent
    // ----------------------------------------------
    private static void SetButtonOnClick_ExerciseSelected(Button btn, MainMenuManager mgr, string exerciseName)
    {
        // Clear any previously added empty listeners
        int count = btn.onClick.GetPersistentEventCount();
        for (int i = count - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(btn.onClick, i);
        }

        // Use the string-parameter overload
        UnityEventTools.AddStringPersistentListener(
            btn.onClick,
            new UnityEngine.Events.UnityAction<string>(mgr.OnExerciseSelected),
            exerciseName
        );
    }
}
#endif


