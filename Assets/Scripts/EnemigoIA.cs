using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EnemigoIA : MonoBehaviour
{
    [Header("Configuración del Jugador")]
    public Transform jugador;
    public float rangoDeteccion = 8f; // Distancia a la que te empieza a seguir

    [Header("Ataque y Cooldown")]
    private float tiempoSiguienteAtaque = 0f;
    public float cooldownAtaque = 1.5f; // Espera 1.5s entre golpe y golpe

    private NavMeshAgent agent;
    private Animator anim;
    private bool juegoTerminado = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (juegoTerminado) return;

        if (jugador != null && agent != null && agent.isOnNavMesh)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);

            if (distancia <= rangoDeteccion)
            {
                agent.SetDestination(jugador.position);
                if (anim != null) anim.SetBool("isWalking", true);
            }
            else
            {
                agent.ResetPath();
                if (anim != null) anim.SetBool("isWalking", false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (juegoTerminado) return;

        // Detecta si choca con el jugador
        if (other.CompareTag("Player") && Time.time >= tiempoSiguienteAtaque)
        {
            tiempoSiguienteAtaque = Time.time + cooldownAtaque;

            // Busca el script PlayerController en el objeto o sus padres
            PlayerController playerScript = other.GetComponentInParent<PlayerController>();
            if (playerScript == null)
            {
                playerScript = other.GetComponent<PlayerController>();
            }

            if (playerScript != null)
            {
                // Le resta 1 corazón/vida al Player a través de su script
                playerScript.RecibirDano(1);

                // Si el jugador se quedó sin vidas tras este golpe
                if (playerScript.inventario != null && playerScript.inventario.vidasActuales <= 0)
                {
                    juegoTerminado = true;
                    StartCoroutine(ReiniciarPartida());
                }
            }
        }
    }

    private IEnumerator ReiniciarPartida()
    {
        // Detiene al enemigo
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        if (anim != null) anim.SetBool("isWalking", false);

        // Espera 2.5 segundos para la animación de muerte
        yield return new WaitForSeconds(2.5f);

        // Busca el GameManager para desplegar el PanelGameOver
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.RecibirDano(3); // Activa el Game Over y pausa el juego
        }
        else
        {
            // Respaldo por si no encuentra el GameManager
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}
