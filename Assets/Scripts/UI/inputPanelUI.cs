using UnityEngine;
using TMPro;

public class inputPanelUI : MonoBehaviour
{
    [Header("Обязательные ссылки")]
    [SerializeField] private TMP_InputField _targetInputField;
    [SerializeField] private InputPanel _numpadPanel;

    private void Reset()
    {
        // Автоматически найдёт InputField на том же объекте при добавлении компонента
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

        // Подписываемся на событие выбора поля
        _targetInputField.onSelect.AddListener(OpenNumpad);
    }

    private void OnDestroy()
    {
        // Обязательно отписываемся, чтобы избежать утечек памяти
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

    // Публичный метод для программного открытия нумпада для этого поля
    public void OpenNumpadManually() => OpenNumpad(string.Empty);
}
