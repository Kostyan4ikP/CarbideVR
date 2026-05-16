using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRButtonHandler : MonoBehaviour
{
    [Header("Настройки кнопки")]
    public string parameterName;
    public float delta;

    private TechProcessController controller;

    void Start()
    {
        controller = FindFirstObjectByType<TechProcessController>();

        if (controller == null)
        {
            Debug.LogError("TechProcessController не найден на сцене!");
            return;
        }

        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();

        if (interactable != null)
        {
            interactable.selectEntered.AddListener(_ => OnButtonPressed());
            Debug.Log($"VRButtonHandler инициализирован для {parameterName}");
        }
        else
        {
            Debug.LogError($"На кнопке {gameObject.name} нет компонента XRSimpleInteractable!");
        }
    }

    void OnButtonPressed()
    {
        if (controller != null)
        {
            controller.ChangeParameter(parameterName, delta);
            Debug.Log($"VR нажатие: {parameterName} + {delta}");
        }
    }

    void OnDestroy()
    {
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(_ => OnButtonPressed());
        }
    }
}