using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class MenuManager : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject _autorizationPanel;
    [SerializeField] private GameObject _scenarioPanel;

    [Header("Поля ввода (Авторизация)")]
    [SerializeField] private TMP_InputField _login;
    [SerializeField] private TMP_InputField _password;
    [SerializeField] private TMP_InputField _ip;
    [SerializeField] private TMP_InputField _port;
    [SerializeField] private Button _connectButton;

    [Header("Кнопки (Сценарии)")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _startButton;

    [Header("Ссылка на сеть")]
    public VRClient _network;

    [Header("VR Movement")]
    [SerializeField] private VRMovementController movementController;

    [Header("Объекты обучения")]
    [SerializeField] private GameObject trainingObjects;

    public int _userId;
    private string _selectedScenarioId;
    public bool IsScenarioSelected => !string.IsNullOrEmpty(_selectedScenarioId);

    private void Start()
    {
        if (movementController != null)
            movementController.DisableMovement();

        if (trainingObjects != null)
            trainingObjects.SetActive(false);

        if (_startButton != null)
            _startButton.interactable = false;

        if (_connectButton != null)
            _connectButton.onClick.AddListener(OnConnectButtonClick);
        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        if (_startButton != null)
            _startButton.onClick.AddListener(OnStartButtonClicked);

        Debug.Log("MenuManager initialized.");
    }

    private void OnDestroy()
    {
        if (_connectButton != null)
            _connectButton.onClick.RemoveListener(OnConnectButtonClick);
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
    }

    public void EnableStartButton()
    {
        if (_startButton != null)
        {
            _startButton.interactable = true;
            Debug.Log("Кнопка Start разблокирована");
        }
    }

    #region Авторизация

    private async void OnConnectButtonClick()
    {
        bool isCorrectData = true;
        string errorMessage = "";

        string login = _login.text.Trim();
        string password = _password.text.Trim();
        string ip = _ip.text.Trim();
        string portStr = _port.text.Trim();

        if (string.IsNullOrEmpty(login))
        {
            errorMessage += "Введите логин!\n";
            isCorrectData = false;
        }

        if (string.IsNullOrEmpty(password))
        {
            errorMessage += "Введите пароль!\n";
            isCorrectData = false;
        }

        if (!ValidateIP(ip))
        {
            errorMessage += "Некорректный формат IP-адреса!\n";
            isCorrectData = false;
        }

        if (!ValidatePort(portStr, out int port))
        {
            errorMessage += "Некорректный формат порта!\n";
            isCorrectData = false;
        }

        if (isCorrectData)
        {
            _connectButton.interactable = false;

            try
            {
                if (!await _network.ConnectToServerAsync(ip, port)) return;

                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"method", "authorization"},
                    {"login", login},
                    {"password", password}
                };

                var response = await _network.SendRequestAsync(data, timeout: 10f);

                if (response.TryGetValue("success", out var successObj) && successObj is bool success)
                {
                    if (success)
                    {
                        Debug.Log("Авторизация успешна!");
                        _userId = JsonConvert.DeserializeObject<int>(response["user_id"].ToString());
                        SwitchToScenarioPanel();

                        _login.text = "";
                        _password.text = "";
                        _ip.text = "";
                        _port.text = "";
                    }
                    else
                    {
                        string error = response.TryGetValue("error", out var e) ? e.ToString() : "Ошибка";
                        ShowError("Ошибка:\n" + error);
                    }
                }
            }
            catch (System.Exception e)
            {
                ShowError($"Ошибка авторизации: {e.Message}");
                Debug.LogError($"Ошибка авторизации: {e.Message}");
            }
            finally
            {
                _connectButton.interactable = true;
            }
        }
        else
        {
            ShowError(errorMessage);
            return;
        }
    }

    private bool ValidateIP(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        return IPAddress.TryParse(ip, out _);
    }

    private bool ValidatePort(string portStr, out int port)
    {
        port = 0;
        if (string.IsNullOrEmpty(portStr)) return false;

        if (int.TryParse(portStr, out port))
        {
            return port >= 1 && port <= 65535;
        }
        return false;
    }

    #endregion

    #region Управление сценариями

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
            Debug.LogWarning("VRClient не назначен в инспекторе MenuManager!");
        }

        SwitchToAutorizationPanel();
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

        Debug.Log("Scenario started successfully!");
    }

    #endregion

    #region Переключение панелей

    private void SwitchToScenarioPanel()
    {
        if (_autorizationPanel != null)
            _autorizationPanel.SetActive(false);
        if (_scenarioPanel != null)
            _scenarioPanel.SetActive(true);
    }

    private void SwitchToAutorizationPanel()
    {
        if (_scenarioPanel != null)
            _scenarioPanel.SetActive(false);
        if (_autorizationPanel != null)
            _autorizationPanel.SetActive(true);
    }

    #endregion

    #region Утилиты

    private void ShowError(string message)
    {
        ErrorPopupManager.ShowError(message);
        Debug.LogWarning($"Ошибка: {message}");
    }

    #endregion
}