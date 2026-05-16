using UnityEngine;
using TMPro;
using UnityEngine.UI; // »спользуйте UnityEngine.UI, если обычный Text

public class ErrorPopupManager : MonoBehaviour
{
    [Header("—сылки на объекты")]
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _closePopupButton;

    public static ErrorPopupManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (_popupPanel != null)
            _popupPanel.SetActive(false);
    }

    private void Start()
    {
        if (_closePopupButton != null)
            _closePopupButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public static void ShowError(string message)
    {
        if (Instance != null)
        {
            Instance._ShowErrorInternal(message);
        }
        else
        {
            Debug.LogError("ErrorPopupManager не найден на сцене!");
        }
    }

    private void _ShowErrorInternal(string message)
    {
        if (_messageText != null)
            _messageText.text = message;

        if (_popupPanel != null)
            _popupPanel.SetActive(true);
    }

    public void OnCloseButtonClicked()
    {
        if (_popupPanel != null)
            _popupPanel.SetActive(false);
    }
}