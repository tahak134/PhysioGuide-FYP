using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Comprehensive editor script to:
/// 1. Populate HUDPanel with Pause + Stop buttons and StopConfirmationPanel
/// 2. Wire ReturnToMenu_Button onClick to GymManager.ReturnToMenu
/// Run on GymScene.
/// </summary>
public class BuildGymUI
{
    public static void Execute()
    {
        // Find required objects
        GameObject canvas = GameObject.Find("GymUI_OverlayCanvas");
        if (canvas == null) { Debug.LogError("[BuildGymUI] GymUI_OverlayCanvas not found!"); return; }

        GymManager gymMgr = Object.FindObjectOfType<GymManager>();
        if (gymMgr == null) { Debug.LogError("[BuildGymUI] GymManager not found!"); return; }

        // --- 1. POPULATE HUD PANEL ---
        Transform hudTransform = canvas.transform.Find("HUDPanel");
        if (hudTransform == null) { Debug.LogError("[BuildGymUI] HUDPanel not found!"); return; }
        GameObject hudPanel = hudTransform.gameObject;

        // Remove any existing children to avoid duplicates
        for (int i = hudPanel.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(hudPanel.transform.GetChild(i).gameObject);

        // --- Pause Button (bottom-right, offset left) ---
        GameObject pauseBtn = CreateTMPButton(hudPanel.transform, "PauseButton", "Pause",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-190, 20), new Vector2(160, 55),
            new Color(0.2f, 0.6f, 0.8f, 0.9f));

        Button pauseButton = pauseBtn.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(pauseButton.onClick, gymMgr.TogglePause);

        // --- Stop Button (bottom-right, edge) ---
        GameObject stopBtn = CreateTMPButton(hudPanel.transform, "StopButton", "Stop",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-20, 20), new Vector2(160, 55),
            new Color(0.8f, 0.2f, 0.2f, 0.9f));

        Button stopButton = stopBtn.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(stopButton.onClick, gymMgr.RequestStop);

        // --- Stop Confirmation Panel (centered overlay) ---
        GameObject confirmPanel = new GameObject("StopConfirmationPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        confirmPanel.transform.SetParent(hudPanel.transform, false);
        RectTransform crt = confirmPanel.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(500, 280);
        crt.anchoredPosition = Vector2.zero;
        confirmPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        // Confirm Text
        GameObject confirmText = new GameObject("ConfirmText",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        confirmText.transform.SetParent(confirmPanel.transform, false);
        RectTransform ctRT = confirmText.GetComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(0.1f, 0.55f);
        ctRT.anchorMax = new Vector2(0.9f, 0.95f);
        ctRT.offsetMin = Vector2.zero;
        ctRT.offsetMax = Vector2.zero;
        TextMeshProUGUI ctTMP = confirmText.GetComponent<TextMeshProUGUI>();
        ctTMP.text = "Are you sure you want\nto stop the exercise?";
        ctTMP.fontSize = 28;
        ctTMP.color = Color.white;
        ctTMP.alignment = TextAlignmentOptions.Center;

        // Yes Button
        GameObject yesBtn = CreateTMPButton(confirmPanel.transform, "YesButton", "Yes, Stop",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-110, -70), new Vector2(180, 55),
            new Color(0.8f, 0.2f, 0.2f, 1f));

        Button yesButton = yesBtn.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(yesButton.onClick, gymMgr.ConfirmStop);

        // No Button
        GameObject noBtn = CreateTMPButton(confirmPanel.transform, "NoButton", "Cancel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(110, -70), new Vector2(180, 55),
            new Color(0.3f, 0.3f, 0.4f, 1f));

        Button noButton = noBtn.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(noButton.onClick, gymMgr.CancelStop);

        // Start hidden
        confirmPanel.SetActive(false);

        // --- 2. WIRE RETURN TO MENU BUTTON ---
        Transform returnBtnT = canvas.transform.Find("SummaryPanel/ReturnToMenu_Button");
        if (returnBtnT != null)
        {
            Button returnBtn = returnBtnT.GetComponent<Button>();
            if (returnBtn != null)
            {
                // Clear existing listeners first
                int count = returnBtn.onClick.GetPersistentEventCount();
                for (int i = count - 1; i >= 0; i--)
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(returnBtn.onClick, i);

                UnityEditor.Events.UnityEventTools.AddPersistentListener(returnBtn.onClick, gymMgr.ReturnToMenu);
                Debug.Log("[BuildGymUI] Wired ReturnToMenu_Button -> GymManager.ReturnToMenu()");
            }
        }
        else
        {
            Debug.LogWarning("[BuildGymUI] ReturnToMenu_Button not found!");
        }

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log("[BuildGymUI] Done! HUD buttons created, confirmation panel added, ReturnToMenu wired.");
    }

    /// <summary>
    /// Helper to create a button with TMP text (not legacy Text).
    /// </summary>
    static GameObject CreateTMPButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        // Button root
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnGO.GetComponent<Image>();
        img.color = bgColor;
        btnGO.GetComponent<Button>().targetGraphic = img;

        // TMP Label child
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(btnGO.transform, false);
        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btnGO;
    }
}
