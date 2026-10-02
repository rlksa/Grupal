using UnityEngine;

public class CanecaReciclaje : MonoBehaviour
{
    [Header("Configuración de la Caneca")]
    public string nombreCaneca = "Caneca de Orgánicos";
    public string tipoPermitido = "Organico";

    private bool jugadorCerca = false;
    private EcoBotInventario inventarioJugador;

    private void Update()
    {
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

            // Caso 1: Tiene el residuo correcto para esta caneca
            if (inventarioJugador.TieneResiduo(tipoLimpio))
            {
                inventarioJugador.DepositarResiduo(tipoLimpio);

                // Dispara el aviso verde ¡CORRECTO! +10 y actualiza puntos en UI
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.DepositarResiduo(true);
                }
            }
            // Caso 2: Intenta depositar pero no lleva ningún residuo
            else if (!inventarioJugador.TieneAlgunResiduo())
            {
                inventarioJugador.MostrarFeedback("¡NO TIENES RESIDUOS!", Color.yellow);
            }
            // Caso 3: Lleva residuos pero ninguno corresponde a esta caneca
            else
            {
                // Dispara el aviso rojo ¡INCORRECTO! -50 y resta puntos en UI
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.DepositarResiduo(false);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            inventarioJugador = other.GetComponent<EcoBotInventario>();

            if (inventarioJugador != null)
            {
                inventarioJugador.MostrarFeedback("[" + nombreCaneca + "] Presiona E para depositar", Color.white);
            }
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