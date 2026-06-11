using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ScenarioData
{
    public string id;
    public string title;
}

public class ScenarioListManager : MonoBehaviour
{
    [Header("Настройки UI")]
    public Transform contentPanel;
    public GameObject scenarioPrefab;

    [Header("Ссылка на ScenarioManager")]
    [SerializeField] private ScenarioManager _scenarioManager; 

    [Header("Данные из БД")]
    public List<ScenarioData> scenariosFromDB;

    private ScenarioItemUI _currentlySelected;

    void Start()
    {
        if (scenariosFromDB == null || scenariosFromDB.Count == 0)
        {
            scenariosFromDB = new List<ScenarioData>
            {
                new ScenarioData { id = "1", title = "Сценарий 1" }
            };
        }
        PopulateList();
    }

    void PopulateList()
    {
        // Очищаем старые элементы
        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        // Создаем новые элементы
        foreach (var data in scenariosFromDB)
        {
            GameObject newItem = Instantiate(scenarioPrefab, contentPanel);
            ScenarioItemUI itemUI = newItem.GetComponent<ScenarioItemUI>();
            itemUI.Setup(data, this);
        }

        // 🔹 НОВОЕ: автоматически выбираем первый элемент, если список не пуст
        if (scenariosFromDB.Count > 0 && contentPanel.childCount > 0)
        {
            // Получаем первый элемент из списка
            Transform firstItemTransform = contentPanel.GetChild(0);
            ScenarioItemUI firstItemUI = firstItemTransform.GetComponent<ScenarioItemUI>();

            // Получаем данные первого сценария
            ScenarioData firstData = scenariosFromDB[0];

            // Программно "кликаем" по первому элементу
            if (firstItemUI != null)
            {
                OnScenarioClicked(firstItemUI, firstData);
            }
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

        if (_scenarioManager != null)
        {
            _scenarioManager.OnScenarioSelected(data.id);
        }
        else
        {
            Debug.LogWarning("ScenarioManager не назначен в ScenarioListManager!");
        }
    }
}