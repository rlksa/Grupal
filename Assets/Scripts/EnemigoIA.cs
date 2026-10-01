using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemigoIA : MonoBehaviour
{
    [Header("Configuración del Jugador")]
    public Transform jugador;
    public float rangoDeteccion = 8f; // Distancia a la que te empieza a seguir

    [Header("Configuración del Sistema de Vidas")]
    public int vidas = 3;
    private float tiempoSiguienteAtaque = 0f;
    private float cooldownAtaque = 1.5f; // Espera 1.5s entre golpe y golpe

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (jugador != null && agent != null && agent.isOnNavMesh)
        {
            // Calcula la distancia entre el enemigo y el jugador
            float distancia = Vector3.Distance(transform.position, jugador.position);

            if (distancia <= rangoDeteccion)
            {
                // Si está dentro del rango, persigue al jugador
                agent.SetDestination(jugador.position);
            }
            else
            {
                // Si sale del rango, se detiene
                agent.ResetPath();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detecta si choca con el robot y si ya pasó el tiempo de espera
        if (other.CompareTag("Player") && Time.time >= tiempoSiguienteAtaque)
        {
            vidas--;
            tiempoSiguienteAtaque = Time.time + cooldownAtaque;

            Debug.Log("¡El enemigo te golpeó! Vidas restantes: " + vidas);

            if (vidas <= 0)
            {
                Debug.Log("¡Has perdido todas las vidas! Reiniciando...");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    // Dibuja una esfera roja en el editor para que veas el rango visualmente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}