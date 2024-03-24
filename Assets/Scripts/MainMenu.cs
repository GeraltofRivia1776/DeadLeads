using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuMenu : MonoBehaviour
{
    [Header("Menu Items")]
    public GameObject[] menuItems;
    public Sprite backOfCardSprite;
    [Range(0f, 1f)]
    public float inactiveCardTransparency = 0.5f; // Adjustable transparency for inactive cards

    [Header("Audio Settings")]
    public AudioClip clickSound; // Sound for clicking on a card
    public AudioClip switchSound; // Sound for switching between cards
    public float soundVolume = 0.5f; // Adjust the volume level as needed
    public AudioSource audioSource; // Reference to the AudioSource component
    private bool isSoundPlaying = false; // Flag to track if a sound is already playing

    [Header("Scroll Settings")]
    public float scrollSpeed = 1.0f;
    public float horizontalScrollSpeed = 100.0f;

    [Header("UI Elements")]
    public RectTransform panelRectTransform; // Assign this in the inspector
    public float cardWidth; // Set this based on your card width, including spacing if any
    public GameObject mainMenuCanvas;
    public GameObject menuManager;
    public GameObject saveLoadPanel;
    public GameObject menuCardPanel;
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    public GameObject creditsPanel;
    public GameObject pauseManager;
    public GameObject pauseMenuPanel;

    [Header("Scaling Animation")]
    public float scaleUpDuration = 0.5f; // Duration of the scaling animation in seconds
    public Vector3 startingCardScale;
    public Vector3 activeScale = new Vector3(1.1f, 1.1f, 1.0f); // Scale up factor for active card
    private float currentScaleTime = 0f; // Current time during the scaling animation
    private Vector3 initialScale; // Initial scale of the active card

    private int selectedIndex = 0;
    private Sprite[] originalSprites; // Store original sprites

    private static MenuMenu instance;

    void Start()
    {
        GameObject audioSourceGameObject = GameObject.FindGameObjectWithTag("MenuAudioSource");
        if (audioSourceGameObject != null)
        {
            audioSource = audioSourceGameObject.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogError("Failed to find the GameObject with tag 'AudioSourceTag'.");
        }

        PlaySwitchSound();

        // Store original sprites
        originalSprites = new Sprite[menuItems.Length];
        for (int i = 0; i < menuItems.Length; i++)
        {
            Image image = menuItems[i].GetComponent<Image>();
            if (image != null)
            {
                originalSprites[i] = image.sprite;
            }

            // Add EventTrigger component to detect clicks
            EventTrigger eventTrigger = menuItems[i].AddComponent<EventTrigger>();

            // Create a new entry for PointerClick event
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;

            // Add listener to call OnMenuItemClicked method with index as parameter
            int index = i;
            entry.callback.AddListener((eventData) => { OnMenuItemClicked(index, selectedIndex == index); });

            // Add the entry to the event trigger's list of events
            eventTrigger.triggers.Add(entry);
        }

        // Set the first card as active by default
        selectedIndex = 0;
        UpdateSelectionVisuals();

        // Set the initial scale of all menu items to the specified startingCardScale
        foreach (GameObject menuItem in menuItems)
        {
            menuItem.transform.localScale = startingCardScale;
        }

        // Store the initial scale of the active card
        initialScale = startingCardScale;
    }

    void Update()
    {
        HandleHorizontalNavigation();
        ConfirmSelectionWithLeftClick(); // Call the method without any arguments     

        // Check if the scaling animation needs to be updated
        if (currentScaleTime < scaleUpDuration)
        {
            UpdateScalingAnimation();
        }
    }

    void PlayScalingAnimation()
    {
        // Reset the current scale time when a new card is selected
        currentScaleTime = 0f;

        // Update the initial scale for the new active card
        initialScale = startingCardScale;

        // This ensures that the animation starts from the initial scale every time
    }

    void UpdateScalingAnimation()
    {
        // Increment the current scale time
        currentScaleTime += Time.deltaTime;

        // Calculate the interpolation factor (0 to 1)
        float t = Mathf.Clamp01(currentScaleTime / scaleUpDuration);

        // Interpolate between the initial scale and the target scale using lerp
        Vector3 targetScale = Vector3.Lerp(initialScale, activeScale, t);
        menuItems[selectedIndex].transform.localScale = targetScale;

        // Ensure the active card is at the target scale when the animation completes
        if (currentScaleTime >= scaleUpDuration)
        {
            menuItems[selectedIndex].transform.localScale = activeScale;
        }
    }

    void CenterActiveCard()
    {
        // Calculate the offset needed to center the active card
        float centerOffsetX = -selectedIndex * cardWidth;

        // Optionally, adjust for the starting offset if your first card is not at the very edge of the panel
        centerOffsetX += panelRectTransform.rect.width / 2 - cardWidth / 2;

        // Apply the offset to the panel's position
        Vector3 newPosition = panelRectTransform.anchoredPosition;
        newPosition.x = centerOffsetX;
        panelRectTransform.anchoredPosition = newPosition;
    }

    public void OnMenuItemClicked(int index, bool isActive)
    {
        // If the clicked card is inactive, do nothing
        if (!isActive)
        {
            return;
        }

        // Play click sound for active card
        if (!isSoundPlaying && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, soundVolume);
            isSoundPlaying = true;
            Invoke("ResetSoundFlag", clickSound.length);
        }

        // Trigger scaling animation when card is clicked
        PlayScalingAnimation();

        // Handle different menu item clicks based on index
        switch (index)
        {
            case 0: // Assuming the first card is meant to load a scene
                mainMenuCanvas.SetActive(false);
                menuManager.SetActive(false);
                SceneManager.LoadScene("MainScene"); // Replace with your scene name
                break;
            case 1: // Save/Load card
                SaveLoadPanel();
                break;
            case 2: // Settings card
                OpenSettingsPanel();
                break;
            case 3: // Controls card
                OpenControlsPanel();
                break;
            case 4: // Credits card
                OpenCreditsPanel();
                break;
            case 5: // The last card, intended for exiting the game
                Application.Quit();
                #if UNITY_EDITOR
                Debug.Log("Exit game requested (Unity Editor)");
                #endif
                break;
                // Add more cases for additional menu items if needed
        }
    }

    private void SaveLoadPanel()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        menuCardPanel.SetActive(false);
        menuManager.SetActive(false);
        saveLoadPanel.SetActive(true);
    }

    public void OpenSettingsPanel()
    {
        saveLoadPanel.SetActive(false);
        menuCardPanel.SetActive(false);
        settingsPanel.SetActive(true);
        controlsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void OpenControlsPanel()
    {
        saveLoadPanel.SetActive(false);
        menuCardPanel.SetActive(false);
        controlsPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void OpenCreditsPanel()
    {
        saveLoadPanel.SetActive(false);
        menuCardPanel.SetActive(false);
        creditsPanel.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

    public void PauseMenuPanel()
    {
        menuCardPanel.SetActive(false);
        menuManager.SetActive(false);
        pauseMenuPanel.SetActive(true);
        pauseManager.SetActive(true);
    }

    void HandleHorizontalNavigation()
    {
        // Check if the main menu panel is active before processing scroll input
        if (menuCardPanel.activeSelf)
        {
            // Incorporate A and D keys for horizontal navigation
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.W))
            {
                MoveSelection(1); // Move right
                                  // Trigger scaling animation when card is switched
                PlayScalingAnimation();
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S))
            {
                MoveSelection(-1); // Move left
                                   // Trigger scaling animation when card is switched
                PlayScalingAnimation();
            }

            // Handle mouse scroll wheel for horizontal navigation
            float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheelInput != 0)
            {
                int direction = scrollWheelInput > 0 ? -1 : 1; // Determine direction based on scroll direction
                MoveSelection(direction);
                // Trigger scaling animation when card is switched
                PlayScalingAnimation();
            }
        }
    }


    void ConfirmSelectionWithLeftClick()
    {
        // Check for left mouse button click, "E" key, or Spacebar to confirm selection
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) // Only left mouse button, E key, or Spacebar can select cards
        {
            // Get the position of the mouse click
            Vector2 mousePosition = Input.mousePosition;

            // Check if the mouse click is over any UI element
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // Convert mouse position to a 2D local position inside the Canvas
                RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, mousePosition, null, out Vector2 localPoint);

                // Convert local position to a 3D world position
                Vector3 worldPoint = panelRectTransform.TransformPoint(localPoint);

                // Check if the world point is within any of the menu items
                for (int i = 0; i < menuItems.Length; i++)
                {
                    RectTransform menuItemRectTransform = menuItems[i].GetComponent<RectTransform>();

                    // Check if the world point is inside the bounds of this menu item
                    if (RectTransformUtility.RectangleContainsScreenPoint(menuItemRectTransform, worldPoint))
                    {
                        // Determine if the clicked item is already the active one
                        bool isActive = (i == selectedIndex);

                        // Call OnMenuItemClicked with the index of the clicked item and whether it is active
                        OnMenuItemClicked(i, isActive);
                        return; // Exit the loop once the click is handled
                    }
                }
            }
        }
    }


    void MoveSelection(float amount)
    {
        // Increment or decrement the selectedIndex by the provided amount
        selectedIndex += (int)amount;

        // Ensure the selection loops within the bounds of the menuItems array
        if (selectedIndex < 0)
        {
            selectedIndex = menuItems.Length - 1;
        }
        else if (selectedIndex >= menuItems.Length)
        {
            selectedIndex = 0;
        }

        // Update the visual representation of the selection
        UpdateSelectionVisuals();

        // Play switch sound if not already playing
        PlaySwitchSound();

        // Center the active card after updating visuals
        CenterActiveCard();

        // Update the initial scale for the new active card
        initialScale = menuItems[selectedIndex].transform.localScale;

        // Reset the scaling animation
        currentScaleTime = 0f;
    }

    void MoveSelectionHorizontal(float amount)
    {
        Debug.Log("Horizontal Input: " + amount);
        selectedIndex = Mathf.Clamp(selectedIndex + Mathf.RoundToInt(amount), 0, menuItems.Length - 1);
        Debug.Log("Selected Index: " + selectedIndex);
        UpdateSelectionVisuals();

        // Play switch sound if not already playing
        PlaySwitchSound();

        // Center the active card after updating visuals
        CenterActiveCard();
    }

    public void OnMenuItemClicked(int index)
    {
        // Handle menu item click event
        selectedIndex = index;
        UpdateSelectionVisuals();

        // Play click sound if not already playing
        PlayClickSound();

        // Center the active card after updating visuals
        CenterActiveCard();

        // Update the initial scale for the new active card
        initialScale = menuItems[selectedIndex].transform.localScale;

        // Reset the scaling animation
        currentScaleTime = 0f;
    }


    void ResetSoundFlag()
    {
        // Reset the flag indicating that a sound is playing
        isSoundPlaying = false;
    }

    void UpdateSelectionVisuals()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            GameObject menuItem = menuItems[i];
            Image image = menuItem.GetComponent<Image>();
            TextMeshProUGUI text = menuItem.GetComponentInChildren<TextMeshProUGUI>();

            if (i == selectedIndex)
            {
                // Set full transparency for the selected menu item
                SetTransparency(image, 1.0f);

                // Store original sprite when item becomes active (selected)
                if (image != null && originalSprites[i] != null)
                {
                    image.sprite = originalSprites[i];
                }

                // Scale up active card
                menuItem.transform.localScale = activeScale;

                // Enable text if available
                if (text != null)
                {
                    text.enabled = true;
                }
            }
            else
            {
                // Set semi-transparent for non-selected menu items
                SetTransparency(image, inactiveCardTransparency); // Set transparency for inactive cards

                // Set back of card sprite for inactive cards
                if (image != null && backOfCardSprite != null)
                {
                    image.sprite = backOfCardSprite;
                }

                // Revert to original scale for inactive cards
                menuItem.transform.localScale = Vector3.one;

                // Disable text for inactive cards
                if (text != null)
                {
                    text.enabled = false;
                }
            }
        }

        // Center the active card after updating visuals
        CenterActiveCard();
    }

    void SetTransparency(Graphic graphic, float transparency)
    {
        // Set transparency of the graphic's color
        Color color = graphic.color;
        color.a = transparency;
        graphic.color = color;
    }

    void PlaySwitchSound()
    {
        if (!isSoundPlaying && switchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchSound, soundVolume);
            isSoundPlaying = true;
            Invoke("ResetSoundFlag", switchSound.length);
        }
        else
        {
            Debug.Log("AudioSource component is null. SwitchSound");
        }
    }

    void PlayClickSound()
    {
        if (!isSoundPlaying && clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, soundVolume);
            isSoundPlaying = true;
            Invoke("ResetSoundFlag", clickSound.length);
        }
        else
        {
            Debug.Log("AudioSource component is null.ClickSound");
        }
    }
}
