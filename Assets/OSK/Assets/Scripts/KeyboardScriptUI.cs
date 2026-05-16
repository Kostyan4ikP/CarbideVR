using UnityEngine;
using TMPro;

public class KeyboardScriptUI : MonoBehaviour
{
    public TMP_InputField input; // Поле ввода на форме авторизации
    public GameObject keyboard;  // Объект клавиатуры

    void Start()
    {
        // Подписываемся на событие выбора поля
        input.onSelect.AddListener(OpenKeyboard);
    }

    void OpenKeyboard(string text)
    {
        if (keyboard == null) return;

        var script = keyboard.GetComponent<KeyboardScript>();

        // 1. Говорим клавиатуре, куда писать текст
        script.SetTargetField(input);

        // 2. Активируем клавиатуру
        keyboard.SetActive(true);

        // 3. (Опционально) Сразу открываем нужную раскладку, например, английскую маленькую
        // script.ShowLayout(script.EngLayoutSml); 
    }
}