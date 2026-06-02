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
    [SerializeField] private TextMeshProUGUI simTimeText;         // t, ч
    [SerializeField] private TextMeshProUGUI COText;              // %
    [SerializeField] private TextMeshProUGUI CO2Text;             // %
    [SerializeField] private TextMeshProUGUI H2Text;              // %

    void Update()
    {
        if (controller == null || !controller.IsRunning || controller.CurrentState == null)
            return;

        var s = controller.CurrentState;

        if (concentrationText  != null) concentrationText.text  = $"{s.C:F2}";
        if (temperatureText    != null) temperatureText.text    = $"{s.Temperature:F0}";
        if (powerText          != null) powerText.text          = $"{s.Q:F0}";
        if (productivityText   != null) productivityText.text   = $"{s.Gprod / 1000.0:F2}";
        if (meltMassText       != null) meltMassText.text       = $"{s.MeltMass:F0}";
        if (arcLengthText      != null) arcLengthText.text      = $"{s.L_mpr:F3}";
        if (voltageText        != null) voltageText.text        = $"{s.U:F1}";
        if (simTimeText        != null) simTimeText.text        = $"{s.Time:F2}";
        if (COText             != null) COText.text             = $"{s.CO / 1000:F3}";
        if (CO2Text            != null) CO2Text.text            = $"{s.CO2 / 1000:F3}";
        if (H2Text             != null) H2Text.text             = $"{s.H2 / 1000:F3}";
    }
}
