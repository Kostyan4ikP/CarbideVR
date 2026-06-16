using System.Collections.Generic;
using UnityEngine;

public class ScenarioDataManager : MonoBehaviour
{
    private static ScenarioDataManager _instance;
    public static ScenarioDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ScenarioDataManager");
                _instance = go.AddComponent<ScenarioDataManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public int ScenarioId { get; private set; }
    public string ScenarioName { get; private set; }
    public int TrainingTime { get; private set; }
    public int UserId { get; private set; }

    public Dictionary<object, Dictionary<object, object>> TechProcess { get; private set; }
    public Dictionary<object, object> ScenarioData { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveScenarioData(
        int scenarioId,
        string scenarioName,
        int trainingTime,
        Dictionary<object, object> scenarioData,
        Dictionary<object, Dictionary<object, object>> techProcess,
        int userId)
    {
        ScenarioId = scenarioId;
        ScenarioName = scenarioName;
        TrainingTime = trainingTime;
        ScenarioData = scenarioData;
        TechProcess = techProcess;
        UserId = userId;

        Debug.Log($"[ScenarioDataManager] Данные сценария {scenarioId} сохранены");
    }
    public string GetScenarioParam(int paramId)
    {
        if (ScenarioData == null) return null;

        if (ScenarioData.TryGetValue(paramId.ToString(), out var value))
            return value?.ToString();

        return null;
    }
    public double GetScenarioParamAsDouble(int paramId)
    {
        var value = GetScenarioParam(paramId);
        return value != null ? System.Convert.ToDouble(value) : 0;
    }
    public object GetTechParamValue(int paramId, string fieldName = "value")
    {
        if (TechProcess == null) return null;

        string paramIdStr = paramId.ToString();

        foreach (var kvp in TechProcess)
        {
            if (kvp.Key is string strKey && strKey == paramIdStr)
            {
                if (kvp.Value.TryGetValue(fieldName, out var value))
                    return value;
            }
            else if (kvp.Key is long longKey && longKey == paramId)
            {
                if (kvp.Value.TryGetValue(fieldName, out var value))
                    return value;
            }
            else if (kvp.Key is int intKey && intKey == paramId)
            {
                if (kvp.Value.TryGetValue(fieldName, out var value))
                    return value;
            }
        }

        return null;
    }

    public double GetTechParamAsDouble(int paramId, string fieldName = "value")
    {
        var value = GetTechParamValue(paramId, fieldName);
        return value != null ? System.Convert.ToDouble(value) : 0;
    }
    public float GetTrainingTime()
    {
        return TrainingTime;
    }
    public string GetScenarioName()
    {
        return ScenarioName;
    }
    public int GetUsedId()
    {
        return UserId;
    }
    public int GetScenarioId()
    {
        return ScenarioId;
    }
    public void ClearData()
    {
        ScenarioId = 0;
        ScenarioName = null;
        TrainingTime = 0;
        TechProcess = null;
        ScenarioData = null;
        UserId = 0;
    }
}