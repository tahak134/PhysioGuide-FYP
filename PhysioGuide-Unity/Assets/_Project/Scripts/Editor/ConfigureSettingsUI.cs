using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ConfigureSettingsUI
{
    public static void Execute()
    {
        MainMenuManager mgr = Object.FindObjectOfType<MainMenuManager>();
        if (mgr == null || mgr.settingsPanel == null) { Debug.LogError("Manager or panel not found"); return; }
        
        Transform panel = mgr.settingsPanel.transform;

        // Scaption
        Transform sLabel = panel.Find("ScaptionDurationLabel");
        Transform sInput = panel.Find("ScaptionDuration_Input");
        if (sLabel) {
            sLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, 90);
            sLabel.GetComponent<TextMeshProUGUI>().text = "Scaption Duration (s):";
        }
        if (sInput) {
            sInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(140, 90);
            TMP_InputField inf = sInput.GetComponent<TMP_InputField>();
            inf.contentType = TMP_InputField.ContentType.IntegerNumber;
            inf.text = "60";
        }

        // Shoulder Flexion
        Transform sfLabel = panel.Find("ShoulderFlexionDurationLabel");
        Transform sfInput = panel.Find("ShoulderFlexionDuration_Input");
        if (sfLabel) {
            sfLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, 10);
            sfLabel.GetComponent<TextMeshProUGUI>().text = "Sh. Flexion Duration (s):";
        }
        if (sfInput) {
            sfInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(140, 10);
            TMP_InputField sinf = sfInput.GetComponent<TMP_InputField>();
            sinf.contentType = TMP_InputField.ContentType.IntegerNumber;
            sinf.text = "60";
        }

        // Wiring
        var so = new SerializedObject(mgr);
        so.Update();
        if (sInput) so.FindProperty("scaptionDurationInput").objectReferenceValue = sInput.GetComponent<TMP_InputField>();
        if (sfInput) so.FindProperty("shoulderFlexionDurationInput").objectReferenceValue = sfInput.GetComponent<TMP_InputField>();
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);

        // Save
        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, activeScene.path);

        Debug.Log("Configuration and wiring complete.");
    }
}
