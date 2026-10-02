using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class ItemResiduo
{
    public string nombreMostrado; // Ej: "Plátano"
    public string tipoCategoria;  // Ej: "Organico"

    public ItemResiduo(string nombre, string categoria)
    {
        nombreMostrado = nombre;
        tipoCategoria = categoria;
    }
}

public class EcoBotInventario : MonoBehaviour
{
    [Header("Salud y UI")]
    public int vidasMaximas = 3;
    public int vidasActuales = 3;
    public Slider barraVidaUI;
    private bool esInmunidad = false;

    [Header("Referencias UI TextMeshPro")]
    public TextMeshProUGUI textoFeedbackUI;
    public TextMeshProUGUI textoPuntos;

    [Header("Slots del Inventario (Máx 3)")]
    public TextMeshProUGUI textoSlot1;
    public TextMeshProUGUI textoSlot2;
    public TextMeshProUGUI textoSlot3;

    [Header("Puntuación e Inventario")]
    public int puntosTotales = 0;
    public int limiteInventario = 3;

    // Guardará objetos con Nombre + Categoría
    public List<ItemResiduo> listaInventario = new List<ItemResiduo>();

    private Coroutine corrutinaFeedback;

    private void Start()
    {
        vidasActuales = vidasMaximas;

        if (barraVidaUI != null)
        {
            barraVidaUI.maxValue = vidasMaximas;
            barraVidaUI.value = vidasActuales;
        }

        ActualizarHUD();
    }

    // Agregar basura respetando el límite de 3 espacios
    public bool AgregarResiduo(string nombre, string categoria, int puntos)
    {
        if (listaInventario.Count >= limiteInventario)
        {
            MostrarFeedback("¡INVENTARIO LLENO! (3/3)", Color.red);
            return false;
        }

        listaInventario.Add(new ItemResiduo(nombre, categoria.Trim()));
        puntosTotales += puntos;
        MostrarFeedback("+ " + nombre.ToUpper() + " (" + puntos + " PTS)", Color.yellow);
        ActualizarHUD();
        return true;
    }

    // Comprueba si lleva un tipo de residuo específico por categoría
    public bool TieneResiduo(string categoria)
    {
        return listaInventario.Exists(item => item.tipoCategoria.Equals(categoria.Trim(), System.StringComparison.OrdinalIgnoreCase));
    }

    // Devuelve true si el inventario tiene AL MENOS 1 objeto
    public bool TieneAlgunResiduo()
    {
        return listaInventario.Count > 0;
    }

    // Depositar SOLO UN residuo correcto (+50 pts)
    public bool DepositarResiduo(string categoria)
    {
        string catLimpia = categoria.Trim();
        int index = listaInventario.FindIndex(item => item.tipoCategoria.Equals(catLimpia, System.StringComparison.OrdinalIgnoreCase));

        if (index != -1)
        {
            string nombreEntregado = listaInventario[index].nombreMostrado;
            listaInventario.RemoveAt(index);
            puntosTotales += 50;
            MostrarFeedback("¡ENTREGADO: " + nombreEntregado.ToUpper() + "! (+50 PTS)", Color.green);
            ActualizarHUD();
            return true;
        }
        return false;
    }

    // Penalización por tirar en tacho equivocado (-20 pts)
    public void RestarPuntos(int cantidad)
    {
        puntosTotales -= cantidad;
        if (puntosTotales < 0) puntosTotales = 0;
        MostrarFeedback("¡INCORRECTO! (-" + cantidad + " PTS)", Color.red);
        ActualizarHUD();
    }

    // Control de daño del robot
    public void RecibirDano(int cantidad)
    {
        if (esInmunidad) return;

        vidasActuales -= cantidad;
        if (vidasActuales < 0) vidasActuales = 0;

        if (barraVidaUI != null)
        {
            barraVidaUI.value = vidasActuales;
        }

        MostrarFeedback("-1 VIDA", Color.red);
        ActualizarHUD();

        if (vidasActuales <= 0)
        {
            MostrarFeedback("¡GAME OVER!", Color.red);
        }
        else
        {
            StartCoroutine(TiempoInmunidad());
        }
    }

    private IEnumerator TiempoInmunidad()
    {
        esInmunidad = true;
        yield return new WaitForSeconds(2.0f);
        esInmunidad = false;
    }

    // Actualiza los textos de los 3 slots con el Nombre del ítem
    public void ActualizarHUD()
    {
        if (textoPuntos != null)
        {
            textoPuntos.text = "Puntos: " + puntosTotales;
        }

        if (textoSlot1 != null) textoSlot1.text = listaInventario.Count > 0 ? listaInventario[0].nombreMostrado : "";
        if (textoSlot2 != null) textoSlot2.text = listaInventario.Count > 1 ? listaInventario[1].nombreMostrado : "";
        if (textoSlot3 != null) textoSlot3.text = listaInventario.Count > 2 ? listaInventario[2].nombreMostrado : "";
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

        yield return new WaitForSeconds(1.8f);

        textoFeedbackUI.text = "";
    }
}