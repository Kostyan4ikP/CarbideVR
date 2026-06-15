using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Отображает данные выбранного сценария в UI-полях.
/// </summary>
public class ScenarioDisplay : MonoBehaviour
{
    [Header("Основная информация")]
    [SerializeField] private TextMeshProUGUI timeText;           // Время сценария
    [SerializeField] private TextMeshProUGUI equipmentNameText;  // nameEquip
    [SerializeField] private TextMeshProUGUI materialNameText;   // nameMaterial

    [Header("Технологические параметры (6 полей)")]
    [SerializeField] private TextMeshProUGUI techParam1Text;     // Например, id_param = 1
    [SerializeField] private TextMeshProUGUI techParam2Text;     // id_param = 2
    [SerializeField] private TextMeshProUGUI techParam3Text;     // id_param = 3
    [SerializeField] private TextMeshProUGUI techParam4Text;     // id_param = 4
    [SerializeField] private TextMeshProUGUI techParam5Text;     // id_param = 5
    [SerializeField] private TextMeshProUGUI techParam6Text;     // id_param = 6

    private Dictionary<object, object> _currentScenarioData;
    private Dictionary<object, Dictionary<object, object>> _currentTechProcess;
    private int _selectedScenarioId;

    public void SetScenarioId(int scenarioId)
    {
        _selectedScenarioId = scenarioId;
    }

    public void SetScenarioData(
        Dictionary<object, object> scenarioData,
        Dictionary<object, Dictionary<object, object>> techProcess,
        Dictionary<int, Dictionary<string, object>> scenariosFromDB)
    {
        _currentScenarioData = scenarioData;
        _currentTechProcess = techProcess;

        UpdateDisplay(scenariosFromDB);
    }
    private void UpdateDisplay(Dictionary<int, Dictionary<string, object>> scenariosFromDB)
    {
        if (timeText != null && scenariosFromDB != null)
        {
            if (scenariosFromDB.TryGetValue(_selectedScenarioId, out var scenarioInfo))
            {
                if (scenarioInfo.TryGetValue("time", out var timeValue))
                {
                    timeText.text = $"{timeValue} мин";
                }
            }
        }

        if (equipmentNameText != null && _currentScenarioData != null)
        {
            if (_currentScenarioData.TryGetValue("nameEquip", out var equipName))
            {
                equipmentNameText.text = equipName?.ToString() ?? "—";
            }
        }

        if (materialNameText != null && _currentScenarioData != null)
        {
            if (_currentScenarioData.TryGetValue("nameMaterial", out var materialName))
            {
                materialNameText.text = materialName?.ToString() ?? "—";
            }
        }

        DisplayTechParam(27, "value", techParam1Text);
        DisplayTechParam(27, "min", techParam2Text);
        DisplayTechParam(30, "min", techParam3Text);
        DisplayTechParam(31, "max", techParam4Text);
        DisplayTechParam(28, "min", techParam5Text);
        DisplayTechParam(28, "max", techParam6Text);
    }
    private void DisplayTechParam(int paramId, string fieldName, TextMeshProUGUI textField)
    {
        if (textField == null || _currentTechProcess == null)
        {
            textField?.SetText("—");
            return;
        }

        Dictionary<object, object> paramData = null;

        foreach (var kvp in _currentTechProcess)
        {
            try
            {
                long keyAsLong = Convert.ToInt64(kvp.Key);
                if (keyAsLong == paramId)
                {
                    paramData = kvp.Value;
                    break;
                }
            }
            catch
            {
                continue;
            }
        }

        if (paramData == null)
        {
            textField.text = "—";
            return;
        }

        if (paramData.TryGetValue(fieldName, out var value) && value != null)
        {
            textField.text = value.ToString();
        }
        else
        {
            textField.text = "—";
        }
    }
    public void ClearDisplay()
    {
        if (timeText != null) timeText.text = "";
        if (equipmentNameText != null) equipmentNameText.text = "";
        if (materialNameText != null) materialNameText.text = "";
        if (techParam1Text != null) techParam1Text.text = "";
        if (techParam2Text != null) techParam2Text.text = "";
        if (techParam3Text != null) techParam3Text.text = "";
        if (techParam4Text != null) techParam4Text.text = "";
        if (techParam5Text != null) techParam5Text.text = "";
        if (techParam6Text != null) techParam6Text.text = "";
    }
}