using UnityEngine;
using UnityEngine.UI;

public class PanelSwitcher : MonoBehaviour
{
    [Header("Кнопки навигации (строго в порядке соответствия панелям)")]
    [SerializeField] private Button[] navigationButtons;

    [Header("Контентные панели (строго в порядке соответствия кнопкам)")]
    [SerializeField] private GameObject[] contentPanels;

    private int activeIndex = -1;

    private void Awake()
    {
        if (navigationButtons.Length != contentPanels.Length)
        {
            Debug.LogError("Ошибка: количество кнопок и панелей должно совпадать.");
            return;
        }

        for (int i = 0; i < navigationButtons.Length; i++)
        {
            int buttonIndex = i;
            navigationButtons[i].onClick.AddListener(() => ActivatePanel(buttonIndex));
        }

        ActivatePanel(0);
    }

    public void ActivatePanel(int index)
    {
        if (activeIndex == index) return;

        for (int i = 0; i < contentPanels.Length; i++)
        {
            contentPanels[i].SetActive(false);
        }

        contentPanels[index].SetActive(true);
        activeIndex = index;

        for (int i = 0; i < navigationButtons.Length; i++)
        {
            navigationButtons[i].interactable = (i != index);
        }
    }
}