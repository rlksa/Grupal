using System.Collections;
using UnityEngine;

/// Animación de entrada para un panel: fade + "pop" + botones que aparecen uno a uno.
/// Usa tiempo SIN escala (unscaled), así funciona aunque el juego esté en pausa (Time.timeScale = 0).
[RequireComponent(typeof(CanvasGroup))]
public class EcoUIPanel : MonoBehaviour
{
    public float duracion = 0.45f;
    public float escalaInicial = 0.88f;
    public RectTransform contenido;                 // lo que "salta" (si es null usa este objeto)
    public RectTransform[] elementosEscalonados;    // título, botones... aparecen en cascada
    public float retardoEntreElementos = 0.12f;
    public RectTransform titulo;                    // pulso suave (opcional)
    public float pulsoTitulo = 0.025f;

    CanvasGroup group;
    Coroutine rutina;

    void Awake() => group = GetComponent<CanvasGroup>();

    void OnEnable()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Entrada());
    }

    IEnumerator Entrada()
    {
        RectTransform c = contenido != null ? contenido : (RectTransform)transform;
        group.alpha = 0f;
        group.interactable = false;
        c.localScale = Vector3.one * escalaInicial;

        if (elementosEscalonados != null)
            foreach (var e in elementosEscalonados) if (e) e.localScale = Vector3.zero;

        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / duracion)
        {
            float k = EaseOutBack(Mathf.Clamp01(t));
            group.alpha = Mathf.Clamp01(t * 1.4f);
            c.localScale = Vector3.LerpUnclamped(Vector3.one * escalaInicial, Vector3.one, k);
            yield return null;
        }
        group.alpha = 1f;
        c.localScale = Vector3.one;

        if (elementosEscalonados != null)
        {
            foreach (var e in elementosEscalonados)
            {
                if (e) StartCoroutine(PopElemento(e));
                yield return new WaitForSecondsRealtime(retardoEntreElementos);
            }
        }
        group.interactable = true;

        // Pulso suave del título mientras el panel esté abierto
        while (titulo != null)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * pulsoTitulo;
            titulo.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    IEnumerator PopElemento(RectTransform e)
    {
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
        {
            if (e == null) yield break;
            e.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(t));
            yield return null;
        }
        if (e) e.localScale = Vector3.one;
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
}
