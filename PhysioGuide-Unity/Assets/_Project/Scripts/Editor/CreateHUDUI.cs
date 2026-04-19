using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateHUDUI
{
    public static void Execute()
    {
        // Find Canvas and GymManager
        GameObject canvasVal = GameObject.Find("GymUI_OverlayCanvas");
        if (canvasVal == null) { Debug.LogError("canvas not found"); return; }
        
        GymManager gymMgr = Object.FindObjectOfType<GymManager>();
        
        // --- 1. HUD Panel ---
        GameObject hudPanel = new GameObject("HUDPanel", typeof(RectTransform));
        hudPanel.transform.SetParent(canvasVal.transform, false);
        RectTransform hudRT = hudPanel.GetComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero; hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = Vector2.zero; hudRT.offsetMax = Vector2.zero;
        
        // Pause Button
        GameObject pauseBtnObj = UnityEngine.UI.DefaultControls.CreateButton(new UnityEngine.UI.DefaultControls.Resources());
        pauseBtnObj.name = "PauseButton";
        pauseBtnObj.transform.SetParent(hudPanel.transform, false);
        RectTransform prt = pauseBtnObj.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(1, 0); prt.anchorMax = new Vector2(1, 0);
        prt.pivot = new Vector2(1, 0);
        prt.anchoredPosition = new Vector2(-180, 20); // Bottom Right, offset left
        prt.sizeDelta = new Vector2(150, 60);
        pauseBtnObj.GetComponentInChildren<Text>().text = "Pause";
        
        if (gymMgr != null) {
            Button pb = pauseBtnObj.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(pb.onClick, gymMgr.TogglePause);
        }

        // Stop Button
        GameObject stopBtnObj = UnityEngine.UI.DefaultControls.CreateButton(new UnityEngine.UI.DefaultControls.Resources());
        stopBtnObj.name = "StopButton";
        stopBtnObj.transform.SetParent(hudPanel.transform, false);
        RectTransform srt = stopBtnObj.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(1, 0); srt.anchorMax = new Vector2(1, 0);
        srt.pivot = new Vector2(1, 0);
        srt.anchoredPosition = new Vector2(-20, 20); // Bottom Right, edge
        srt.sizeDelta = new Vector2(150, 60);
        stopBtnObj.GetComponentInChildren<Text>().text = "Stop";
        
        if (gymMgr != null) {
            Button sb = stopBtnObj.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(sb.onClick, gymMgr.RequestStop);
        }

        // --- 2. StopConfirmationPanel ---
        GameObject confirmPanel = new GameObject("StopConfirmationPanel", typeof(RectTransform), typeof(Image));
        confirmPanel.transform.SetParent(hudPanel.transform, false);
        RectTransform crt = confirmPanel.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(500, 300);
        confirmPanel.GetComponent<Image>().color = new Color(0,0,0,0.8f);

        // Text
        GameObject txtObj = new GameObject("ConfirmText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(confirmPanel.transform, false);
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "Are you sure you want to stop?";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 32;
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(0, 0); trt.offsetMax = new Vector2(0, 0);

        // Confirm Yes
        GameObject yesBtn = UnityEngine.UI.DefaultControls.CreateButton(new UnityEngine.UI.DefaultControls.Resources());
        yesBtn.name = "YesButton";
        yesBtn.transform.SetParent(confirmPanel.transform, false);
        RectTransform yrt = yesBtn.GetComponent<RectTransform>();
        yrt.anchoredPosition = new Vector2(-100, -50);
        yesBtn.GetComponentInChildren<Text>().text = "Yes, Stop";
        if (gymMgr != null) {
            Button yb = yesBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(yb.onClick, gymMgr.ConfirmStop);
        }

        // Confirm No
        GameObject noBtn = UnityEngine.UI.DefaultControls.CreateButton(new UnityEngine.UI.DefaultControls.Resources());
        noBtn.name = "NoButton";
        noBtn.transform.SetParent(confirmPanel.transform, false);
        RectTransform nrt = noBtn.GetComponent<RectTransform>();
        nrt.anchoredPosition = new Vector2(100, -50);
        noBtn.GetComponentInChildren<Text>().text = "Cancel";
        if (gymMgr != null) {
            Button nb = noBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(nb.onClick, gymMgr.CancelStop);
        }

        confirmPanel.SetActive(false);
        hudPanel.SetActive(false); // Initially off

        // Make UIManager link to these if UIManager somehow exists
        UIManager uiMgr = Object.FindObjectOfType<UIManager>();
        if (uiMgr != null) {
            uiMgr.HUDPanel = hudPanel;
            UnityEditor.EditorUtility.SetDirty(uiMgr);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasVal.scene);
    }
}
