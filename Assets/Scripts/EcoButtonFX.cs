using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Efecto hover/click para botones: crece, se ilumina el borde/fondo/texto. Funciona con el juego en pausa.
[RequireComponent(typeof(Button))]
public class EcoButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public float escalaHover = 1.07f;
    public float escalaPresionado = 0.96f;
    public float velocidad = 14f;

    [Header("Colores (normal -> hover)")]
    public Graphic borde;  public Color bordeNormal = Color.white;  public Color bordeHover = Color.white;
    public Graphic fondo;  public Color fondoNormal = Color.black;  public Color fondoHover = Color.gray;
    public Graphic texto;  public Color textoNormal = Color.white;  public Color textoHover = Color.white;

    Vector3 baseScale = Vector3.one, destino = Vector3.one;
    bool hover;

    void Awake() { baseScale = destino = transform.localScale; }
    void OnEnable() { transform.localScale = baseScale; destino = baseScale; hover = false; Aplicar(1f); }

    void Update()
    {
        float k = Mathf.Clamp01(velocidad * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, destino, k);
        Aplicar(k);
    }

    void Aplicar(float k)
    {
        if (borde) borde.color = Color.Lerp(borde.color, hover ? bordeHover : bordeNormal, k);
        if (fondo) fondo.color = Color.Lerp(fondo.color, hover ? fondoHover : fondoNormal, k);
        if (texto) texto.color = Color.Lerp(texto.color, hover ? textoHover : textoNormal, k);
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; destino = baseScale * escalaHover; }
    public void OnPointerExit(PointerEventData e) { hover = false; destino = baseScale; }
    public void OnPointerDown(PointerEventData e) { destino = baseScale * escalaPresionado; }
    public void OnPointerUp(PointerEventData e) { destino = hover ? baseScale * escalaHover : baseScale; }
    public void OnSelect(BaseEventData e) { hover = true; destino = baseScale * escalaHover; }
    public void OnDeselect(BaseEventData e) { hover = false; destino = baseScale; }
}
