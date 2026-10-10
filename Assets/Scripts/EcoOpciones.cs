using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Opciones: volumen, calidad gráfica (con flechas) y pantalla completa. Se guarda en PlayerPrefs.
/// Va en el Canvas (siempre activo) para aplicar el volumen guardado apenas abre el menú.
public class EcoOpciones : MonoBehaviour
{
    public Slider volumen;
    public TMP_Text textoVolumen;
    public TMP_Text textoCalidad;
    public Toggle pantallaCompleta;

    void Start()
    {
        float v = PlayerPrefs.GetFloat("EcoVolumen", 1f);
        AudioListener.volume = v;

        if (volumen)
        {
            volumen.SetValueWithoutNotify(v);
            volumen.onValueChanged.AddListener(SetVolumen);
        }
        ActualizarTextoVolumen(v);

        int q = PlayerPrefs.GetInt("EcoCalidad", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(Mathf.Clamp(q, 0, QualitySettings.names.Length - 1), true);
        ActualizarTextoCalidad();

        if (pantallaCompleta)
        {
            pantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);
            pantallaCompleta.onValueChanged.AddListener(f => Screen.fullScreen = f);
        }
    }

    void SetVolumen(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("EcoVolumen", v);
        ActualizarTextoVolumen(v);
    }

    public void CalidadMas() => CambiarCalidad(+1);
    public void CalidadMenos() => CambiarCalidad(-1);

    void CambiarCalidad(int d)
    {
        int n = QualitySettings.names.Length;
        int q = (QualitySettings.GetQualityLevel() + d + n) % n;
        QualitySettings.SetQualityLevel(q, true);
        PlayerPrefs.SetInt("EcoCalidad", q);
        ActualizarTextoCalidad();
    }

    void ActualizarTextoVolumen(float v) { if (textoVolumen) textoVolumen.text = Mathf.RoundToInt(v * 100f) + "%"; }
    void ActualizarTextoCalidad() { if (textoCalidad) textoCalidad.text = QualitySettings.names[QualitySettings.GetQualityLevel()].ToUpper(); }
}
