using System.Collections;
using UnityEngine;
using TMPro;

public class EcoBotInventario : MonoBehaviour
{
    [Header("Salud del Jugador")]
    public int vidas = 3;
    private bool esInmunidad = false;

    [Header("Referencias UI (Asignar cuando esté lista la UI)")]
    public TextMeshProUGUI textoFeedbackUI; // Mensaje central (+10, ¡CORRECTO!, etc.)
    public TextMeshProUGUI textoPuntos;
    public TextMeshProUGUI textoVidas;
    public TextMeshProUGUI textoOrganico;
    public TextMeshProUGUI textoPlastico;
    public TextMeshProUGUI textoPapel;
    public TextMeshProUGUI textoVidrio;

    [Header("Puntuación e Inventario")]
    public int puntosTotales = 0;
    public int organicoCount = 0;
    public int plasticoCount = 0;
    public int papelCount = 0;
    public int vidrioCount = 0;

    private Coroutine corrutinaFeedback;

    private void Start()
    {
        ActualizarHUD();
    }

    public void AgregarResiduo(string tipo)
    {
        string t = tipo.Trim();
        if (t.Equals("Organico", System.StringComparison.OrdinalIgnoreCase)) organicoCount++;
        else if (t.Equals("Plastico", System.StringComparison.OrdinalIgnoreCase)) plasticoCount++;
        else if (t.Equals("Papel", System.StringComparison.OrdinalIgnoreCase)) papelCount++;
        else if (t.Equals("Vidrio", System.StringComparison.OrdinalIgnoreCase)) vidrioCount++;

        puntosTotales += 10;
        ActualizarHUD();
    }

    public bool TieneResiduo(string tipo)
    {
        string t = tipo.Trim();
        if (t.Equals("Organico", System.StringComparison.OrdinalIgnoreCase)) return organicoCount > 0;
        if (t.Equals("Plastico", System.StringComparison.OrdinalIgnoreCase)) return plasticoCount > 0;
        if (t.Equals("Papel", System.StringComparison.OrdinalIgnoreCase)) return papelCount > 0;
        if (t.Equals("Vidrio", System.StringComparison.OrdinalIgnoreCase)) return vidrioCount > 0;
        return false;
    }

    public void DepositarResiduo(string tipo)
    {
        string t = tipo.Trim();
        if (t.Equals("Organico", System.StringComparison.OrdinalIgnoreCase) && organicoCount > 0) organicoCount--;
        else if (t.Equals("Plastico", System.StringComparison.OrdinalIgnoreCase) && plasticoCount > 0) plasticoCount--;
        else if (t.Equals("Papel", System.StringComparison.OrdinalIgnoreCase) && papelCount > 0) papelCount--;
        else if (t.Equals("Vidrio", System.StringComparison.OrdinalIgnoreCase) && vidrioCount > 0) vidrioCount--;

        puntosTotales += 50;
        ActualizarHUD();
    }

    public void RecibirDano(int cantidad)
    {
        if (esInmunidad) return;

        vidas -= cantidad;
        MostrarFeedback("-1 VIDA", Color.red);
        ActualizarHUD();

        if (vidas <= 0)
        {
            MostrarFeedback("¡GAME OVER!", Color.red);
            // Aquí reiniciaremos el nivel o congelaremos al jugador
        }
        else
        {
            StartCoroutine(TiempoInmunidad());
        }
    }

    private IEnumerator TiempoInmunidad()
    {
        esInmunidad = true;
        yield return new WaitForSeconds(2.0f); // 2 segundos de gracia sin recibir daño
        esInmunidad = false;
    }

    public void ActualizarHUD()
    {
        if (textoPuntos != null) textoPuntos.text = puntosTotales.ToString();
        if (textoVidas != null) textoVidas.text = "Vidas: " + vidas;
        if (textoOrganico != null) textoOrganico.text = organicoCount.ToString();
        if (textoPlastico != null) textoPlastico.text = plasticoCount.ToString();
        if (textoPapel != null) textoPapel.text = papelCount.ToString();
        if (textoVidrio != null) textoVidrio.text = vidrioCount.ToString();
    }

    public void MostrarFeedback(string mensaje, Color colorTexto)
    {
        if (textoFeedbackUI == null) return;

        if (corrutinaFeedback != null)
            StopCoroutine(corrutinaFeedback);

        corrutinaFeedback = StartCoroutine(RutinaFeedback(mensaje, colorTexto));
    }

    private IEnumerator RutinaFeedback(string mensaje, Color colorTexto)
    {
        textoFeedbackUI.text = mensaje;
        textoFeedbackUI.color = colorTexto;
        textoFeedbackUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        textoFeedbackUI.text = "";
    }
}