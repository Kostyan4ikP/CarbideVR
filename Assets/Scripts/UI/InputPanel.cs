using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class InputPanel : MonoBehaviour
{
    [Header("Поле ввода")]
    [SerializeField] private TMP_InputField _inputField;

    [Header("Кнопки цифр")]
    [SerializeField] private Button _button1;
    [SerializeField] private Button _button2;
    [SerializeField] private Button _button3;
    [SerializeField] private Button _button4;
    [SerializeField] private Button _button5;
    [SerializeField] private Button _button6;
    [SerializeField] private Button _button7;
    [SerializeField] private Button _button8;
    [SerializeField] private Button _button9;
    [SerializeField] private Button _button0;

    [Header("Специальные кнопки")]
    [SerializeField] private Button _buttonBackspace;
    [SerializeField] private Button _buttonClear;
    [SerializeField] private Button _buttonOK;
    [SerializeField] private Button _buttonDecimal;
    [SerializeField] private Button _buttonMinus;
    [SerializeField] private Button _buttonBack;

    [Header("Настройки")]
    [SerializeField] private int _maxLength = 10;

    public event Action OnValueConfirmed;
    public event Action OnValueCancelled;

    private Action<string> _onValueReceived;

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        _button1?.onClick.AddListener(() => AppendDigit("1"));
        _button2?.onClick.AddListener(() => AppendDigit("2"));
        _button3?.onClick.AddListener(() => AppendDigit("3"));
        _button4?.onClick.AddListener(() => AppendDigit("4"));
        _button5?.onClick.AddListener(() => AppendDigit("5"));
        _button6?.onClick.AddListener(() => AppendDigit("6"));
        _button7?.onClick.AddListener(() => AppendDigit("7"));
        _button8?.onClick.AddListener(() => AppendDigit("8"));
        _button9?.onClick.AddListener(() => AppendDigit("9"));
        _button0?.onClick.AddListener(() => AppendDigit("0"));

        _buttonBackspace?.onClick.AddListener(OnBackspaceClicked);
        _buttonClear?.onClick.AddListener(OnClearClicked);
        _buttonMinus?.onClick.AddListener(OnMinusClicked);
        _buttonDecimal?.onClick.AddListener(OnDecimalClicked);
        _buttonBack?.onClick.AddListener(OnBackClicked);
        _buttonOK?.onClick.AddListener(OnOKClicked);
    }

    public void ShowForField(Action<string> targetFieldSetter, string initialValue = "")
    {
        _onValueReceived = targetFieldSetter;

        _inputField.text = string.IsNullOrEmpty(initialValue) ? "" : initialValue;

        gameObject.SetActive(true);
        _inputField?.ActivateInputField();
    }

    private void AppendDigit(string digit)
    {
        if (_inputField.text.Length >= _maxLength) return;

        if (_inputField.text == "0" || _inputField.text == "-0")
        {
            string sign = _inputField.text.StartsWith("-") ? "-" : "";
            _inputField.text = sign + digit;
        }
        else
        {
            _inputField.text += digit;
        }
    }

    private void OnBackspaceClicked()
    {
        if (string.IsNullOrEmpty(_inputField.text)) return;

        _inputField.text = _inputField.text.Substring(0, _inputField.text.Length - 1);

        if (_inputField.text == "-") _inputField.text = "";
    }

    private void OnClearClicked()
    {
        _inputField.text = "";
    }

    private void OnMinusClicked()
    {
        if (string.IsNullOrEmpty(_inputField.text))
        {
            _inputField.text = "-";
        }
        else if (_inputField.text.StartsWith("-"))
        {
            _inputField.text = _inputField.text.Substring(1);
        }
        else
        {
             _inputField.text = "-" + _inputField.text;
        }
    }

    private void OnDecimalClicked()
    {
        if (_inputField.text.Contains(".")) return;

        if (string.IsNullOrEmpty(_inputField.text) || _inputField.text == "-")
        {
            _inputField.text += "0.";
        }
        else
        {
            _inputField.text += ".";
        }
    }

    private void OnOKClicked()
    {
        string result = _inputField.text;

        if (string.IsNullOrEmpty(result) || result == "-")
        {
            result = "";
            _inputField.text = "";
        }

        _onValueReceived?.Invoke(result);

        OnValueConfirmed?.Invoke();

        _onValueReceived = null;
        gameObject.SetActive(false);
    }

    private void OnBackClicked()
    {
        _inputField.text = "";
        _onValueReceived = null;
        OnValueCancelled?.Invoke();
        gameObject.SetActive(false);
    }

    public void SetValue(string value)
    {
        _inputField.text = value;
    }
}