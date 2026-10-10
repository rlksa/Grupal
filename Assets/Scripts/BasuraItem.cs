using UnityEngine;

public class BasuraItem : MonoBehaviour
{
    [Header("Configuración del Residuo")]
    public string nombreItem = "Plátano";
    public string tipoResiduo = "Organico";
    public int puntosPorRecoger = 10;

    private bool jugadorCerca = false;
    private EcoBotInventario inventarioJugador;

    private void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            RecogerBasura();
        }
    }

    private void RecogerBasura()
    {
        if (inventarioJugador != null)
        {
            bool exito = inventarioJugador.AgregarResiduo(nombreItem, tipoResiduo, puntosPorRecoger);

            if (exito)
            {
                // Avisa al GameManager para que sume puntos y haga sonar el audio
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RecogerBasura();
                }

                Animator anim = inventarioJugador.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("pickup");
                }

                // Ya no hay nada que recoger: esconde el cartelito
                if (HUDJuego.Instance != null) HUDJuego.Instance.OcultarPrompt();

                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            inventarioJugador = other.GetComponent<EcoBotInventario>();

            // Cartelito con el nombre del objeto
            if (HUDJuego.Instance != null)
            {
                HUDJuego.Instance.MostrarPrompt("E", "Recoger: " + nombreItem);
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
