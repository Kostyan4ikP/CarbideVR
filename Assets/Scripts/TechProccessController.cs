using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Analytics.IAnalytic;

public class TechProcessController : MonoBehaviour
{
    private const double SimStepHours = 0.05;

    [Header("Ссылки на UI")]
    [SerializeField] private TextMeshProUGUI electrodeValueText;
    [SerializeField] private TextMeshProUGUI limeValueText;
    [SerializeField] private TextMeshProUGUI cokeValueText;
    [SerializeField] private Button startSimulationButton;

    [Header("Ссылка на графики")]
    [SerializeField] private ChartDisplay chartDisplay;

    [Header("Скорость симуляции")]
    [SerializeField] private float realSecondsPerSimStep = 1f;

    [Header("Ограничение симуляции")]
    [SerializeField] private int maxDrainsBeforeStop = 3;

    [Header("Сервер")]
    public VRClient network;

    private int _drainCount = 0;

    private double _electrodeMovement;
    private double _limeFeed;
    private double _cokeFeed;

    private CalciumCarbideModel _model;
    private ControlInputs _controlInputs = new ControlInputs();
    private Coroutine _simulationCoroutine;

    public SimulationStep CurrentState { get; private set; }
    public bool IsRunning { get; private set; }
    public string protocolName { get; private set; }
    public int modelingNumber { get; private set; } = 0;
    public List<SimulationStep> modelingProtocol { get; set; }
    public DateTime startProtocolTime = DateTime.Now;

    void Start()
    {
        InitializeParameters();
        UpdateUI();
    }
    private void InitializeParameters()
    {
        _electrodeMovement = ScenarioDataManager.Instance.GetTechParamAsDouble(37, "value");
        _limeFeed = ScenarioDataManager.Instance.GetTechParamAsDouble(35, "value");
        _cokeFeed = ScenarioDataManager.Instance.GetTechParamAsDouble(36, "value");

        Debug.Log($"[TechProcessController] Параметры инициализированы: electrode={_electrodeMovement}, lime={_limeFeed}, coke={_cokeFeed}");
    }

    public void ChangeParameter(string param, float delta)
    {
        switch (param)
        {
            case "Electrode":
                _electrodeMovement = Math.Clamp(
                    _electrodeMovement + delta,
                    ScenarioDataManager.Instance.GetTechParamAsDouble(37, "min"),
                    ScenarioDataManager.Instance.GetTechParamAsDouble(37, "max"));
                break;
            case "Lime":
                _limeFeed = Math.Clamp(
                    _limeFeed + delta,
                    ScenarioDataManager.Instance.GetTechParamAsDouble(35, "min"),
                    ScenarioDataManager.Instance.GetTechParamAsDouble(35, "max"));
                break;
            case "Coke":
                _cokeFeed = Math.Clamp(
                    _cokeFeed + delta,
                    ScenarioDataManager.Instance.GetTechParamAsDouble(36, "min"),
                    ScenarioDataManager.Instance.GetTechParamAsDouble(36, "max"));
                break;
        }

        ApplyControlInputs();
        UpdateUI();
    }

    public void StartSimulation()
    {
        if (IsRunning) return;

        ChartDisplay chartDisplay = FindFirstObjectByType<ChartDisplay>();
        if (chartDisplay != null)
            chartDisplay.ClearAllCharts();

        _model = new CalciumCarbideModel();
        CurrentState = _model.InitialState();
        modelingProtocol = new List<SimulationStep>();
        modelingProtocol.Add(CurrentState);
        ApplyControlInputs();

        if (startSimulationButton != null)
            startSimulationButton.interactable = false;

        protocolName = ScenarioDataManager.Instance.GetScenarioName() + " " + startProtocolTime;
        modelingNumber += 1;

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

    private void ApplyControlInputs()
    {
        if (_controlInputs == null)
        {
            _controlInputs = new ControlInputs();
            Debug.LogWarning("[TechProcessController] _controlInputs был null, создан заново");
        }

        _controlInputs.G_izvest = _limeFeed * 1000.0;
        _controlInputs.G_coks = _cokeFeed * 1000.0;
        _controlInputs.K_ctrl = _electrodeMovement > 0 ? 1
            : _electrodeMovement < 0 ? -1 : 0;

        _controlInputs.L_ctrl = Math.Abs(_electrodeMovement / 1000);
    }

    private void UpdateUI()
    {
        if (electrodeValueText != null)
            electrodeValueText.text = $"{_electrodeMovement}";
        if (limeValueText != null)
            limeValueText.text = $"{_limeFeed:F1}";
        if (cokeValueText != null)
            cokeValueText.text = $"{_cokeFeed:F1}";
    }

    private IEnumerator SimulationCoroutine()
    {
        IsRunning = true;
        _drainCount = 0;

        while (true)
        {
            double targetTime = CurrentState.Time + SimStepHours;
            bool drainOccurredThisStep = false;

            while (CurrentState.Time < targetTime - _model.DtStep * 0.5)
            {
                CurrentState = _model.Advance(CurrentState, _controlInputs);
                modelingProtocol.Add(CurrentState);

                if (CurrentState.DrainEvent)
                    drainOccurredThisStep = true;
            }

            if (drainOccurredThisStep)
            {
                _drainCount++;
                Debug.Log($"[CarbideVR] Слив расплава! №{_drainCount}, t = {CurrentState.Time:F2} ч");

                if (_drainCount >= maxDrainsBeforeStop)
                {
                    Debug.Log($"[CarbideVR] Достигнут лимит сливов ({maxDrainsBeforeStop}). Остановка симуляции.");
                    StopSimulation();
                    SendProtocolToServer();
                    yield break;
                }
            }

            yield return new WaitForSeconds(realSecondsPerSimStep);
        }
    }

    private async void SendProtocolToServer()
    {
        Dictionary<string, object> request = new Dictionary<string, object>()
        {
            { "method", "sendProtocol" },
            { "protocolName", protocolName },
            { "protocolData", modelingProtocol },
            { "modelingNumber", modelingNumber },
            { "userId", ScenarioDataManager.Instance.UserId },
            { "scenarioId", ScenarioDataManager.Instance.ScenarioId }
        };

        var response = await network.SendRequestAsync(request, timeout: 10f);

        if (response.TryGetValue("success", out var successObj) && successObj is bool success)
        {
            if (success)
            {
                Debug.Log("Протокол успешно добавлен!");
            }
            else
            {
                string error = response.TryGetValue("error", out var e) ? e.ToString() : "Ошибка";
                Debug.Log("Ошибка:\n" + error);
            }
        }
    }
}