using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[System.Serializable]
public class ScenarioData
{
    public int id;
    public object title;
}

public class ScenarioListManager : MonoBehaviour
{
    [Header("Настройки UI")]
    public Transform contentPanel;
    public GameObject scenarioPrefab;

    [Header("Ссылка на MenuManager")]
    [SerializeField] private MenuManager _menuManager;

    [Header("Ссылка на ScenarioDisplay")]
    [SerializeField] private ScenarioDisplay _scenarioDisplay;

    [Header("Данные из БД")]
    Dictionary<int, Dictionary<string, object>> scenariosFromDB;

    [Header("Сервер")]
    public VRClient network;

    private ScenarioItemUI _currentlySelected;

    public Dictionary<object, object> currentScenarioData;
    public Dictionary<object, Dictionary<object, object>> currentScenarioTechProcess;

    void OnEnable()
    {
        ReadScenariosFromServer();
    }

    void PopulateList()
    {
        if (scenariosFromDB == null || scenariosFromDB.Count == 0)
        {
            Debug.LogWarning("Нет сценариев для отображения");
            return;
        }

        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        foreach (var kvp in scenariosFromDB)
        {
            int scenarioId = kvp.Key;
            var scenarioName = kvp.Value["name"];

            ScenarioData data = new ScenarioData
            {
                id = scenarioId,
                title = scenarioName
            };

            GameObject newItem = Instantiate(scenarioPrefab, contentPanel);
            ScenarioItemUI itemUI = newItem.GetComponent<ScenarioItemUI>();
            itemUI.Setup(data, this);
        }
    }

    public void OnScenarioClicked(ScenarioItemUI clickedUI, ScenarioData data)
    {
        if (_currentlySelected != null && _currentlySelected != clickedUI)
        {
            _currentlySelected.SetSelected(false);
        }

        _currentlySelected = clickedUI;
        _currentlySelected.SetSelected(true);

        Debug.Log("Выбран сценарий ID: " + data.id + ", Название: " + data.title);

        if (_menuManager != null)
        {
            _menuManager.EnableStartButton();
        }

        if (_menuManager != null)
        {
            _menuManager.OnScenarioSelected(data.id.ToString());
        }
        else
        {
            Debug.LogWarning("ScenarioManager не назначен в ScenarioListManager!");
        }

        if (_scenarioDisplay != null)
        {
            _scenarioDisplay.SetScenarioId(data.id);
        }

        GetCurrentScenarioData(data.id);
    }

    public async void ReadScenariosFromServer()
    {
        Dictionary<string, object> request = new Dictionary<string, object>()
        {
            { "method", "getScenarios" },
        };

        var response = await network.SendRequestAsync(request);

        if (response.TryGetValue("success", out var successObj) && successObj is bool success)
        {
            if (success)
            {
                scenariosFromDB = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<string, object>>>(response["data"].ToString());

                Debug.Log($"Получено сценариев с сервера: {scenariosFromDB.Count}");

                PopulateList();
            }
            else
            {
                Debug.LogError("Сервер вернул success = false");
            }
        }
        else
        {
            Debug.LogError("Не удалось получить ответ от сервера или неверный формат");
        }
    }
    public async void GetCurrentScenarioData(int id_scenario)
    {
        Dictionary<string, object> data = new Dictionary<string, object>()
    {
        {"method", "getScenarioData"},
        {"id_scenario", id_scenario},
    };

        var response = await network.SendRequestAsync(data, timeout: 10f);

        if (response.TryGetValue("success", out var successObj) && successObj is bool success)
        {
            if (success)
            {
                currentScenarioData = JsonConvert.DeserializeObject<Dictionary<object, object>>(response["data"].ToString());
                currentScenarioTechProcess = JsonConvert.DeserializeObject<Dictionary<object, Dictionary<object, object>>>(response["techProcess"].ToString());

                Debug.Log("Данные сценария получены");

                if (_scenarioDisplay != null)
                {
                    _scenarioDisplay.SetScenarioData(currentScenarioData, currentScenarioTechProcess, scenariosFromDB);
                }

                int trainingTime = 0;
                string scenarioName = "";

                if (scenariosFromDB != null && scenariosFromDB.TryGetValue(id_scenario, out var scenarioInfo))
                {
                    if (scenarioInfo.TryGetValue("time", out var timeValue))
                    {
                        trainingTime = System.Convert.ToInt32(timeValue);
                    }

                    if (scenarioInfo.TryGetValue("name", out var nameValue))
                    {
                        scenarioName = nameValue?.ToString() ?? "";
                    }
                }

                ScenarioDataManager.Instance.SaveScenarioData(
                    id_scenario,
                    scenarioName,
                    trainingTime,
                    currentScenarioData,
                    currentScenarioTechProcess,
                    _menuManager._userId
                );
            }
            else
            {
                Debug.LogError("Сервер вернул success = false");
            }
        }
        else
        {
            Debug.LogError("Не удалось получить ответ от сервера или неверный формат");
        }
    }
}