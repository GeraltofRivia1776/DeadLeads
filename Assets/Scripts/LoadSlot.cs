using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Sprites")]
    public Sprite backOfCardSprite;
    public Sprite frontOfCardSprite;

    [Header("UI Elements")]
    public Image screenShot;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI missionText;
    public Button loadButton;
    public GameObject loadConfirmationPanel1; // Separate reference for each load confirmation panel
    public GameObject loadConfirmationPanel2;
    public GameObject loadConfirmationPanel3;

    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioSource audioSource;

    [Header("Appearance Settings")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float normalTransparency = 0.5f;
    [SerializeField] private float hoverTransparency = 1f;

    [Header("Save Slot References")]
    public SaveSlot saveSlot1;
    public SaveSlot saveSlot2;
    public SaveSlot saveSlot3;

    private Image cardImageComponent;
    private RectTransform rectTransform;
    private bool isMouseOver = false;
    private bool isPopulated = false;
    private int saveSlotIndex; // Index of the save slot

    private SaveSlot associatedSaveSlot; // Reference to the associated SaveSlot
    private GameObject currentLoadConfirmationPanel; // Reference to the current load confirmation panel
    private bool isConfirmationPanelActive = false; // Track if any confirmation panel is currently active
    private static GameObject currentActiveConfirmationPanel; // Reference to the currently active confirmation panel

    private Vector3 savedPlayerPosition; // Variable to store saved player position

    private void Start()
    {
        cardImageComponent = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        SetCardToBack();
        screenShot.gameObject.SetActive(false);
        dateText.gameObject.SetActive(false);
        missionText.gameObject.SetActive(false);

        // Set save slot index based on the slot's position in the hierarchy
        saveSlotIndex = transform.GetSiblingIndex() + 1; // Assuming sibling index starts from 0

        // Find the associated SaveSlot by index
        switch (saveSlotIndex)
        {
            case 1:
                associatedSaveSlot = saveSlot1;
                currentLoadConfirmationPanel = GetLoadConfirmationPanel(1);
                break;
            case 2:
                associatedSaveSlot = saveSlot2;
                currentLoadConfirmationPanel = GetLoadConfirmationPanel(2);
                break;
            case 3:
                associatedSaveSlot = saveSlot3;
                currentLoadConfirmationPanel = GetLoadConfirmationPanel(3);
                break;
            default:
                Debug.LogError($"Invalid save slot index: {saveSlotIndex}");
                break;
        }

        // Populate slot if associated save slot is populated
        if (associatedSaveSlot != null && associatedSaveSlot.IsPopulated())
        {
            PopulateSlot(associatedSaveSlot.GetSaveData());
        }

        loadButton.onClick.AddListener(OnLoadButtonClicked);

        // Load saved player position
        LoadPlayerPosition();
    }

    private void PopulateSlot(GameData saveData)
    {
        // Update UI elements with save data
        dateText.text = saveData.date;
        missionText.text = saveData.mission;
        dateText.gameObject.SetActive(true);
        missionText.gameObject.SetActive(true);

        // Load and display the screenshot
        if (saveData.screenshotData != null)
        {
            Texture2D screenshotTexture = new Texture2D(1, 1);
            screenshotTexture.LoadImage(saveData.screenshotData);
            screenShot.sprite = Sprite.Create(screenshotTexture, new Rect(0, 0, screenshotTexture.width, screenshotTexture.height), Vector2.zero);
            screenShot.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Screenshot data is null.");
            screenShot.gameObject.SetActive(false);
        }

        // Determine if the slot is populated
        isPopulated = saveData != null;

        // Show or hide the load button based on whether the slot is populated
        loadButton.gameObject.SetActive(isPopulated);

        // Set card appearance
        UpdateCardImage();
    }

    private void LoadPlayerPosition()
    {
        // Example: Retrieve saved player position from PlayerPrefs or any other storage
        savedPlayerPosition = new Vector3(PlayerPrefs.GetFloat("PlayerPosX", 0f),
                                          PlayerPrefs.GetFloat("PlayerPosY", 0f),
                                          PlayerPrefs.GetFloat("PlayerPosZ", 0f));

        // Set player position only if it's not the initial position (0, 0, 0)
        if (savedPlayerPosition != Vector3.zero)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = savedPlayerPosition;
            }
            else
            {
                Debug.LogError("Player GameObject not found.");
            }
        }
    }

    // Inside LoadSlot class

    private void OnLoadButtonClicked()
    {
        // Check if associatedSaveSlot is not null
        if (associatedSaveSlot != null)
        {
            // Call LoadGameData from the associated SaveSlot
            associatedSaveSlot.LoadGameData();
        }
        else
        {
            Debug.LogError("Associated SaveSlot is not found.");
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        UpdateAppearance();

        if (audioSource != null && hoverSound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
        UpdateAppearance();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPopulated)
        {
            // Play click sound
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }

            // Show load confirmation panel
            ShowLoadConfirmationPanel();
        }
    }

    private void UpdateAppearance()
    {
        if (isMouseOver)
        {
            rectTransform.localScale = new Vector3(hoverScale, hoverScale, 1f);
            cardImageComponent.color = new Color(1f, 1f, 1f, hoverTransparency);
        }
        else
        {
            rectTransform.localScale = new Vector3(normalScale, normalScale, 1f);
            cardImageComponent.color = new Color(1f, 1f, 1f, normalTransparency);
        }
    }

    private void SetCardToFront()
    {
        cardImageComponent.sprite = frontOfCardSprite;
        cardImageComponent.color = Color.white;
    }

    private void SetCardToBack()
    {
        cardImageComponent.sprite = backOfCardSprite;
        cardImageComponent.color = new Color(1f, 1f, 1f, normalTransparency);
    }

    private void UpdateCardImage()
    {
        if (!isPopulated)
        {
            SetCardToBack();
        }
        else
        {
            SetCardToFront();
        }
    }

    private GameObject GetLoadConfirmationPanel(int index)
    {
        switch (index)
        {
            case 1:
                return loadConfirmationPanel1;
            case 2:
                return loadConfirmationPanel2;
            case 3:
                return loadConfirmationPanel3;
            default:
                return null;
        }
    }

    private void ShowLoadConfirmationPanel()
    {
        // Close the current active confirmation panel if it exists
        if (currentActiveConfirmationPanel != null)
        {
            currentActiveConfirmationPanel.SetActive(false);
        }

        // Open the current load confirmation panel
        currentLoadConfirmationPanel.SetActive(true);
        currentActiveConfirmationPanel = currentLoadConfirmationPanel;

        // Play hover sound
        if (audioSource != null && hoverSound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    private void CloseCurrentConfirmationPanel()
    {
        // Close the currently active confirmation panel
        if (currentLoadConfirmationPanel != null)
        {
            currentLoadConfirmationPanel.SetActive(false);
            isConfirmationPanelActive = false;
        }
    }

    public void LoadGameData()
    {
        // Check if the associated save slot is not null
        if (associatedSaveSlot != null)
        {
            // Retrieve the game data from the associated save slot
            GameData saveData = associatedSaveSlot.GetSaveData();

            // Check if game data is loaded successfully
            if (saveData != null)
            {
                // Update player's position if loaded data contains position information
                if (saveData.playerPositionX != 0f || saveData.playerPositionY != 0f || saveData.playerPositionZ != 0f)
                {
                    // Get the player GameObject
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        // Update player's position
                        player.transform.position = new Vector3(saveData.playerPositionX, saveData.playerPositionY, saveData.playerPositionZ);
                    }
                    else
                    {
                        Debug.LogError("Player GameObject is not found.");
                    }
                }
                else
                {
                    Debug.LogWarning("Player position data is missing in the loaded game data.");
                }

                // Here you can implement additional logic to load other game data as needed

                // Log success message
                Debug.Log("Game data loaded successfully.");
            }
            else
            {
                Debug.LogError("Failed to load game data from the associated save slot.");
            }
        }
        else
        {
            Debug.LogError("Associated SaveSlot is not found.");
        }

        // After loading, close the current confirmation panel
        CloseCurrentConfirmationPanel();
    }


}
