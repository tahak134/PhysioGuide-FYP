using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script to set up the Settings Panel with per-exercise duration inputs.
/// Run via MCP execute_script. Creates: ScaptionDuration_Label, ScaptionDuration_Input,
/// ShoulderFlexionDuration_Label, ShoulderFlexionDuration_Input.
/// Binds them to MainMenuManager fields and wires SaveSettings_Button.
/// </summary>
public class SetupSettingsPanel
{
    public static void Execute()
    {
        // Find the Settings_Panel in the MainMenu scene
        GameObject settingsPanel = GameObject.Find("MainMenu_Canvas/Settings_Panel");
        if (settingsPanel == null) { Debug.LogError("Settings_Panel not found!"); return; }

        MainMenuManager mgr = Object.FindObjectOfType<MainMenuManager>();
        if (mgr == null) { Debug.LogError("MainMenuManager not found!"); return; }

        RectTransform panelRT = settingsPanel.GetComponent<RectTransform>();

        // --- Scaption Duration ---
        // Label
        GameObject scaLabel = CreateLabel(settingsPanel.transform, "ScaptionDuration_Label",
            "Scaption Duration (sec)", new Vector2(0, -100));

        // Input
        GameObject scaInputGO = CreateInputField(settingsPanel.transform, "ScaptionDuration_Input",
            "60", new Vector2(130, -100));
        TMP_InputField scaInput = scaInputGO.GetComponent<TMP_InputField>();
        if (scaInput != null) scaInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        // --- Shoulder Flexion Duration ---
        // Label
        GameObject sfLabel = CreateLabel(settingsPanel.transform, "ShoulderFlexionDuration_Label",
            "Shoulder Flexion Duration (sec)", new Vector2(0, -160));

        // Input
        GameObject sfInputGO = CreateInputField(settingsPanel.transform, "ShoulderFlexionDuration_Input",
            "60", new Vector2(130, -160));
        TMP_InputField sfInput = sfInputGO.GetComponent<TMP_InputField>();
        if (sfInput != null) sfInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        // Bind to MainMenuManager
        var so = new SerializedObject(mgr);
        so.Update();
        var scaProp = so.FindProperty("scaptionDurationInput");
        var sfProp = so.FindProperty("shoulderFlexionDurationInput");
        if (scaProp != null && scaInput != null) scaProp.objectReferenceValue = scaInput;
        if (sfProp != null && sfInput != null) sfProp.objectReferenceValue = sfInput;
        so.ApplyModifiedProperties();

        // Wire SaveSettings_Button
        GameObject saveBtn = GameObject.Find("MainMenu_Canvas/Settings_Panel/SaveSettings_Button");
        if (saveBtn != null)
        {
            Button btn = saveBtn.GetComponent<Button>();
            if (btn != null)
            {
                // Clear existing listeners and add new one
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    btn.onClick,
                    mgr.OnSaveSettingsClicked
                );
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(settingsPanel.scene);
        Debug.Log("[SetupSettingsPanel] Done! Created per-exercise duration inputs and wired to MainMenuManager.");
    }

    static GameObject CreateLabel(Transform parent, string name, string text, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }

    static GameObject CreateInputField(Transform parent, string name, string defaultVal, Vector2 anchoredPos)
    {
        // Use DefaultControls to create a proper InputField, then convert to TMP
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.55f, 1);
        rt.anchorMax = new Vector2(0.85f, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(0, 35);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        // Text Area child
        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        RectTransform taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(5, 2); taRT.offsetMax = new Vector2(-5, -2);

        // Text child
        GameObject textChild = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textChild.transform.SetParent(textArea.transform, false);
        RectTransform tcRT = textChild.GetComponent<RectTransform>();
        tcRT.anchorMin = Vector2.zero; tcRT.anchorMax = Vector2.one;
        tcRT.offsetMin = Vector2.zero; tcRT.offsetMax = Vector2.zero;
        TextMeshProUGUI textTMP = textChild.GetComponent<TextMeshProUGUI>();
        textTMP.fontSize = 18;
        textTMP.color = Color.white;
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Placeholder child
        GameObject placeholderChild = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderChild.transform.SetParent(textArea.transform, false);
        RectTransform phRT = placeholderChild.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
        TextMeshProUGUI phTMP = placeholderChild.GetComponent<TextMeshProUGUI>();
        phTMP.text = defaultVal;
        phTMP.fontSize = 18;
        phTMP.color = new Color(1, 1, 1, 0.5f);
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Wire up TMP_InputField
        TMP_InputField inputField = go.GetComponent<TMP_InputField>();
        inputField.textComponent = textTMP;
        inputField.placeholder = phTMP;
        inputField.textViewport = taRT;
        inputField.text = defaultVal;

        return go;
    }
}
