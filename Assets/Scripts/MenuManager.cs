using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    public string nombreEscenaJuego = "Demo_Scene_1";

    [Header("UI Paneles")]
    public GameObject panelOpciones;

    private void Start()
    {
        // Reproduce la música de fondo del menú principal si está asignada
        if (SoundManager.Instance != null && SoundManager.Instance.musicaMenu != null)
        {
            SoundManager.Instance.CambiarMusica(SoundManager.Instance.musicaMenu);
        }
    }

    public void CargarJuego()
    {
        ReproducirSonidoBoton();
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void AbrirOpciones()
    {
        ReproducirSonidoBoton();
        if (panelOpciones != null)
            panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        ReproducirSonidoBoton();
        if (panelOpciones != null)
            panelOpciones.SetActive(false);
    }

    public void SalirDelJuego()
    {
        ReproducirSonidoBoton();
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ReproducirSonidoBoton()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ReproducirBoton();
        }
    }
}