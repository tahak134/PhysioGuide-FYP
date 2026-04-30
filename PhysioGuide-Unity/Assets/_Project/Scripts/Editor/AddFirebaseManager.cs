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
        
        fm.supabaseUrl = "https://yuqrjilbdvxcenivppci.supabase.co";
        fm.supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Inl1cXJqaWxiZHZ4Y2VuaXZwcGNpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc0NDA4MzMsImV4cCI6MjA5MzAxNjgzM30.r94G_PBHG5xab1-jBdiSgtOxdtnD_7lD4r1To5Kp9L0";
        fm.supabaseBucket = "session-recordings";

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fmObj.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("Added and configured FirebaseManager with Supabase credentials.");
    }
}
