using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerTextTMP;
    [SerializeField] private UnityEngine.Events.UnityEvent onTimerComplete;

    private float timeRemaining = 45f * 60f; // 45 минут в секундах
    private Coroutine timerCoroutine;

    // Используем OnEnable вместо Start. 
    // Он вызывается каждый раз, когда объект становится активным (SetActive(true))
    private void OnEnable()
    {
        // Запускаем таймер только если он еще не запущен
        if (timerCoroutine == null)
        {
            Debug.Log("Таймер активирован и запущен!");
            timerCoroutine = StartCoroutine(TimerCoroutine());
        }
    }

    // Останавливаем корутину, когда объект скрывают (SetActive(false))
    // Это предотвращает утечки памяти и баги при повторной активации
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
            // Обновляем отображение
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (timerTextTMP != null)
            {
                timerTextTMP.text = timeString;
            }
            else
            {
                // ВАЖНО: Этот лог подскажет вам, если вы забыли перетащить текст в инспектор!
                Debug.LogError("Ошибка таймера: Поле timerTextTMP не назначено в Инспекторе!", this);
            }

            yield return null;

            // ИСПОЛЬЗУЕМ unscaledDeltaTime! 
            // Это гарантирует, что таймер будет тикать, даже если игра на паузе во время авторизации.
            timeRemaining -= Time.unscaledDeltaTime;
        }

        if (timerTextTMP != null)
            timerTextTMP.text = "00:00";

        Debug.Log("Таймер завершен!");
        onTimerComplete?.Invoke();
    }
}