using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableButton : MonoBehaviour, IPointerClickHandler
{
    public string parameterName;
    public float delta;

    public void OnPointerClick(PointerEventData eventData)
    {
        TechProcessController controller = FindFirstObjectByType<TechProcessController>();

        if (controller != null)
        {
            controller.ChangeParameter(parameterName, delta);
        }
        else
        {
            Debug.LogError("Контроллер не найден! Добавьте TechProcessController на любой GameObject в сцене.");
        }
    }
}