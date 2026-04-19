using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FinalizeGymUI
{
    public static void Execute()
    {
        GameObject hudPanel = GameObject.Find("GymUI_OverlayCanvas/HUDPanel");
        if (hudPanel != null)
        {
            Transform confirmPanel = hudPanel.transform.Find("StopConfirmationPanel");
            if (confirmPanel != null)
                confirmPanel.gameObject.SetActive(false);
                
            hudPanel.SetActive(false);
            Debug.Log("[FinalizeGymUI] Deactivated StopConfirmationPanel and HUDPanel for initial state.");
        }
        
        // Ensure we save the correct active scene
        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, activeScene.path);
        Debug.Log($"[FinalizeGymUI] Saved scene to {activeScene.path}");
    }
}
