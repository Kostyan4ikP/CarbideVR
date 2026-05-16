using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Net;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] GameObject AutorisationPanel;
    [SerializeField] GameObject ScenarioPanel;

    [Header("Поля ввода")]
    [SerializeField] private TMP_InputField _login;
    [SerializeField] private TMP_InputField _password;
    [SerializeField] private TMP_InputField _ip;
    [SerializeField] private TMP_InputField _port;
    [SerializeField] private Button _startButton;

    [Header("Ссылка на сеть")]
    public VRClient _network;

    private void Start()
    {
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(OnConnectButtonClick);
        }
    }

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
            _startButton.interactable = false;

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
                        AutorisationPanel.SetActive(false);
                        ScenarioPanel.SetActive(true);

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
                _startButton.interactable = true;
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

    private void ShowError(string message)
    {
        ErrorPopupManager.ShowError(message);
        Debug.LogWarning($"Ошибка валидации: {message}");
    }
}
