using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text traitLabel;
    [SerializeField] private Slider valueSlider;

    [Header("Default")]
    [SerializeField] [Range(0f, 10f)] private float defaultValue = 5f;

    public float SelectedValue => valueSlider != null ? valueSlider.value : defaultValue;

    private void Awake()
    {
        if (valueSlider != null)
        {
            valueSlider.minValue = 0f;
            valueSlider.maxValue = 10f;
            valueSlider.wholeNumbers = false;
            valueSlider.value = Mathf.Clamp(defaultValue, 0f, 10f);
        }
    }

    public void SetLabel(string newLabel)
    {
        if (traitLabel != null)
            traitLabel.text = newLabel;
    }

    public void SetValue(float value)
    {
        if (valueSlider != null)
            valueSlider.value = Mathf.Clamp(value, 0f, 10f);
    }
}