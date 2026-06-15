using UnityEngine;
using TMPro;

public class InputPanelUI : MonoBehaviour
{
    [Header("Обязательные ссылки")]
    [SerializeField] private TMP_InputField _targetInputField;
    [SerializeField] private InputPanel _numpadPanel;

    private void Reset()
    {
        if (_targetInputField == null)
            _targetInputField = GetComponent<TMP_InputField>();
    }

    private void Start()
    {
        if (_targetInputField == null)
        {
            Debug.LogError($"[NumpadOpener] Не назначен InputField на объекте {gameObject.name}", gameObject);
            enabled = false;
            return;
        }

        if (_numpadPanel == null)
        {
            Debug.LogError($"[NumpadOpener] Не назначен InputPanel (нумпад) на объекте {gameObject.name}", gameObject);
            enabled = false;
            return;
        }

        _targetInputField.onSelect.AddListener(OpenNumpad);
    }

    private void OnDestroy()
    {
        if (_targetInputField != null)
            _targetInputField.onSelect.RemoveListener(OpenNumpad);
    }

    private void OpenNumpad(string _)
    {
        _numpadPanel.ShowForField(
            targetFieldSetter: (value) => _targetInputField.text = value,
            initialValue: _targetInputField.text
        );
    }

    public void OpenNumpadManually() => OpenNumpad(string.Empty);
}
