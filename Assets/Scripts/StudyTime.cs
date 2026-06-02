using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerTextTMP; // или TextMeshPro
    [SerializeField] private UnityEngine.Events.UnityEvent onTimerComplete;

    private float timeRemaining = 45f * 60f; // 45 минут в секундах

    void Start()
    {
        StartCoroutine(TimerCoroutine());
    }

    IEnumerator TimerCoroutine()
    {
        while (timeRemaining > 0)
        {
            // Обновляем отображение
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);


            if (timerTextTMP != null)
                timerTextTMP.text = timeString;

            yield return null;
            timeRemaining -= Time.deltaTime;
        }

        if (timerTextTMP != null)
            timerTextTMP.text = "00:00";

        // Вызываем ивент по окончанию
        onTimerComplete?.Invoke();
    }
}