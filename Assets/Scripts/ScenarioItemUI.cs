using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioItemUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Image buttonImage;

    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.6f, 1f, 0.6f);

    private ScenarioData _data;
    private ScenarioListManager _manager;

    public void Setup(ScenarioData data, ScenarioListManager manager)
    {
        _data = data;
        _manager = manager;

        titleText.text = (string)data.title;
        SetSelected(false);

        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            _manager.OnScenarioClicked(this, _data);
        });
    }

    public void SetSelected(bool isSelected)
    {
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedColor : normalColor;
        }
    }
}