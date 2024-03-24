using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BackButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject saveMenuPanel;
    public GameObject cardPanel;
    public GameObject menuManager;
    public AudioClip hoverSound;
    public AudioSource audioSource;

    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private GameObject card;
    [SerializeField] private float normalTransparency = 0.4f;
    [SerializeField] private float hoverTransparency = 1f;

    private bool isMouseOver = false;
    private bool isPlayingSound = false; // Flag to check if a sound is currently playing
    private Image cardImage;

    private void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is not assigned to the BackButton script.");
        }

        if (card != null)
        {
            cardImage = card.GetComponent<Image>();
            if (cardImage == null)
            {
                Debug.LogError("Card does not have Image component.");
            }
        }
        else
        {
            Debug.LogError("Card is not assigned to the BackButton script.");
        }

        // Activate the cardPanel if it's not already active
        if (cardPanel != null && !cardPanel.activeSelf)
        {
            cardPanel.SetActive(true);
        }
    }

    public void GoBack()
    {
        saveMenuPanel.SetActive(false);
        menuManager.SetActive(true);
        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        UpdateAppearance();
        if (!isPlayingSound && hoverSound != null) // Play sound only if no sound is currently playing and hoverSound is not null
        {
            PlayHoverSound();
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
            transform.localScale = new Vector3(hoverScale, hoverScale, 1f);
            SetCardTransparency(hoverTransparency); // Set full transparency
        }
        else
        {
            transform.localScale = new Vector3(normalScale, normalScale, 1f);
            SetCardTransparency(normalTransparency); // Set partial transparency
        }
    }

    private void SetCardTransparency(float alpha)
    {
        if (cardImage != null)
        {
            Color color = cardImage.color;
            color.a = alpha;
            cardImage.color = color;
        }
        else
        {
            Debug.LogError("Card image is not assigned to the BackButton script.");
        }
    }

    private void PlayHoverSound()
    {
        isPlayingSound = true; // Set the flag to indicate sound is playing
        audioSource.PlayOneShot(hoverSound);
        Invoke(nameof(ResetIsPlayingSound), hoverSound.length); // Reset the flag after the sound duration
    }

    private void ResetIsPlayingSound()
    {
        isPlayingSound = false;
    }
}
