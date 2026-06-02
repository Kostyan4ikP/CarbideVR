using UnityEngine;
using XCharts.Runtime;

public class ChartDisplay : MonoBehaviour
{
    [SerializeField] private TechProcessController controller;
    [SerializeField] private LineChart temperatureChart;
    [SerializeField] private LineChart powerChart;
    [SerializeField] private LineChart concentrationChart;
    [SerializeField] private LineChart productivityChart;
    [SerializeField] private LineChart lmprChart;

    private double _lastRecordedTime = -1f;

    void Update()
    {
        if (controller == null || !controller.IsRunning || controller.CurrentState == null)
            return;

        var state = controller.CurrentState;

        // ƒобавл€ем точку “ќЋ№ ќ когда врем€ симул€ции изменилось
        if (System.Math.Abs(state.Time - _lastRecordedTime) < 0.0001)
            return;

        // —лив расплава Ч врем€ сбросилось, очищаем графики
        if (state.Time < _lastRecordedTime)
        {
            ClearAllCharts();
        }

        _lastRecordedTime = state.Time;

        AddPoint(temperatureChart, (float)state.Time, (float)state.Temperature);
        AddPoint(powerChart, (float)state.Time, (float)state.Q);
        AddPoint(concentrationChart, (float)state.Time, (float)state.C);
        AddPoint(productivityChart, (float)state.Time, (float)state.Gprod / 1000f);
        AddPoint(lmprChart, (float)state.Time, (float)state.L_mpr);
    }

    private void AddPoint(LineChart chart, float x, float y)
    {
        if (chart == null) return;

        // явно указываем тип Line
        if (chart.series.Count == 0)
        {
            chart.AddSerie<Line>("data");
        }

        chart.AddData(0, x, y);
    }

    public void ClearAllCharts()
    {
        temperatureChart?.ClearData();
        powerChart?.ClearData();
        concentrationChart?.ClearData();
        productivityChart?.ClearData();
        lmprChart?.ClearData();
        _lastRecordedTime = -1f;

        Debug.Log("√рафики очищены");
    }
}