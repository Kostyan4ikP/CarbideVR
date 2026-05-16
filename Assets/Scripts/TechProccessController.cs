using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TechProcessController : MonoBehaviour
{
    [Header("Ссылки на объекты из Canvas")]
    [SerializeField] private TextMeshProUGUI electrodeValueText;
    [SerializeField] private TextMeshProUGUI limeValueText;
    [SerializeField] private TextMeshProUGUI cokeValueText;
    [SerializeField] private Button startSimulationButton;

    [Header("Настройка модели")]
    private int simulationDuration = 120; // 2 минуты
    private float calculationStep = 1f; // 1 секунда

    [Header("Текущие значения")]
    private double electrodeMovement = 0.0;
    private double limeFeed = 12.0;
    private double cokeFeed = 6.0;

    ControlInputs controlInputs = new ControlInputs();

    MathModel mathModel = new MathModel();
    SimulationStep simulationStep = new SimulationStep();
    private Coroutine simulationCoroutine;

    void Start()
    {
        UpdateUI();
    }
     
    public void ChangeParameter(string param, float delta)
    {
        switch (param)
        {
            case "Electrode":
                electrodeMovement = Math.Clamp(electrodeMovement + delta, -0.020, 0.020);
                break;
            case "Lime":
                limeFeed = Math.Clamp(limeFeed + delta, 8.0, 16.0);
                break;
            case "Coke":
                cokeFeed = Math.Clamp(cokeFeed + delta, 4.0, 8.0);
                break;
        }

        UpdateUI();

        // Здесь позже добавим вызов математической модели
        // RecalculateProcess();
    }

    void UpdateUI()
    {
        if (electrodeValueText != null)
            electrodeValueText.text = $"{electrodeMovement:F3}";

        if (limeValueText != null)
            limeValueText.text = $"{limeFeed:F1}";

        if (cokeValueText != null)
            cokeValueText.text = $"{cokeFeed:F1}";
    }

    public void StartSimulation()
    {
        Debug.Log("Моделирование запущено!");

        mathModel = new MathModel();
        simulationStep = mathModel.InitialState();

        controlInputs.G_lime = limeFeed * 1000;
        controlInputs.G_coke = cokeFeed * 1000;
        controlInputs.L_ctrl = electrodeMovement;
        controlInputs.K_ctrl = electrodeMovement > 0.0 ? 1 : (electrodeMovement < 0.0 ? -1 : 0);

        if (startSimulationButton != null)
            startSimulationButton.interactable = false;

        if (simulationCoroutine != null) return;

        Debug.Log("Моделирование запущено на 120 секунд");
        simulationCoroutine = StartCoroutine(SimulationCoroutine());
    }

    private IEnumerator SimulationCoroutine()
    {
        float currentTime = 0f;

        while (currentTime < simulationDuration)
        {
            mathModel.Advance(simulationStep, controlInputs);
            // Ждём следующий шаг расчёта
            yield return new WaitForSeconds(calculationStep);

            // Увеличиваем время
            currentTime += calculationStep;
        }

        // Моделирование завершено
        Debug.Log("Моделирование завершено");
    }
}