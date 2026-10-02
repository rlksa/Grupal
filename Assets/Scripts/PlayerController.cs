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

    [Header("Conexión con Inventario y UI")]
    public EcoBotInventario inventario;
    public GameObject[] corazonesUI;   // Arrastra aquí los 3 corazones
    public GameObject panelInventarioUI; // Arrastra aquí el panel que contiene los 3 slots de abajo
    public KeyCode teclaInventario = KeyCode.I; // Tecla para abrir y cerrar el inventario

    private Animator anim;
    private bool estaMuerto = false;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();

        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;

        if (inventario == null)
            inventario = GetComponent<EcoBotInventario>();

        // Si las vidas están en 0 al iniciar, le asigna tantas vidas como corazones asignaste
        if (inventario != null && inventario.vidasActuales <= 0)
        {
            inventario.vidasActuales = corazonesUI.Length > 0 ? corazonesUI.Length : 3;
        }

        ActualizarCorazonesUI();

        // El inventario inicia oculto
        if (panelInventarioUI != null)
        {
            panelInventarioUI.SetActive(false);
        }
    }

    void Update()
    {
        if (estaMuerto || controller == null) return;

        // Abrir / Cerrar el inventario
        if (Input.GetKeyDown(teclaInventario))
        {
            ToggleInventario();
        }

        // Teclas de prueba (L = Recibir Daño, K = Morir al instante)
        if (Input.GetKeyDown(KeyCode.K))
        {
            Morir();
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            RecibirDano(1);
        }

        // Movimiento y Física
        if (controller.isGrounded && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
            if (anim != null) anim.SetBool("isJumping", false);
        }

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocidadVertical.y = Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
            if (anim != null) anim.SetBool("isJumping", true);
        }

        if (Input.GetKeyDown(KeyCode.E) && anim != null)
        {
            anim.SetTrigger("pickup");
        }

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direccion = new Vector3(horizontal, 0f, vertical).normalized;

        bool estaMoviendose = direccion.magnitude >= 0.1f;
        bool estaCorriendo = estaMoviendose && Input.GetKey(KeyCode.LeftShift);

        if (anim != null)
        {
            anim.SetBool("isWalking", estaMoviendose);
            anim.SetBool("isRunning", estaCorriendo);
        }

        if (estaMoviendose && camara != null)
        {
            float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg + camara.eulerAngles.y;
            float angulo = Mathf.SmoothDampAngle(transform.eulerAngles.y, anguloObjetivo, ref velocidadRotacionDeseada, tiempoSuavizadoRotacion);

            transform.rotation = Quaternion.Euler(0f, angulo, 0f);

            float velocidadActual = estaCorriendo ? velocidadCorrer : velocidadCaminar;
            Vector3 direccionMovimiento = Quaternion.Euler(0f, anguloObjetivo, 0f) * Vector3.forward;
            controller.Move(direccionMovimiento.normalized * velocidadActual * Time.deltaTime);
        }
    }

    // Método para abrir y cerrar el inventario al pulsar la tecla o clic en un botón de mochila
    public void ToggleInventario()
    {
        if (panelInventarioUI != null)
        {
            bool estadoActual = panelInventarioUI.activeSelf;
            panelInventarioUI.SetActive(!estadoActual);
        }
    }

    public void RecibirDano(int cantidad)
    {
        if (estaMuerto) return;

        if (inventario != null)
        {
            inventario.vidasActuales -= cantidad;
            if (inventario.vidasActuales < 0) inventario.vidasActuales = 0;

            if (inventario.barraVidaUI != null)
            {
                inventario.barraVidaUI.value = inventario.vidasActuales;
            }
        }

        // Actualiza las imágenes de la pantalla
        ActualizarCorazonesUI();

        if (inventario != null && inventario.vidasActuales <= 0)
        {
            Morir();
        }
    }

    // Método seguro que verifica que los corazones no hayan sido eliminados
    public void ActualizarCorazonesUI()
    {
        if (corazonesUI == null || corazonesUI.Length == 0) return;

        int vidas = (inventario != null) ? inventario.vidasActuales : 0;

        for (int i = 0; i < corazonesUI.Length; i++)
        {
            // La validación '!Equals(null)' evita el error de MissingReferenceException
            if (corazonesUI[i] != null && !corazonesUI[i].Equals(null))
            {
                corazonesUI[i].SetActive(i < vidas);
            }
        }
    }

    public void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;

        if (inventario != null)
        {
            inventario.vidasActuales = 0;
            if (inventario.barraVidaUI != null)
            {
                inventario.barraVidaUI.value = 0;
            }
        }

        // Apaga TODOS los corazones al morir
        ActualizarCorazonesUI();

        if (anim != null)
        {
            anim.SetTrigger("die");
        }
    }
}