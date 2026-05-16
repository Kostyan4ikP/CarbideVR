using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScenarioManager : MonoBehaviour
{
    [Header("Сцена обучаемого")]
    [SerializeField] private string _operatorScene = "OperatorScene";

    [Header("Панели")]
    [SerializeField] private GameObject _autorisationPanel;
    [SerializeField] private GameObject _scenarioPanel;

    [Header("Кнопки")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _startButton;

    [Header("Ссылка на сеть")]
    public VRClient _network;

    private void Start()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        if (_startButton != null)
            _startButton.onClick.AddListener(OnStartButtonClicked);
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
        if (_autorisationPanel != null)
            _autorisationPanel.SetActive(true);
    }

    private void OnStartButtonClicked()
    {
        if (string.IsNullOrEmpty(_operatorScene))
        {
            Debug.LogError("Имя сцены не указано в инспекторе!");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(_operatorScene))
        {
            Debug.LogError($"Сцена '{_operatorScene}' не найдена. Добавьте её в Build Settings (File → Build Settings → Add Open Scenes)");
            return;
        }

        SceneManager.LoadScene(_operatorScene);
    }
}