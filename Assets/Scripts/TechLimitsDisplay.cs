using UnityEngine;
using TMPro;

public class TechLimitsDisplay : MonoBehaviour
{
    [Header("Target UI Elements - Основные параметры")]
    [SerializeField] private TextMeshProUGUI concentrationTarget;
    [SerializeField] private TextMeshProUGUI productivityTarget;
    [SerializeField] private TextMeshProUGUI energyTarget;
    [SerializeField] private TextMeshProUGUI temperatureTarget;
    [SerializeField] private TextMeshProUGUI lmprTarget;

    [Header("Target UI Elements - Выбросы")]
    [SerializeField] private TextMeshProUGUI coTarget;
    [SerializeField] private TextMeshProUGUI co2Target;
    [SerializeField] private TextMeshProUGUI h2Target;

    [Header("Settings")]
    [Tooltip("Формат отображения: {0} - мин, {1} - макс")]
    [SerializeField] private string displayFormat = "Допустимо: {0} – {1}";

    private const int PARAM_CONCENTRATION = 27;
    private const int PARAM_PRODUCTIVITY = 30;
    private const int PARAM_ENERGY = 31;
    private const int PARAM_TEMPERATURE = 28;
    private const int PARAM_LMPR = 29;

    private const int PARAM_CO = 32;
    private const int PARAM_CO2 = 33;
    private const int PARAM_H2 = 34;

    void Start()
    {
        UpdateAllLimits();
    }

    public void UpdateAllLimits()
    {
        UpdateLimitDisplay(concentrationTarget, PARAM_CONCENTRATION, "%");
        UpdateLimitDisplay(productivityTarget, PARAM_PRODUCTIVITY, " т/ч");
        UpdateLimitDisplay(energyTarget, PARAM_ENERGY, " кВт·ч");
        UpdateLimitDisplay(temperatureTarget, PARAM_TEMPERATURE, " °C");
        UpdateLimitDisplay(lmprTarget, PARAM_LMPR, " м");

        UpdateLimitDisplay(coTarget, PARAM_CO, " т/ч");
        UpdateLimitDisplay(co2Target, PARAM_CO2, " т/ч");
        UpdateLimitDisplay(h2Target, PARAM_H2, " т/ч");
    }

    private void UpdateLimitDisplay(TextMeshProUGUI targetText, int paramId, string unit)
    {
        if (targetText == null) return;

        double min = ScenarioDataManager.Instance.GetTechParamAsDouble(paramId, "min");
        double max = ScenarioDataManager.Instance.GetTechParamAsDouble(paramId, "max");

        bool hasMin = min > 0;
        bool hasMax = max > 0;

        if (hasMin && hasMax)
        {
            targetText.text = $"{min:F2}{unit} – {max:F2}{unit}";
        }
        else if (hasMin)
        {
            targetText.text = $"≥ {min:F2}{unit}";
        }
        else if (hasMax)
        {
            targetText.text = $"≤ {max:F2}{unit}";
        }
        else
        {
            targetText.text = "—";
        }
    }

    public void UpdateLimit(int paramId)
    {
        switch (paramId)
        {
            case PARAM_CONCENTRATION:
                UpdateLimitDisplay(concentrationTarget, paramId, "%");
                break;
            case PARAM_PRODUCTIVITY:
                UpdateLimitDisplay(productivityTarget, paramId, " т/ч");
                break;
            case PARAM_ENERGY:
                UpdateLimitDisplay(energyTarget, paramId, " кВт·ч");
                break;
            case PARAM_TEMPERATURE:
                UpdateLimitDisplay(temperatureTarget, paramId, " °C");
                break;
            case PARAM_LMPR:
                UpdateLimitDisplay(lmprTarget, paramId, " м");
                break;
            case PARAM_CO:
                UpdateLimitDisplay(coTarget, paramId, " т/ч");
                break;
            case PARAM_CO2:
                UpdateLimitDisplay(co2Target, paramId, " т/ч");
                break;
            case PARAM_H2:
                UpdateLimitDisplay(h2Target, paramId, " т/ч");
                break;
        }
    }

    public void Refresh()
    {
        UpdateAllLimits();
    }
}