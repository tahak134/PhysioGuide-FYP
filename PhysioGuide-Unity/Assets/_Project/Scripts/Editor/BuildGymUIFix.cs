using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

public class BuildGymUIFix
{
    public static void Execute()
    {
        GymManager gm = Object.FindObjectOfType<GymManager>();
        if (gm == null) { Debug.LogError("GymManager missing"); return; }
        
        GameObject canvas = GameObject.Find("GymUI_OverlayCanvas");
        if (canvas == null) { Debug.LogError("OverlayCanvas missing"); return; }

        // 1. Fix CountdownText
        Transform cd = canvas.transform.Find("CountdownText");
        if (cd) {
            RectTransform cdRt = cd.GetComponent<RectTransform>();
            cdRt.anchorMin = new Vector2(0, 0.5f);
            cdRt.anchorMax = new Vector2(1, 0.5f);
            cdRt.offsetMin = new Vector2(50, -100);
            cdRt.offsetMax = new Vector2(-50, 100);
            cdRt.pivot = new Vector2(0.5f, 0.5f);
            
            TextMeshProUGUI tmp = cd.GetComponent<TextMeshProUGUI>();
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 36;
            tmp.fontSizeMax = 120; // Let it shrink if message is long
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }

        // 2. Create HUDPanel if it doesn't exist
        Transform hud = canvas.transform.Find("HUDPanel");
        if (hud) Object.DestroyImmediate(hud.gameObject);

        GameObject hudPanelGO = new GameObject("HUDPanel", typeof(RectTransform));
        hudPanelGO.transform.SetParent(canvas.transform, false);
        RectTransform hudRt = hudPanelGO.GetComponent<RectTransform>();
        // Bottom-Right corner
        hudRt.anchorMin = new Vector2(1, 0);
        hudRt.anchorMax = new Vector2(1, 0);
        hudRt.pivot = new Vector2(1, 0);
        hudRt.anchoredPosition = new Vector2(-50, 50); // Padding
        hudRt.sizeDelta = new Vector2(250, 100);

        // Pause Button
        GameObject pauseBtnGO = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        pauseBtnGO.transform.SetParent(hudPanelGO.transform, false);
        RectTransform pbRt = pauseBtnGO.GetComponent<RectTransform>();
        pbRt.anchorMin = new Vector2(0, 0);
        pbRt.anchorMax = new Vector2(0.5f, 1);
        pbRt.sizeDelta = new Vector2(-10, 0); // Padding between buttons
        pbRt.anchoredPosition = new Vector2(-5, 0);
        pauseBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.8f);
        Button pauseBtn = pauseBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(pauseBtn.onClick, gm.TogglePause);

        GameObject pauseTxtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        pauseTxtGO.transform.SetParent(pauseBtnGO.transform, false);
        RectTransform ptRt = pauseTxtGO.GetComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one; ptRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI pTmp = pauseTxtGO.GetComponent<TextMeshProUGUI>();
        pTmp.text = "Pause"; pTmp.color = Color.white; pTmp.alignment = TextAlignmentOptions.Center; pTmp.fontSize = 28;

        // Stop Button
        GameObject stopBtnGO = new GameObject("StopButton", typeof(RectTransform), typeof(Image), typeof(Button));
        stopBtnGO.transform.SetParent(hudPanelGO.transform, false);
        RectTransform sbRt = stopBtnGO.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.5f, 0);
        sbRt.anchorMax = new Vector2(1, 1);
        sbRt.sizeDelta = new Vector2(-10, 0);
        sbRt.anchoredPosition = new Vector2(5, 0);
        stopBtnGO.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f);
        Button stopBtn = stopBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(stopBtn.onClick, gm.RequestStop);

        GameObject stopTxtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        stopTxtGO.transform.SetParent(stopBtnGO.transform, false);
        RectTransform stRt = stopTxtGO.GetComponent<RectTransform>();
        stRt.anchorMin = Vector2.zero; stRt.anchorMax = Vector2.one; stRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI sTmp = stopTxtGO.GetComponent<TextMeshProUGUI>();
        sTmp.text = "Stop"; sTmp.color = Color.white; sTmp.alignment = TextAlignmentOptions.Center; sTmp.fontSize = 28;

        // 3. Create StopConfirmationPanel
        Transform conf = canvas.transform.Find("StopConfirmationPanel");
        if (conf) Object.DestroyImmediate(conf.gameObject);

        GameObject confPanelGO = new GameObject("StopConfirmationPanel", typeof(RectTransform), typeof(Image));
        confPanelGO.transform.SetParent(canvas.transform, false);
        RectTransform cpRt = confPanelGO.GetComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0.5f, 0.5f); cpRt.anchorMax = new Vector2(0.5f, 0.5f); cpRt.pivot = new Vector2(0.5f, 0.5f);
        cpRt.sizeDelta = new Vector2(600, 300);
        confPanelGO.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark background

        GameObject confTxtGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        confTxtGO.transform.SetParent(confPanelGO.transform, false);
        RectTransform ctRt = confTxtGO.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0, 0.6f); ctRt.anchorMax = new Vector2(1, 1); ctRt.offsetMin = Vector2.zero; ctRt.offsetMax = Vector2.zero;
        TextMeshProUGUI cTmp = confTxtGO.GetComponent<TextMeshProUGUI>();
        cTmp.text = "Are you sure you want to stop the exercise early?";
        cTmp.color = Color.white; cTmp.alignment = TextAlignmentOptions.Center; cTmp.fontSize = 32; cTmp.enableWordWrapping = true;

        // Yes Button
        GameObject yesBtnGO = new GameObject("YesButton", typeof(RectTransform), typeof(Image), typeof(Button));
        yesBtnGO.transform.SetParent(confPanelGO.transform, false);
        RectTransform ybRt = yesBtnGO.GetComponent<RectTransform>();
        ybRt.anchorMin = new Vector2(0.1f, 0.2f); ybRt.anchorMax = new Vector2(0.45f, 0.5f); ybRt.offsetMin = Vector2.zero; ybRt.offsetMax = Vector2.zero;
        yesBtnGO.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f); // Red
        Button yesBtn = yesBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(yesBtn.onClick, gm.ConfirmStop);

        GameObject yesTxtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        yesTxtGO.transform.SetParent(yesBtnGO.transform, false);
        RectTransform ytRt = yesTxtGO.GetComponent<RectTransform>();
        ytRt.anchorMin = Vector2.zero; ytRt.anchorMax = Vector2.one; ytRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI yTmp = yesTxtGO.GetComponent<TextMeshProUGUI>();
        yTmp.text = "Yes, Stop"; yTmp.color = Color.white; yTmp.alignment = TextAlignmentOptions.Center; yTmp.fontSize = 28;

        // Cancel Button
        GameObject noBtnGO = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
        noBtnGO.transform.SetParent(confPanelGO.transform, false);
        RectTransform nbRt = noBtnGO.GetComponent<RectTransform>();
        nbRt.anchorMin = new Vector2(0.55f, 0.2f); nbRt.anchorMax = new Vector2(0.9f, 0.5f); nbRt.offsetMin = Vector2.zero; nbRt.offsetMax = Vector2.zero;
        noBtnGO.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f); // Gray
        Button noBtn = noBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(noBtn.onClick, gm.CancelStop);

        GameObject noTxtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        noTxtGO.transform.SetParent(noBtnGO.transform, false);
        RectTransform ntRt = noTxtGO.GetComponent<RectTransform>();
        ntRt.anchorMin = Vector2.zero; ntRt.anchorMax = Vector2.one; ntRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI nTmp = noTxtGO.GetComponent<TextMeshProUGUI>();
        nTmp.text = "Cancel"; nTmp.color = Color.white; nTmp.alignment = TextAlignmentOptions.Center; nTmp.fontSize = 28;

        // Hide HUD by default
        hudPanelGO.SetActive(false);
        confPanelGO.SetActive(false);

        // Update GymManager state explicitly via SerializedObject
        var so = new SerializedObject(gm);
        so.Update();
        so.FindProperty("instructionPanel").objectReferenceValue = canvas.transform.Find("InstructionPanel").gameObject;
        so.FindProperty("countdownText").objectReferenceValue = cd.GetComponent<TextMeshProUGUI>();
        // Wait, GymManager doesn't expose HUDPanel public fields but it finds it dynamically in Start. Perfect.
        so.ApplyModifiedProperties();

        // 4. Update SummaryPanel ReturnToMenu logic just in case
        Transform retBtn = canvas.transform.Find("SummaryPanel/ReturnToMenu_Button");
        if (retBtn) {
            Button btn = retBtn.GetComponent<Button>();
            for(int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--) {
                UnityEventTools.RemovePersistentListener(btn.onClick, i);
            }
            UnityEventTools.AddPersistentListener(btn.onClick, gm.ReturnToMenu);
        }

        // Save
        EditorUtility.SetDirty(gm);
        EditorUtility.SetDirty(canvas);
        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, activeScene.path);
        
        Debug.Log("Gym UI Overhauled & Saved.");
    }
}
