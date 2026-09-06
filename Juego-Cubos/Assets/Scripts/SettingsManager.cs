using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;

    private const string SensitivityKey = "MouseSensitivity";
    private const float DefaultSensitivity = 2f;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        float savedSensitivity =
            PlayerPrefs.GetFloat(
                SensitivityKey,
                DefaultSensitivity
            );

        sensitivitySlider.value =
            savedSensitivity;

        UpdateSensitivityDisplay(
            savedSensitivity
        );

        sensitivitySlider.onValueChanged.AddListener(
            SetSensitivity
        );
    }


    // =========================================================
    // CAMBIAR SENSIBILIDAD
    // =========================================================

    public void SetSensitivity(
        float value
    )
    {
        PlayerPrefs.SetFloat(
            SensitivityKey,
            value
        );

        PlayerPrefs.Save();

        UpdateSensitivityDisplay(
            value
        );
    }


    // =========================================================
    // ACTUALIZAR TEXTO
    // =========================================================

    private void UpdateSensitivityDisplay(
        float value
    )
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text =
                value.ToString("F1");
        }
    }
}