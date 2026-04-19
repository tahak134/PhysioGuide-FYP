using UnityEngine;
using UnityEditor;

public class AddFirebaseManager
{
    public static void Execute()
    {
        // Check if FirebaseManager already exists
        FirebaseManager existing = Object.FindObjectOfType<FirebaseManager>();
        if (existing != null)
        {
            Debug.Log("FirebaseManager already exists.");
            return;
        }

        GameObject fmObj = new GameObject("FirebaseManager");
        FirebaseManager fm = fmObj.AddComponent<FirebaseManager>();
        
        fm.apiKey = "AIzaSyBX1KT-n6F3qWz1tWz33j-k4to8ns4-TCY";
        fm.projectId = "physioguide-dashboard";

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fmObj.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("Added and configured FirebaseManager.");
    }
}
