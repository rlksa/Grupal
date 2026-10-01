using UnityEngine;

public class Basuraltem : MonoBehaviour
{
    [Header("Configuración del Residuo")]
    public string tipoResiduo = "Organico";
    public int puntosPorRecoger = 10;

    private bool jugadorCerca = false;
    private EcoBotInventario inventarioJugador;

    private void Update()
    {
        // Si el jugador está cerca y presiona la tecla E
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            RecogerBasura();
        }
    }

    private void RecogerBasura()
    {
        if (inventarioJugador != null)
        {
            inventarioJugador.AgregarResiduo(tipoResiduo);
            inventarioJugador.MostrarFeedback("+" + puntosPorRecoger + " PUNTOS", Color.yellow);
            Destroy(gameObject);
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