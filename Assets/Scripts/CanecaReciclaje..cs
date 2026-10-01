using UnityEngine;

public class CanecaReciclaje : MonoBehaviour
{
    [Header("Configuración de la Caneca")]
    public string tipoPermitido = "Organico";

    private bool jugadorCerca = false;
    private EcoBotInventario inventarioJugador;

    private void Update()
    {
        // Solo deposita si el jugador está junto a la caneca y presiona E
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            DepositarEnCaneca();
        }
    }

    private void DepositarEnCaneca()
    {
        if (inventarioJugador != null)
        {
            string tipoLimpio = tipoPermitido.Trim();

            if (inventarioJugador.TieneResiduo(tipoLimpio))
            {
                inventarioJugador.DepositarResiduo(tipoLimpio);
                inventarioJugador.MostrarFeedback("¡CORRECTO!", Color.green);
            }
            else
            {
                inventarioJugador.MostrarFeedback("¡INCORRECTO!", Color.red);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            inventarioJugador = other.GetComponent<EcoBotInventario>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            inventarioJugador = null;
        }
    }
}