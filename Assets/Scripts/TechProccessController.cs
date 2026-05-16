using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Центральный контроллер симуляции карбидной печи.
/// Запускает непрерывный цикл модели; кнопки меняют управляющие воздействия на лету.
/// Один шаг симуляции (SimStepHours ч) выполняется каждые realSecondsPerSimStep реальных секунд.
/// </summary>
public class TechProcessController : MonoBehaviour
{
    // Каждый тик реального таймера продвигает симуляцию на 0.05 ч (как в WinForms-симуляторе)
    private const double SimStepHours = 0.05;

    [Header("Ссылки на UI")]
    [SerializeField] private TextMeshProUGUI electrodeValueText;
    [SerializeField] private TextMeshProUGUI limeValueText;
    [SerializeField] private TextMeshProUGUI cokeValueText;
    [SerializeField] private Button startSimulationButton;

    [Header("Скорость симуляции")]
    [Tooltip("Сколько реальных секунд занимает один шаг модели (0.05 ч)")]
    [SerializeField] private float realSecondsPerSimStep = 1f;

    // Текущие параметры управления (т/ч для извести и кокса)
    private double _electrodeMovement = 0.0;  // диапазон [-0.02, +0.02]
    private double _limeFeed  = 11.0;          // т/ч
    private double _cokeFeed  = 4.0;           // т/ч

    private CarbideModel    _model;
    private ControlInputs   _controlInputs = new ControlInputs();
    private Coroutine       _simulationCoroutine;

    /// <summary>Текущее состояние модели, доступно для чтения дисплеями.</summary>
    public SimulationStep CurrentState { get; private set; }

    /// <summary>Симуляция запущена и работает.</summary>
    public bool IsRunning { get; private set; }

    void Start()
    {
        UpdateUI();
    }

    // ── Управление параметрами (вызывается кнопками через VRButtonHandler) ──

    /// <summary>
    /// Изменяет один из управляющих параметров.
    /// param: "Electrode" | "Lime" | "Coke"
    /// delta: шаг изменения (положительный или отрицательный)
    /// </summary>
    public void ChangeParameter(string param, float delta)
    {
        switch (param)
        {
            case "Electrode":
                _electrodeMovement = Math.Clamp(_electrodeMovement + delta, -0.020, 0.020);
                break;
            case "Lime":
                _limeFeed = Math.Clamp(_limeFeed + delta, 8.0, 16.0);
                break;
            case "Coke":
                _cokeFeed = Math.Clamp(_cokeFeed + delta, 2.0, 8.0);
                break;
        }

        ApplyControlInputs();
        UpdateUI();
    }

    // ── Запуск / остановка ───────────────────────────────────────────────

    public void StartSimulation()
    {
        if (IsRunning) return;

        _model       = new CarbideModel();
        CurrentState = _model.InitialState();
        ApplyControlInputs();

        if (startSimulationButton != null)
            startSimulationButton.interactable = false;

        _simulationCoroutine = StartCoroutine(SimulationCoroutine());
    }

    public void StopSimulation()
    {
        if (_simulationCoroutine != null)
        {
            StopCoroutine(_simulationCoroutine);
            _simulationCoroutine = null;
        }

        IsRunning = false;

        if (startSimulationButton != null)
            startSimulationButton.interactable = true;
    }

    // ── Внутренняя логика ────────────────────────────────────────────────

    private void ApplyControlInputs()
    {
        _controlInputs.G_izvest = _limeFeed * 1000.0;
        _controlInputs.G_coks   = _cokeFeed * 1000.0;
        _controlInputs.K_ctrl   = _electrodeMovement >  1e-9 ? 1
                                 : _electrodeMovement < -1e-9 ? -1 : 0;
        // Скорость движения электрода в м/ч, нормированная на шаг симуляции
        _controlInputs.L_ctrl   = Math.Abs(_electrodeMovement) / SimStepHours;
    }

    private void UpdateUI()
    {
        if (electrodeValueText != null)
            electrodeValueText.text = $"{_electrodeMovement:F3}";
        if (limeValueText != null)
            limeValueText.text = $"{_limeFeed:F1}";
        if (cokeValueText != null)
            cokeValueText.text = $"{_cokeFeed:F1}";
    }

    private IEnumerator SimulationCoroutine()
    {
        IsRunning = true;

        while (true)
        {
            // Продвигаем модель на SimStepHours, используя мелкие внутренние шаги DtStep
            double targetTime = CurrentState.Time + SimStepHours;
            while (CurrentState.Time < targetTime - _model.DtStep * 0.5)
                CurrentState = _model.Advance(CurrentState, _controlInputs);

            // Слив расплава: сбрасываем время и оставляем остаток
            if (CurrentState.Mraspl >= _model.Mraspl_max)
            {
                Debug.Log($"[CarbideVR] Слив расплава! t = {CurrentState.Time:F2} ч");
                CurrentState = _model.InitialState();
            }

            yield return new WaitForSeconds(realSecondsPerSimStep);
        }
    }
}
