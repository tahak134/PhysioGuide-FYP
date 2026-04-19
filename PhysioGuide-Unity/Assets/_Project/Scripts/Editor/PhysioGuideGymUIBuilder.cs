#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool that generates the GymScene UI panels:
///   - InstructionPanel (shown before exercise starts)
///   - SummaryPanel (shown after exercise ends)
/// Always creates a dedicated Screen Space Overlay canvas so panels
/// render on top of the 3D gym environment.
/// Run via: PhysioGuide > Build Gym Scene UI
/// </summary>
public class PhysioGuideGymUIBuilder : EditorWindow
{
    private static readonly Color COL_BG_INSTRUCTION = new Color(0.08f, 0.10f, 0.16f, 0.95f);
    private static readonly Color COL_BG_SUMMARY     = new Color(0.06f, 0.08f, 0.14f, 0.95f);
    private static readonly Color COL_BTN_PRIMARY    = new Color(0.18f, 0.55f, 0.78f, 1f);
    private static readonly Color COL_BTN_SUCCESS    = new Color(0.20f, 0.65f, 0.35f, 1f);
    private static readonly Color COL_TITLE          = new Color(0.40f, 0.75f, 1.00f, 1f);
    private static readonly Color COL_SUBTITLE       = new Color(0.70f, 0.70f, 0.80f, 1f);

    private const string CANVAS_NAME = "GymUI_OverlayCanvas";

    [MenuItem("PhysioGuide/Build Gym Scene UI")]
    public static void BuildGymSceneUI()
    {
        // ──────────────────────────────────────────
        // 0. CLEAN UP: Delete old generated canvas if re-running
        // ──────────────────────────────────────────
        GameObject oldCanvas = GameObject.Find(CANVAS_NAME);
        if (oldCanvas != null)
        {
            Undo.DestroyObjectImmediate(oldCanvas);
            Debug.Log("[GymUIBuilder] Removed previous generated canvas.");
        }

        // Also clean up orphaned panels from previous runs on wrong canvases
        CleanupOrphaned("InstructionPanel");
        CleanupOrphaned("SummaryPanel");

        // ──────────────────────────────────────────
        // 1. CREATE DEDICATED SCREEN SPACE OVERLAY CANVAS
        //    (Never reuse WorldSpace or Camera canvases)
        // ──────────────────────────────────────────
        GameObject canvasGO = new GameObject(CANVAS_NAME);
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Gym UI");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // On top of everything

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

        // ──────────────────────────────────────────
        // 2. FIND MANAGERS IN SCENE
        // ──────────────────────────────────────────
        GymManager gymMgr = Object.FindFirstObjectByType<GymManager>();
        UIManager uiMgr = Object.FindFirstObjectByType<UIManager>();

        if (gymMgr == null) Debug.LogWarning("[GymUIBuilder] GymManager not found! Wire references manually.");
        if (uiMgr == null) Debug.LogWarning("[GymUIBuilder] UIManager not found! Wire references manually.");

        // ──────────────────────────────────────────
        // 3. INSTRUCTION PANEL
        // ──────────────────────────────────────────
        GameObject instrPanel = CreatePanel("InstructionPanel", canvasGO.transform, COL_BG_INSTRUCTION);

        CreateText("InstructionTitle", instrPanel.transform,
            new Vector2(0, 250), new Vector2(900, 80),
            "PHYSIOGUIDE", 56, COL_TITLE, FontStyles.Bold);

        TextMeshProUGUI instrTitle = CreateText("ExerciseName", instrPanel.transform,
            new Vector2(0, 170), new Vector2(900, 60),
            "Exercise Name", 40, Color.white, FontStyles.Bold);

        CreateDivider(instrPanel.transform, new Vector2(0, 130), 700f);

        TextMeshProUGUI instrBody = CreateText("InstructionBody", instrPanel.transform,
            new Vector2(0, -10), new Vector2(800, 280),
            "Instructions will appear here.\n\nFollow the guidance and perform the exercise correctly.",
            26, new Color(0.9f, 0.9f, 0.95f, 1f), FontStyles.Normal);

        CreateDivider(instrPanel.transform, new Vector2(0, -180), 700f);

        CreateText("ReadyPrompt", instrPanel.transform,
            new Vector2(0, -220), new Vector2(600, 40),
            "Press the button below when you are ready.", 22, COL_SUBTITLE, FontStyles.Italic);

        Button startBtn = CreateButton("StartExercise_Button", instrPanel.transform,
            new Vector2(0, -300), new Vector2(450, 80),
            "Start Exercise", 34, COL_BTN_SUCCESS);

        // Wire GymManager references
        if (gymMgr != null)
        {
            gymMgr.instructionPanel = instrPanel;
            gymMgr.instructionTitleText = instrTitle;
            gymMgr.instructionBodyText = instrBody;
            UnityEventTools.AddPersistentListener(startBtn.onClick,
                new UnityEngine.Events.UnityAction(gymMgr.OnAcceptInstructionsClicked));
            EditorUtility.SetDirty(gymMgr);
        }

        // ──────────────────────────────────────────
        // 4. SUMMARY PANEL
        // ──────────────────────────────────────────
        GameObject summaryPanel = CreatePanel("SummaryPanel", canvasGO.transform, COL_BG_SUMMARY);

        TextMeshProUGUI summaryTitle = CreateText("SummaryTitle", summaryPanel.transform,
            new Vector2(0, 380), new Vector2(900, 80),
            "Session Complete!", 52, COL_TITLE, FontStyles.Bold);

        CreateDivider(summaryPanel.transform, new Vector2(0, 330), 700f);

        TextMeshProUGUI summaryDetails = CreateText("SummaryDetails", summaryPanel.transform,
            new Vector2(0, 80), new Vector2(800, 400),
            "<b>Patient:</b> ---\n" +
            "<b>Exercise:</b> ---\n" +
            "<b>Duration:</b> ---\n" +
            "<b>Repetitions:</b> ---\n" +
            "<b>Accuracy:</b> ---\n" +
            "<b>Frames Recorded:</b> ---",
            28, Color.white, FontStyles.Normal, TextAlignmentOptions.Left);

        TextMeshProUGUI summaryErrors = CreateText("SummaryErrors", summaryPanel.transform,
            new Vector2(0, -170), new Vector2(800, 100),
            "", 24, COL_SUBTITLE, FontStyles.Normal);

        CreateDivider(summaryPanel.transform, new Vector2(0, -240), 700f);

        Button returnBtn = CreateButton("ReturnToMenu_Button", summaryPanel.transform,
            new Vector2(0, -320), new Vector2(450, 80),
            "Return to Menu", 32, COL_BTN_PRIMARY);

        // Wire UIManager references
        if (uiMgr != null)
        {
            uiMgr.SummaryPanel = summaryPanel;
            uiMgr.SummaryTitleText = summaryTitle;
            uiMgr.SummaryDetailsText = summaryDetails;
            uiMgr.SummaryErrorsText = summaryErrors;
            EditorUtility.SetDirty(uiMgr);
        }

        // Wire Return button
        if (gymMgr != null)
        {
            UnityEventTools.AddPersistentListener(returnBtn.onClick,
                new UnityEngine.Events.UnityAction(gymMgr.ReturnToMenu));
        }

        // ──────────────────────────────────────────
        // 5. DEFAULT VISIBILITY
        // ──────────────────────────────────────────
        instrPanel.SetActive(false);   // GymManager.Start() will enable it
        summaryPanel.SetActive(false); // UIManager enables on GameState.Summary

        Selection.activeGameObject = canvasGO;
        Debug.Log("<color=cyan>[PhysioGuide]</color> Gym UI built on dedicated ScreenSpaceOverlay canvas and wired!");
    }

    // ──────────────────────────────────────────
    // CLEANUP HELPER
    // ──────────────────────────────────────────
    private static void CleanupOrphaned(string name)
    {
        // Find all GameObjects with this name and remove ones that were generated
        foreach (var go in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
        {
            if (go.gameObject.name == name && go.transform.parent != null)
            {
                // Only remove if it's directly under a Canvas (generated panels)
                Canvas parentCanvas = go.transform.parent.GetComponent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log($"[GymUIBuilder] Cleaning up orphaned '{name}' from '{parentCanvas.gameObject.name}'.");
                    Undo.DestroyObjectImmediate(go.gameObject);
                }
            }
        }
    }

    // ──────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────

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
        img.type = Image.Type.Sliced;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f, 1f);
        cb.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f, 1f);
        btn.colors = cb;

        CreateText("Label", go.transform, Vector2.zero, size, label, fontSize,
            Color.white, FontStyles.Bold);

        return btn;
    }

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
}
#endif
