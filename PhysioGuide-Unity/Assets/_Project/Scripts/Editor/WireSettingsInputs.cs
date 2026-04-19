using UnityEngine;
using UnityEditor;
using TMPro;

public class WireSettingsInputs
{
    public static void Execute()
    {
        MainMenuManager mgr = Object.FindObjectOfType<MainMenuManager>();
        if (mgr == null) { Debug.LogError("MainMenuManager not found!"); return; }

        GameObject scaInput = GameObject.Find("MainMenu_Canvas/5_SettingsPanel/ScaptionDuration_Input");
        GameObject sfInput = GameObject.Find("MainMenu_Canvas/5_SettingsPanel/ShoulderFlexionDuration_Input");

        var so = new SerializedObject(mgr);
        so.Update();

        if (scaInput != null)
        {
            var prop = so.FindProperty("scaptionDurationInput");
            if (prop != null) prop.objectReferenceValue = scaInput.GetComponent<TMP_InputField>();
        }
        if (sfInput != null)
        {
            var prop = so.FindProperty("shoulderFlexionDurationInput");
            if (prop != null) prop.objectReferenceValue = sfInput.GetComponent<TMP_InputField>();
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
        Debug.Log("[WireSettingsInputs] Done! Wired scaptionDurationInput and shoulderFlexionDurationInput.");
    }
}
