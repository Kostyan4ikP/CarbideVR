using TMPro;
using UnityEngine;

/// <summary>
/// Читает текущее состояние модели из TechProcessController
/// и обновляет TMP-поля на пульте оператора.
/// Повесить на любой GameObject на сцене; назначить поля в Inspector.
/// </summary>
public class SimulationDisplay : MonoBehaviour
{
    [SerializeField] private TechProcessController controller;

    [Header("Числовые показатели")]
    [SerializeField] private TextMeshProUGUI concentrationText;   // C, %
    [SerializeField] private TextMeshProUGUI temperatureText;     // T, °C
    [SerializeField] private TextMeshProUGUI powerText;           // Q, кВт
    [SerializeField] private TextMeshProUGUI productivityText;    // G, т/ч
    [SerializeField] private TextMeshProUGUI meltMassText;        // Mraspl, кг
    [SerializeField] private TextMeshProUGUI arcLengthText;       // L_mpr, м
    [SerializeField] private TextMeshProUGUI voltageText;         // U, В
    [SerializeField] private TextMeshProUGUI efficiencyText;      // КПД, %
    [SerializeField] private TextMeshProUGUI simTimeText;         // t, ч

    void Update()
    {
        if (controller == null || !controller.IsRunning || controller.CurrentState == null)
            return;

        var s = controller.CurrentState;

        if (concentrationText  != null) concentrationText.text  = $"{s.C:F2} %";
        if (temperatureText    != null) temperatureText.text    = $"{s.Temperature:F0} °C";
        if (powerText          != null) powerText.text          = $"{s.Q:F0} кВт";
        if (productivityText   != null) productivityText.text   = $"{s.Gprod / 1000.0:F2} т/ч";
        if (meltMassText       != null) meltMassText.text       = $"{s.Mraspl:F0} кг";
        if (arcLengthText      != null) arcLengthText.text      = $"{s.L_mpr:F3} м";
        if (voltageText        != null) voltageText.text        = $"{s.U:F1} В";
        if (efficiencyText     != null) efficiencyText.text     = $"{s.KPD * 100.0:F1} %";
        if (simTimeText        != null) simTimeText.text        = $"{s.Time:F2} ч";
    }
}
