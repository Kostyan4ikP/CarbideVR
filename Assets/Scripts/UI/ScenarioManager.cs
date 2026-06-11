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

    private string _selectedScenarioId;

    public bool IsScenarioSelected => !string.IsNullOrEmpty(_selectedScenarioId);

    private void Start()
    {
        if (movementController != null)
            movementController.DisableMovement();

        if (trainingObjects != null)
            trainingObjects.SetActive(false);

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

    public void OnScenarioSelected(string scenarioId)
    {
        _selectedScenarioId = scenarioId;
        Debug.Log($"Scenario selected: {scenarioId}");
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
            _autorizationPanel.SetActive(true);
    }

    private void OnStartButtonClicked()
    {
        Debug.Log($"Starting scenario: {_selectedScenarioId}");

        if (_scenarioPanel != null)
            _scenarioPanel.SetActive(false);

        if (trainingObjects != null)
            trainingObjects.SetActive(true);
        else
            Debug.LogWarning("Training Objects не назначены!");

        if (movementController != null)
        {
            movementController.EnableMovement();
            Debug.Log("Movement enabled");
        }

        // 🔹 НОВОЕ: здесь можно использовать _selectedScenarioId
        // Например, передать его в сеть, загрузить нужную сцену и т.д.
        // _network.StartScenario(_selectedScenarioId);

        Debug.Log("Scenario started successfully!");
    }
}