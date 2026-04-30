using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class FixUIHierarchy
{
    public static void Execute()
    {
        string scenePath = "Assets/_Project/Scenes/MenuScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        Debug.Log("Scene opened: " + scene.name);
        
        var allGOs = Object.FindObjectsOfType<GameObject>(true);
        GameObject historyTextObj = null;
        
        foreach (var go in allGOs)
        {
            if (go.name == "historyContentText" || go.name == "HistoryContentText")
            {
                historyTextObj = go;
                break;
            }
        }
        
        if (historyTextObj != null)
        {
            Debug.Log($"Found historyText on {historyTextObj.name}");
            
            // Check RectTransform
            RectTransform rt = historyTextObj.GetComponent<RectTransform>();
            if (rt != null) {
                Debug.Log($"RectTransform sizeDelta: {rt.sizeDelta}");
                Debug.Log($"RectTransform anchorMin: {rt.anchorMin}, anchorMax: {rt.anchorMax}");
            }
            
            // Check ContentSizeFitter
            ContentSizeFitter csf = historyTextObj.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                Debug.Log("ContentSizeFitter is MISSING! Adding it now...");
                csf = historyTextObj.AddComponent<ContentSizeFitter>();
            }
            
            if (csf.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
            {
                Debug.Log($"ContentSizeFitter verticalFit was {csf.verticalFit}. Changing to PreferredSize.");
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Check mask on parent
            Transform parent = historyTextObj.transform.parent;
            while (parent != null)
            {
                var mask = parent.GetComponent<Mask>();
                var rectMask = parent.GetComponent<RectMask2D>();
                if (mask != null) Debug.Log($"Found Mask on parent: {parent.name}");
                if (rectMask != null) Debug.Log($"Found RectMask2D on parent: {parent.name}");
                
                var vlg = parent.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    Debug.Log($"Found VerticalLayoutGroup on parent: {parent.name}");
                    var parentCsf = parent.GetComponent<ContentSizeFitter>();
                    if (parentCsf == null)
                    {
                        Debug.Log($"Parent {parent.name} is missing ContentSizeFitter! Adding one...");
                        parentCsf = parent.gameObject.AddComponent<ContentSizeFitter>();
                        parentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }
                    else if (parentCsf.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                    {
                        Debug.Log($"Parent {parent.name} ContentSizeFitter verticalFit was {parentCsf.verticalFit}. Changing to PreferredSize.");
                        parentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }
                }

                parent = parent.parent;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("Scene saved successfully.");
        }
        else
        {
            Debug.LogError("Could not find historyContentText GameObject in MenuScene.");
        }
    }
}
