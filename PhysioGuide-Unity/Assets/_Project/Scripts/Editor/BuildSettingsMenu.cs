using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class BuildSettingsMenu
{
    public static void Execute()
    {
        MainMenuManager mgr = Object.FindObjectOfType<MainMenuManager>();
        if (mgr == null) { Debug.LogError("[BuildSettingsMenu] MainMenuManager not found!"); return; }

        if (mgr.settingsPanel == null) { Debug.LogError("[BuildSettingsMenu] mgr.settingsPanel is null!"); return; }
        Transform settingsPanel = mgr.settingsPanel.transform;

        // Find reference IP fields to duplicate
        GameObject ipLabel = settingsPanel.Find("IPLabel")?.gameObject;
        GameObject ipInput = settingsPanel.Find("IP_Input")?.gameObject;
        if (ipLabel == null || ipInput == null) { Debug.LogError("[BuildSettingsMenu] IPLabel or IP_Input not found!"); return; }

        // --- Remove any existing ones to avoid dupes ---
        Transform oldScaL = settingsPanel.Find("ScaptionDurationLabel");
        Transform oldScaI = settingsPanel.Find("ScaptionDuration_Input");
        Transform oldSfL = settingsPanel.Find("ShoulderFlexionDurationLabel");
        Transform oldSfI = settingsPanel.Find("ShoulderFlexionDuration_Input");
        
        if (oldScaL) Object.DestroyImmediate(oldScaL.gameObject);
        if (oldScaI) Object.DestroyImmediate(oldScaI.gameObject);
        if (oldSfL) Object.DestroyImmediate(oldSfL.gameObject);
        if (oldSfI) Object.DestroyImmediate(oldSfI.gameObject);

        // --- Create Scaption Fields (-80 Y offset from IP) ---
        GameObject scaLabel = Object.Instantiate(ipLabel, settingsPanel);
        scaLabel.name = "ScaptionDurationLabel";
        TextMeshProUGUI scaTMP = scaLabel.GetComponent<TextMeshProUGUI>();
        scaTMP.text = "Scaption Duration (s):";
        RectTransform scaLRT = scaLabel.GetComponent<RectTransform>();
        scaLRT.anchoredPosition = new Vector2(scaLRT.anchoredPosition.x, scaLRT.anchoredPosition.y - 80);

        GameObject scaInputGO = Object.Instantiate(ipInput, settingsPanel);
        scaInputGO.name = "ScaptionDuration_Input";
        RectTransform scaIRT = scaInputGO.GetComponent<RectTransform>();
        scaIRT.anchoredPosition = new Vector2(scaIRT.anchoredPosition.x, scaIRT.anchoredPosition.y - 80);
        TMP_InputField scaInput = scaInputGO.GetComponent<TMP_InputField>();
        scaInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        scaInput.text = "60";

        // --- Create Shoulder Flexion Fields (-160 Y offset from IP) ---
        GameObject sfLabel = Object.Instantiate(ipLabel, settingsPanel);
        sfLabel.name = "ShoulderFlexionDurationLabel";
        TextMeshProUGUI sfTMP = sfLabel.GetComponent<TextMeshProUGUI>();
        sfTMP.text = "Sh. Flexion Duration (s):";
        RectTransform sfLRT = sfLabel.GetComponent<RectTransform>();
        sfLRT.anchoredPosition = new Vector2(sfLRT.anchoredPosition.x, sfLRT.anchoredPosition.y - 160);

        GameObject sfInputGO = Object.Instantiate(ipInput, settingsPanel);
        sfInputGO.name = "ShoulderFlexionDuration_Input";
        RectTransform sfIRT = sfInputGO.GetComponent<RectTransform>();
        sfIRT.anchoredPosition = new Vector2(sfIRT.anchoredPosition.x, sfIRT.anchoredPosition.y - 160);
        TMP_InputField sfInput = sfInputGO.GetComponent<TMP_InputField>();
        sfInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        sfInput.text = "60";

        // --- Wire to MainMenuManager ---
        var so = new SerializedObject(mgr);
        so.Update();
        so.FindProperty("scaptionDurationInput").objectReferenceValue = scaInput;
        so.FindProperty("shoulderFlexionDurationInput").objectReferenceValue = sfInput;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);

        // --- Save Scene ---
        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, activeScene.path);

        Debug.Log($"[BuildSettingsMenu] Successfully created per-exercise settings and saved {activeScene.path}");
    }
}
