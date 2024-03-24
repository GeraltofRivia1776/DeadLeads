using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;

// Custom attribute to mark prefab usage in scenes
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
public class PrefabUsedInSceneAttribute : System.Attribute { }

public class AssetDestroyer : EditorWindow
{
    private Vector2 scrollPosition;
    private Dictionary<string, bool> unusedAssets = new Dictionary<string, bool>();
    private List<string> assetsToDelete = new List<string>();
    private bool selectAllToggle = false;
    private Texture2D logoTexture;
    private float buttonHeight = 30; // Adjust the button height as needed

    private float maxButtonScale = 0.2f;

    private float previewImageSize = 64; // Adjust the preview image size as needed
    private string versionNumber = "v1.0"; // Your version number here

    private GUIStyle tooltipStyle; // Custom GUIStyle for tooltips

    [MenuItem("Tools/Asset Destroyer")]
    public static void ShowWindow()
    {
        GetWindow<AssetDestroyer>("Asset Destroyer");
    }

    private void OnGUI()
    {
        // Display the version number in the top-right corner
        Rect versionRect = new Rect(position.width - 60, 0, 60, 20);
        GUIStyle versionStyle = new GUIStyle(EditorStyles.label);
        versionStyle.alignment = TextAnchor.UpperRight;
        EditorGUI.LabelField(versionRect, versionNumber, versionStyle);
        logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/AssetDestroyer/ADLogo.png");


        // Display the logo at the top center if it's loaded
        if (logoTexture != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Centers the logo

            // Display the logo with a specific size
            GUILayout.Label(logoTexture, GUILayout.Width(95), GUILayout.Height(95));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // Create a custom GUIStyle for tooltips within OnGUI
        if (tooltipStyle == null)
        {
            tooltipStyle = new GUIStyle(GUI.skin.box);
            tooltipStyle.normal.textColor = Color.white; // Text color
            tooltipStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.7f)); // Background color (semi-transparent black)
        }

        float windowWidth = position.width; // Get the current width of the window

        // Use vertical layout if the window is too narrow
        if (windowWidth < 300) // You can adjust this threshold
        {
            GUILayout.BeginVertical();

            // Vertical layout for main buttons
            if (GUILayout.Button("Find Unused", GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight)))
            {
                FindUnusedAssets();
            }

            if (GUILayout.Button(selectAllToggle ? "Deselect All" : "Select All", GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight)))
            {
                SelectDeselectAll();
            }

            if (GUILayout.Button("Destroy Assets", GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight)))
            {
                DeleteSelectedAssets();
            }

            GUILayout.EndVertical();
        }
        else
        {
            GUILayout.BeginHorizontal();

            // Horizontal layout for main buttons
            if (GUILayout.Button("Find Unused", GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight)))
            {
                FindUnusedAssets();
            }

            if (GUILayout.Button(selectAllToggle ? "Deselect All" : "Select All", GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight)))
            {
                SelectDeselectAll();
            }

            if (GUILayout.Button("Destroy Assets", GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight)))
            {
                DeleteSelectedAssets();
            }

            GUILayout.EndHorizontal();
        }

        // Define thresholds
        float veryNarrowWidthThreshold = 300; // Adjust this threshold for very narrow layout
        bool isVeryNarrow = windowWidth < veryNarrowWidthThreshold;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        if (unusedAssets.Count > 0)
        {
            foreach (var asset in unusedAssets.OrderBy(a => a.Key))
            {
                EditorGUILayout.BeginHorizontal();

                // Display the toggle
                unusedAssets[asset.Key] = EditorGUILayout.Toggle("", unusedAssets[asset.Key], GUILayout.Width(20), GUILayout.Height(buttonHeight));

                // Get the asset preview texture
                Texture2D assetPreviewTexture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Key));

                // Define a maximum scale factor to control the maximum size of the preview image
                float maxPreviewImageScale = 1.2f; // You can adjust this as needed
                                                   // Calculate the dynamic width based on a percentage of the window width
                float previewImageScale = Mathf.Clamp01(windowWidth / 5f); // You can adjust the divisor as needed
                float dynamicWidth = Mathf.Min(previewImageSize * previewImageScale * maxPreviewImageScale, windowWidth * maxButtonScale);
                float dynamicHeight = dynamicWidth * (assetPreviewTexture.height / assetPreviewTexture.width);

                if (assetPreviewTexture != null && asset.Key != null)
                {
                    // Display the asset preview image as a clickable label
                    GUIContent assetLabel = new GUIContent(assetPreviewTexture);
                    Rect imageRect = GUILayoutUtility.GetRect(assetLabel, GUIStyle.none, GUILayout.Width(dynamicWidth), GUILayout.Height(dynamicHeight));

                    if (GUI.Button(imageRect, assetLabel, GUIStyle.none))
                    {
                        // Clicking on the image triggers the "View" action
                        SelectAndPingAsset(asset.Key);
                    }

                    // Check if the mouse is hovering over the image
                    if (imageRect.Contains(Event.current.mousePosition))
                    {
                        // Display a tooltip when hovering over the image
                        GUIContent tooltipContent = new GUIContent("Click to view in your project window");
                        GUIStyle tooltipStyle = GUI.skin.box;
                        tooltipStyle.normal.textColor = Color.white;
                        tooltipStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.7f));

                        // Show the tooltip label
                        GUI.Label(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 215, 20), tooltipContent, tooltipStyle);
                    }
                }

                // Create a label for the file name with word wrap
                EditorGUILayout.LabelField(asset.Key, GUILayout.Height(buttonHeight));

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }



    // Helper method to create a texture with a specific color
    private Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = color;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void SelectAndPingAsset(string assetPath)
    {
        var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }

    private void FindUnusedAssets()
    {
        unusedAssets.Clear();
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
        int totalAssets = allAssetPaths.Length;
        int currentAssetIndex = 0;

        foreach (string assetPath in allAssetPaths)
        {
            // Update the progress bar
            if (EditorUtility.DisplayCancelableProgressBar("Finding Unused Assets", $"Checking: {assetPath}", (float)currentAssetIndex / totalAssets))
            {
                // User clicked the Cancel button
                EditorUtility.ClearProgressBar();
                Debug.Log("Unused asset search canceled by user.");
                return;
            }

            currentAssetIndex++;

            if (!assetPath.StartsWith("Assets/") || assetPath.StartsWith("Assets/Editor"))
                continue;

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                if (!IsFolderUsed(assetPath))
                {
                    unusedAssets[assetPath] = false;
                }
            }
            else if (!IsAssetUsed(assetPath))
            {
                unusedAssets[assetPath] = false;
            }
        }

        // Clear the progress bar when done
        EditorUtility.ClearProgressBar();

        Debug.Log($"Found {unusedAssets.Count} unused assets.");
    }



    private bool IsAssetUsed(string assetPath)
    {
        var currentScene = SceneManager.GetActiveScene().path;
        if (assetPath == currentScene)
        {
            return true; // The current scene is always considered as used
        }

        if (IsAssetUsedInScenes(assetPath))
        {
            return true;
        }

        if (IsAssetUsedInPrefabs(assetPath))
        {
            return true;
        }

        // Check if the asset is used as a material
        if (IsMaterialUsed(assetPath))
        {
            return true;
        }

        // Additional check for prefab instances
        if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(GameObject))
        {
            if (IsPrefabUsedInScene(assetPath))
            {
                return true;
            }
        }

        return false;
    }


    private bool IsAssetUsedInScenes(string assetPath)
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (IsAssetUsedInGameObject(obj, assetPath))
                {
                    Debug.Log($"Asset {assetPath} is used in scene: {scenePath}");
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsFolderUsed(string folderPath)
    {
        string[] assetGUIDs = AssetDatabase.FindAssets("", new[] { folderPath });
        foreach (string guid in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsAssetUsed(path))
            {
                return true; // If any asset in the folder is used, the folder is used
            }
        }
        return false; // The folder is unused if none of the assets in it are used
    }


    private bool IsMaterialUsed(string assetPath)
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.sharedMaterial != null)
                    {
                        string materialPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
                        if (materialPath == assetPath)
                        {
                            Debug.Log($"Material {assetPath} is used in scene: {scenePath}");
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }


    private bool IsPrefabUsed(string prefabPath)
    {
        string[] allScenes = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string scenePath in allScenes)
        {
            string[] dependencies = AssetDatabase.GetDependencies(scenePath);
            if (dependencies.Contains(prefabPath))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPrefabUsedInScene(string prefabPath)
    {
        UnityEngine.GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
        if (prefab == null)
            return false;

        string[] scenePaths = UnityEditor.AssetDatabase.FindAssets("t:Scene")
            .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string scenePath in scenePaths)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            // Check if the prefab is used as an instance in the scene
            UnityEngine.GameObject[] sceneObjects = scene.GetRootGameObjects();
            foreach (UnityEngine.GameObject sceneObject in sceneObjects)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(sceneObject))
                {
                    UnityEngine.GameObject rootPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(sceneObject);
                    if (rootPrefab == prefab)
                    {
                        // The prefab is used as an instance in this scene
                        return true;
                    }
                }
            }

            // Check if the prefab is used as a reference in the scene
            UnityEngine.GameObject[] roots = scene.GetRootGameObjects();
            foreach (UnityEngine.GameObject root in roots)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab)
                {
                    // The prefab is used as a reference in this scene
                    return true;
                }
            }
        }

        return false;
    }


    private bool CheckGameObjectForPrefab(GameObject obj, GameObject prefab)
    {
        if (PrefabUtility.GetCorrespondingObjectFromSource(obj) == prefab)
        {
            return true;
        }

        foreach (Transform child in obj.transform)
        {
            if (CheckGameObjectForPrefab(child.gameObject, prefab))
                return true;
        }

        return false;
    }


    private bool IsPrefabInstanceInChildren(Transform parent, GameObject prefab)
    {
        foreach (Transform child in parent)
        {
            GameObject childGameObject = child.gameObject;

            // Check if the child GameObject is an instance of the prefab
            if (PrefabUtility.GetCorrespondingObjectFromSource(childGameObject) == prefab)
            {
                return true;
            }

            // Recursively check children
            if (IsPrefabInstanceInChildren(child, prefab))
            {
                return true;
            }
        }

        return false;
    }



    private bool IsAssetUsedInGameObject(GameObject obj, string assetPath)
    {
        // Check components of the GameObject
        foreach (Component component in obj.GetComponents<Component>())
        {
            if (IsAssetUsedInComponent(component, assetPath))
            {
                return true;
            }
        }

        // Recursively check child GameObjects
        foreach (Transform child in obj.transform)
        {
            if (IsAssetUsedInGameObject(child.gameObject, assetPath))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAssetUsedInComponent(Component component, string assetPath)
    {
        if (component == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty serializedProperty = serializedObject.GetIterator();

        while (serializedProperty.NextVisible(true))
        {
            if (serializedProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (serializedProperty.objectReferenceValue != null)
                {
                    string referencedAssetPath = AssetDatabase.GetAssetPath(serializedProperty.objectReferenceValue);
                    if (referencedAssetPath == assetPath)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsAssetUsedInPrefabs(string assetPath)
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (IsAssetUsedInGameObject(prefab, assetPath))
            {
                Debug.Log($"Asset {assetPath} is used in prefab: {prefabPath}");
                return true;
            }
        }

        return false;
    }
    private bool PrefabContainsAsset(GameObject prefab, string assetPath)
    {
        if (prefab == null)
            return false;

        // Check if the asset is used in any component or material of the prefab
        Component[] components = prefab.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty serializedProperty = serializedObject.GetIterator();

            while (serializedProperty.NextVisible(true))
            {
                if (serializedProperty.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (serializedProperty.objectReferenceValue != null)
                    {
                        string referencedAssetPath = AssetDatabase.GetAssetPath(serializedProperty.objectReferenceValue);
                        if (referencedAssetPath == assetPath)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private void DeleteSelectedAssets()
    {
        assetsToDelete.Clear();
        foreach (var asset in unusedAssets)
        {
            if (asset.Value)
            {
                assetsToDelete.Add(asset.Key);
            }
        }

        if (assetsToDelete.Count > 0)
        {
            // Ask for confirmation before deleting assets
            bool shouldDelete = EditorUtility.DisplayDialog("Delete Assets", $"Are you sure you want to delete {assetsToDelete.Count} selected assets?", "Yes", "No");

            if (shouldDelete)
            {
                foreach (var assetPath in assetsToDelete)
                {
                    // Check if the asset is a folder or a file
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        // Delete folders using AssetDatabase.DeleteAsset
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    else
                    {
                        // Delete files using FileUtil.DeleteFileOrDirectory
                        FileUtil.DeleteFileOrDirectory(assetPath);
                    }

                    unusedAssets.Remove(assetPath);
                }

                AssetDatabase.Refresh();

                // Play a sound effect here (You need to have an audio clip assigned)
                PlaySoundEffect();
            }
        }
    }


    private void PlaySoundEffect()
    {
#if UNITY_EDITOR
        // In the Unity Editor, use DestroyImmediate
        // Specify the path to the BangFX audio clip in the SFX folder
        string audioClipPath = "Assets/Editor/AssetDestroyer/SFX/BangSFX.wav"; // Update the file extension and path as needed

        // Load the audio clip
        AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);

        if (audioClip != null)
        {
            // Create a GameObject with an AudioSource component to play the audio clip
            GameObject audioObject = new GameObject("TempAudioObject");
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.playOnAwake = false;

            // Play the audio clip
            audioSource.Play();

            // Destroy the temporary GameObject after the audio clip has finished playing
            float clipDuration = audioClip.length;
            DestroyImmediate(audioObject, false); // Use DestroyImmediate in editor mode
        }
#endif
    }




    private void SelectDeselectAll()
    {
        for (int i = 0; i < unusedAssets.Count; i++)
        {
            var item = unusedAssets.ElementAt(i);
            unusedAssets[item.Key] = !selectAllToggle;
        }
        selectAllToggle = !selectAllToggle;
    }
}
