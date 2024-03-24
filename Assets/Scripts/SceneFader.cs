using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage; // Reference to the UI image used for fading
    public float fadeDuration = 1.0f; // Duration of the fade effect

    private static SceneFader instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Make the SceneFader object persistent across scenes
            SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to the sceneLoaded event
        }
    }

    void Start()
    {
        // Initialize the screen to black
        fadeImage.color = Color.black;
        // Start the FadeIn effect immediately
        StartCoroutine(FadeIn());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeIn()); // Fade in every time a new scene is loaded
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the sceneLoaded event
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoadScene(sceneName));
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        // Start fading to black and wait for it to finish
        yield return StartCoroutine(FadeOut());

        // Load the new scene asynchronously in the background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // This operation will immediately start but return before the scene is fully loaded
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            // Optionally, you can show loading progress here
            // Example: loadingProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Unity loads scenes up to 90% then jumps to 100% when done
            yield return null;
        }

        // The screen is still black here since we haven't changed the fadeImage color after FadeOut
        // Now that the new scene is loaded, we can start fading back in
        StartCoroutine(FadeIn());
    }


    private IEnumerator FadeOut()
    {
        float elapsedTime = 0;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // Ensure the screen stays black at the end of the fade out
        fadeImage.color = new Color(0, 0, 0, 1);
    }


    private IEnumerator FadeIn()
    {
        fadeImage.color = Color.clear; // Reset to clear at start
        float elapsedTime = 0;

        // Start with the screen fully black
        fadeImage.color = Color.black;

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = Color.clear; // Ensure the screen is clear after fading in
    }
}
