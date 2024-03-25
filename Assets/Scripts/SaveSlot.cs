using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System;
using System.Collections;

[System.Serializable]
public class GameData
{
    public string date;
    public string mission;
    public byte[] screenshotData; // Serialized screenshot data as byte array

    // Additional fields for character locations
    public float playerPositionX;
    public float playerPositionY;
    public float playerPositionZ;

    public GameData(string date, string mission, Texture2D screenshotTexture, Vector3 playerPosition)
    {
        this.date = date;
        this.mission = mission;

        // Convert the screenshot texture to a byte array
        if (screenshotTexture != null)
        {
            this.screenshotData = screenshotTexture.EncodeToPNG();
        }
        else
        {
            Debug.LogError("Screenshot texture is null.");
        }

        // Save player position
        this.playerPositionX = playerPosition.x;
        this.playerPositionY = playerPosition.y;
        this.playerPositionZ = playerPosition.z;
    }
}


public class SaveSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite backOfCardSprite;
    public Sprite frontOfCardSprite;
    private Texture2D screenshotTexture; // Store the screenshot texture

    [Header("UI Elements")]
    public Image screenShot;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI missionText;
    public Button deleteButton;
    public Button overwriteConfirmationButton;
    public Button overwriteCancelButton;
    public GameObject pauseMenuPanel;

    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioClip saveSound;
    public AudioSource audioSource;

    [Header("Appearance Settings")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float normalTransparency = 0.5f;
    [SerializeField] private float hoverTransparency = 1f;

    [Header("Popup")]
    public GameObject deleteConfirmationPanel;
    public GameObject overwriteConfirmationPanel;

    private GameObject player;

    private Image cardImageComponent;
    private RectTransform rectTransform;
    private bool isMouseOver = false;
    private bool isEmpty = true;
    private PauseMenu pauseMenu; // Reference to PauseMenu script

    private Texture2D capturedScreenshot; // Store the captured screenshot
    private SaveSlot[] saveSlots; // Array of SaveSlot references

    private int saveSlotIndex; // Index of the save slot

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 playerPosition = player.transform.position;
            // Rest of the code that uses playerPosition
        }
        else
        {
            Debug.LogError("Player object is not assigned.");
        }


        // Find the PauseMenu script instance in the scene
        pauseMenu = FindObjectOfType<PauseMenu>();
        if (pauseMenu == null)
        {
            Debug.LogError("PauseMenu script not found in the scene.");
        }
        cardImageComponent = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        SetCardToBack();
        screenShot.gameObject.SetActive(false);
        dateText.gameObject.SetActive(false);
        missionText.gameObject.SetActive(false);
        deleteButton.gameObject.SetActive(false);
        deleteConfirmationPanel.SetActive(false);
        overwriteConfirmationPanel.SetActive(false);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        overwriteConfirmationButton.onClick.AddListener(OnOverwriteConfirmation);
        overwriteCancelButton.onClick.AddListener(OnOverwriteCancel);

        // Set save slot index based on the slot's position in the hierarchy
        saveSlotIndex = transform.GetSiblingIndex() + 1; // Assuming sibling index starts from 0
        GameData loadedData = LoadGameData();
        if (loadedData != null)
        {
            // Assuming SetSaveData and UpdateScreenshots are methods used to update the save slot UI
            SetSaveData(loadedData.date, loadedData.mission);
            Texture2D screenshotTexture = new Texture2D(2, 2);
            screenshotTexture.LoadImage(loadedData.screenshotData); // Assumes screenshotData is a valid byte array
            UpdateScreenshots(screenshotTexture);
        }
    }

    void LateUpdate()
    {
        // Check if PauseMenu script is available and the captured screenshot is not null
        if (pauseMenu != null && pauseMenu.GetCapturedScreenshot() != null)
        {
            // Update the save slots with the captured screenshot
            pauseMenu.UpdateSaveSlotsWithScreenshot();
        }
    }
    void Update()
    {
        // Check for the Escape key press to capture the screenshot
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(DelayedScreenshotCapture());
        }
    }

    private IEnumerator DelayedScreenshotCapture()
    {
        // Wait for the end of the frame
        yield return new WaitForEndOfFrame();

        // Capture the screenshot
        CaptureScreenshot();
    }


    private void CaptureScreenshot()
{
    // Capture the screenshot
    capturedScreenshot = ScreenCapture.CaptureScreenshotAsTexture();

    // Check if the screenshot capture was successful
    if (capturedScreenshot == null)
    {
        Debug.LogError("Failed to capture screenshot.");
    }
}


    public void UpdateScreenshots(Texture2D screenshotTexture)
    {
        if (screenshotTexture != null)
        {
            this.screenshotTexture = screenshotTexture; // Store the screenshot texture

            // Set the screenshot image sprite
            Sprite screenshotSprite = Sprite.Create(screenshotTexture, new Rect(0, 0, screenshotTexture.width, screenshotTexture.height), Vector2.one * 0.5f);
            screenShot.sprite = screenshotSprite;

            // Activate the screenshot image
            screenShot.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Screenshot texture is null.");
        }
    }

    public void SetSaveData(string date, string mission)
    {
        // Set date and mission text
        dateText.text = date;
        missionText.text = mission;

        // Activate UI elements
        dateText.gameObject.SetActive(true);
        missionText.gameObject.SetActive(true);
        deleteButton.gameObject.SetActive(true);

        // Set card appearance
        isEmpty = false;
        SetCardToFront();
        UpdateCardImage();
    }

    public void SaveGameData(GameData data)
    {
        // Get the player's current position from the player GameObject
        Vector3 playerPosition = player.transform.position;

        // Serialize the GameData object to JSON
        string jsonData = JsonUtility.ToJson(data);
        string savePath = Path.Combine(Application.persistentDataPath, $"saveData_{saveSlotIndex}.json");

        try
        {
            // Write the JSON data to a file
            File.WriteAllText(savePath, jsonData);
            Debug.Log($"Saved game data to: {savePath}");
            UpdateDateText(data.date);

            // Save the player's position in the GameData object
            data.playerPositionX = playerPosition.x;
            data.playerPositionY = playerPosition.y;
            data.playerPositionZ = playerPosition.z;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save game data: {ex}");
        }
    }


    public GameData LoadGameData()
    {
        string savePath = Path.Combine(Application.persistentDataPath, $"saveData_{saveSlotIndex}.json");
        if (File.Exists(savePath))
        {
            try
            {
                string jsonData = File.ReadAllText(savePath);
                GameData loadedData = JsonUtility.FromJson<GameData>(jsonData);
                return loadedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game data: {ex}");
            }
        }
        else
        {
            Debug.Log($"No save file found at: {savePath}");
        }
        return null;
    }

    private void UpdateDateText(string formattedDateTime)
    {
        dateText.text = formattedDateTime;
    }

    public void ClearSaveData()
    {
        // Show delete confirmation panel
        deleteConfirmationPanel.SetActive(true);
    }

    public void ConfirmDelete()
    {
        // Hide delete confirmation panel
        deleteConfirmationPanel.SetActive(false);

        // Clear save data
        screenShot.gameObject.SetActive(false);
        dateText.text = "";
        missionText.text = "";
        deleteButton.gameObject.SetActive(false); // Deactivate delete button
        isEmpty = true;
        UpdateCardImage(); // Update card image

        // Delete the save file
        string savePath = Path.Combine(Application.persistentDataPath, $"saveData_{saveSlotIndex}.json");
        if (File.Exists(savePath))
        {
            try
            {
                File.Delete(savePath);
                Debug.Log($"Deleted save file: {savePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete save file: {ex}");
            }
        }
        else
        {
            Debug.Log("No save file found to delete.");
        }
    }


    // Method to handle delete button click
    private void OnDeleteButtonClicked()
    {
        // Clear save data for this save slot
        ClearSaveData();
    }

    public void CancelDelete()
    {
        // Hide delete confirmation panel
        deleteConfirmationPanel.SetActive(false);
    }

    public void OnCardClicked()
    {
        if (isEmpty)
        {
            // Activate the screenshot UI
            screenShot.gameObject.SetActive(true);

            // Generate the current date and time
            DateTime now = DateTime.Now;
            string currentDate = FormatDateTime(now); // Format the current date and time
            string formattedDateTime = FormatDateTimeForDisplay(now); // Format the current date and time for display
            string missionDescription = "Test Mission"; // Replace with your mission description

            // Update UI elements with the generated data
            SetSaveData(currentDate, missionDescription);

            // Capture the screenshot using PauseMenu reference
            if (pauseMenu != null)
            {
                capturedScreenshot = pauseMenu.GetCapturedScreenshot(); // Get the captured screenshot
                UpdateScreenshots(capturedScreenshot); // Update the screenshot UI
            }
            else
            {
                Debug.LogError("PauseMenu reference is not assigned.");
            }

            // Play the save sound if available
            if (audioSource != null && saveSound != null)
            {
                audioSource.PlayOneShot(saveSound);
            }

            // Log the action
            Debug.Log("Clicked on empty save slot with current date and time: " + formattedDateTime);

            // Save the game data
            GameData updatedGameData = new GameData(formattedDateTime, missionDescription, capturedScreenshot, player.transform.position);
            SaveGameData(updatedGameData); // Save the game data
        }
        else
        {
            // If the save slot is not empty, show the overwrite confirmation panel
            ShowOverwriteConfirmationPanel();
        }
    }


    // Method to format the date and time
    private string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("yyyy/MM/dd h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
    }

    // Method to format the date and time for display
    private string FormatDateTimeForDisplay(DateTime dateTime)
    {
        return dateTime.ToString("yyyy/MM/dd h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
    }


    private void ShowOverwriteConfirmationPanel()
    {
        overwriteConfirmationPanel.SetActive(true);

        if (audioSource != null && hoverSound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(hoverSound);
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
        if (isEmpty)
        {
            SetCardToBack();
        }
        else
        {
            SetCardToFront();
        }
    }

    public void OnOverwriteConfirmation()
    {
        if (screenshotTexture != null)
        {
            DateTime currentTime = DateTime.Now;
            string formattedDateTime = currentTime.ToString("yyyy/MM/dd h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            string updatedMission = "Updated Mission Description";

            // Pass the actual screenshot texture instead of null
            GameData updatedGameData = new GameData(formattedDateTime, updatedMission, capturedScreenshot, player.transform.position);

            SaveGameData(updatedGameData);

            dateText.text = formattedDateTime;
            missionText.text = updatedMission;

            // Call UpdateScreenshots with the captured screenshot texture
            if (pauseMenu != null)
            {
                capturedScreenshot = pauseMenu.GetCapturedScreenshot(); // Get the captured screenshot
                UpdateScreenshots(capturedScreenshot);
            }
            else
            {
                Debug.LogError("PauseMenu reference is not assigned.");
            }
            overwriteConfirmationPanel.SetActive(false);

            Debug.Log($"Overwrite confirmation: Game data updated at {formattedDateTime}");
        }
        else
        {
            Debug.LogError("Screenshot texture is null.");
        }
    }

    private void OnOverwriteCancel()
    {
        overwriteConfirmationPanel.SetActive(false);
    }

    public bool IsPopulated()
    {
        // Check if the save slot is populated
        return !isEmpty; // Return true if populated, false if empty
    }

    public GameData GetSaveData()
    {
        // Retrieve the save data from the save slot
        if (IsPopulated())
        {
            // If the save slot is populated, load the saved game data from file
            return LoadGameData();
        }
        else
        {
            // If the save slot is empty, return null
            return null;
        }
    }

}