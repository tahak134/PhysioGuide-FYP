using UnityEngine;
using UnityEditor;
using TMPro;

public class WireIPSetting
{
    public static void Execute()
    {
        MainMenuManager mgr = Object.FindObjectOfType<MainMenuManager>();
        if (mgr == null) { Debug.LogError("MainMenuManager not found!"); return; }

        GameObject ipInputGO = GameObject.Find("MainMenu_Canvas/5_SettingsPanel/IP_Input");
        
        // Sometimes it's called Settings_Panel or SettingsPanel depending on what was used. Let's find via component type or children
        if (ipInputGO == null)
        {
            // Search globally for IP_Input
            var allInputs = Object.FindObjectsOfType<TMP_InputField>(true);
            foreach(var inp in allInputs)
            {
                if (inp.gameObject.name == "IP_Input" || inp.gameObject.name == "IPAddress_Input")
                {
                    ipInputGO = inp.gameObject;
                    break;
                }
            }
        }

        if (ipInputGO == null)
        {
            Debug.LogError("[WireIPSetting] Could not find IP_Input GameObject in scene!");
            return;
        }

        var so = new SerializedObject(mgr);
        so.Update();

        var prop = so.FindProperty("ipAddressInput");
        if (prop != null) 
        {
            prop.objectReferenceValue = ipInputGO.GetComponent<TMP_InputField>();
            Debug.Log("[WireIPSetting] Wired IP field successfully.");
        }
        else
        {
            Debug.LogError("[WireIPSetting] Could not find ipAddressInput property on MainMenuManager!");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(mgr.gameObject.scene, mgr.gameObject.scene.path);
        Debug.Log("[WireIPSetting] Done!");
    }
}
