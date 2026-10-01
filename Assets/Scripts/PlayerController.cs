using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;
    public Transform camara;

    [Header("Ajustes de Movimiento")]
    public float velocidadCaminar = 6f;
    public float velocidadCorrer = 10f;
    public float tiempoSuavizadoRotacion = 0.1f;
    private float velocidadRotacionDeseada;

    [Header("Salto y Gravedad")]
    public float fuerzaSalto = 1.5f;
    public float gravedad = -19.62f;
    private Vector3 velocidadVertical;

    private Animator anim;
    private bool estaMuerto = false;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Si el robot muere, se bloquean todos los controles
        if (estaMuerto) return;

        // 1. Tecla de prueba para Morir (Tecla K)
        if (Input.GetKeyDown(KeyCode.K))
        {
            Morir();
            return;
        }

        // 2. Detección de Suelo
        if (controller.isGrounded && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
            if (anim != null) anim.SetBool("isJumping", false);
        }

        // 3. Salto (Tecla Espacio)
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocidadVertical.y = Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
            if (anim != null) anim.SetBool("isJumping", true);
        }

        // 4. Recoger / Agacharse (Tecla E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (anim != null) anim.SetTrigger("pickup");
        }

        // 5. Gravedad
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        // 6. Movimiento WASD / Flechas
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direccion = new Vector3(horizontal, 0f, vertical).normalized;

        bool estaMoviendose = direccion.magnitude >= 0.1f;
        bool estaCorriendo = estaMoviendose && Input.GetKey(KeyCode.LeftShift);

        // 7. Enviar estados al Animator
        if (anim != null)
        {
            anim.SetBool("isWalking", estaMoviendose);
            anim.SetBool("isRunning", estaCorriendo);
        }

        // 8. Aplicar Desplazamiento
        if (estaMoviendose)
        {
            float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg + camara.eulerAngles.y;
            float angulo = Mathf.SmoothDampAngle(transform.eulerAngles.y, anguloObjetivo, ref velocidadRotacionDeseada, tiempoSuavizadoRotacion);

            transform.rotation = Quaternion.Euler(0f, angulo, 0f);

            float velocidadActual = estaCorriendo ? velocidadCorrer : velocidadCaminar;
            Vector3 direccionMovimiento = Quaternion.Euler(0f, anguloObjetivo, 0f) * Vector3.forward;
            controller.Move(direccionMovimiento.normalized * velocidadActual * Time.deltaTime);
        }
    }

    // Método para activar la muerte desde cualquier evento o enemigo
    public void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;
        if (anim != null)
        {
            anim.SetTrigger("die");
        }
    }
}