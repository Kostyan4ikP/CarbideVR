using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerTextTMP;
    [SerializeField] private UnityEngine.Events.UnityEvent onTimerComplete;

    private float timeRemaining;
    private Coroutine timerCoroutine;

    private void OnEnable()
    {
        timeRemaining = ScenarioDataManager.Instance.GetTrainingTime() * 60f;

        if (timerCoroutine == null)
        {
            Debug.Log("Таймер активирован и запущен!");
            timerCoroutine = StartCoroutine(TimerCoroutine());
        }
    }

    private void OnDisable()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            Debug.Log("Таймер остановлен (объект скрыт).");
        }
    }

    IEnumerator TimerCoroutine()
    {
        while (timeRemaining > 0)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (timerTextTMP != null)
            {
                timerTextTMP.text = timeString;
            }
            else
            {
                Debug.LogError("Ошибка таймера: Поле timerTextTMP не назначено в Инспекторе!", this);
            }

            yield return null;
            timeRemaining -= Time.unscaledDeltaTime;
        }

        if (timerTextTMP != null)
            timerTextTMP.text = "00:00";

        Debug.Log("Таймер завершен!");
        onTimerComplete?.Invoke();
    }
}