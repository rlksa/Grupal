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

                // Suma puntos (+50) y suena el audio
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
                // Aviso rojo ¡INCORRECTO! -50
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

            if (HUDJuego.Instance != null)
            {
                // Cartelito fijo mientras estés cerca de la caneca
                HUDJuego.Instance.MostrarPrompt("E", "Depositar en: " + nombreCaneca);
            }
            else if (inventarioJugador != null)
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

            if (HUDJuego.Instance != null) HUDJuego.Instance.OcultarPrompt();
        }
    }
}
