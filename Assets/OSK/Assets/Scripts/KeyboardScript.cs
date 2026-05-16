using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardScript : MonoBehaviour
{
    public TMP_InputField TextField; // Визуальное поле на самой клавиатуре
    public GameObject RusLayoutSml, RusLayoutBig, EngLayoutSml, EngLayoutBig, SymbLayout;

    // Ссылка на реальное поле ввода в форме (Авторизация и т.д.)
    private TMP_InputField _targetField;

    // Метод для установки цели ввода (вызывается при открытии клавиатуры)
    public void SetTargetField(TMP_InputField inputField)
    {
        _targetField = inputField;
        // При открытии копируем текст из формы в визуальное поле клавиатуры
        if (_targetField != null && TextField != null)
        {
            TextField.text = _targetField.text;
        }
    }

    public void alphabetFunction(string alphabet)
    {
        // Добавляем символ в визуальное поле клавиатуры
        TextField.text += alphabet;

        // Сразу добавляем в целевое поле формы
        if (_targetField != null)
        {
            _targetField.text += alphabet;
        }
    }

    public void BackSpace()
    {
        // Удаляем из визуального поля
        if (TextField.text.Length > 0)
            TextField.text = TextField.text.Remove(TextField.text.Length - 1);

        // Сразу удаляем из целевого поля формы
        if (_targetField != null && _targetField.text.Length > 0)
        {
            _targetField.text = _targetField.text.Remove(_targetField.text.Length - 1);
        }
    }

    public void CloseAllLayouts()
    {
        RusLayoutSml.SetActive(false);
        RusLayoutBig.SetActive(false);
        EngLayoutSml.SetActive(false);
        EngLayoutBig.SetActive(false);
        SymbLayout.SetActive(false);
    }

    public void ShowLayout(GameObject SetLayout)
    {
        CloseAllLayouts();
        SetLayout.SetActive(true);
    }

    // Метод для кнопки "Закрыть/Готово" на самой клавиатуре
    public void HideKeyboard()
    {
        gameObject.SetActive(false);
    }

    // Старый метод SetText теперь не обязателен для логики, но можно оставить для совместимости
    public void SetText(TMP_InputField inputField)
    {
        if (inputField != null) inputField.text = TextField.text;
    }
}