using UnityEngine;
using UnityEngine.UI;

public class ScenarioManager : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject _scenarioPanel;
    [SerializeField] private GameObject _autorizationPanel;

    [Header("Кнопки")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _startButton;

    [Header("Ссылка на сеть")]
    public VRClient _network;

    [Header("VR Movement")]
    [SerializeField] private VRMovementController movementController;

    [Header("Объекты обучения")]
    [SerializeField] private GameObject trainingObjects;

    private void Start()
    {
        // Блокируем движение при старте игры
        if (movementController != null)
            movementController.DisableMovement();

        // Скрываем объекты обучения до начала сценария
        if (trainingObjects != null)
            trainingObjects.SetActive(false);

        // Подписываемся на кнопки
        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        if (_startButton != null)
            _startButton.onClick.AddListener(OnStartButtonClicked);

        Debug.Log("ScenarioManager initialized. Waiting for scenario selection...");
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
    }

    private void OnCloseButtonClicked()
    {
        if (_network != null)
        {
            _network.Disconnect();
        }
        else
        {
            Debug.LogWarning("VRClient не назначен в инспекторе ScenarioManager!");
        }

        if (_scenarioPanel != null)
            _scenarioPanel.SetActive(false);
        if (_autorizationPanel != null)
            _autorizationPanel .SetActive(true);
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("Starting scenario...");

        // 1. Скрываем панель авторизации
        if (_scenarioPanel != null)
            _scenarioPanel.SetActive(false);

        // 2. Показываем ангар и объекты обучения
        if (trainingObjects != null)
            trainingObjects.SetActive(true);
        else
            Debug.LogWarning("Training Objects не назначены!");

        // 3. Разблокируем управление
        if (movementController != null)
        {
            movementController.EnableMovement();
            Debug.Log("Movement enabled");
        }

        Debug.Log("Scenario started successfully!");
    }
}