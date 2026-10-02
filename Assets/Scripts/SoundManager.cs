using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource audioSourceSFX;
    public AudioSource audioSourceMusica;

    [Header("Clips de Sonido (SFX)")]
    public AudioClip sonidoGolpe;
    public AudioClip sonidoGameOver;
    public AudioClip sonidoBoton;
    public AudioClip sonidoAcierto;   // Sonido al depositar bien (+10)
    public AudioClip sonidoError;     // Sonido al equivocarse (-50)

    [Header("Música de Fondo")]
    public AudioClip musicaMenu;
    public AudioClip musicaJuego;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReproducirSonido(AudioClip clip)
    {
        if (clip != null && audioSourceSFX != null)
        {
            audioSourceSFX.PlayOneShot(clip);
        }
    }

    public void ReproducirBoton()
    {
        ReproducirSonido(sonidoBoton);
    }

    public void CambiarMusica(AudioClip nuevaMusica)
    {
        if (nuevaMusica == null || audioSourceMusica == null) return;
        if (audioSourceMusica.clip == nuevaMusica && audioSourceMusica.isPlaying) return;

        audioSourceMusica.Stop();
        audioSourceMusica.clip = nuevaMusica;
        audioSourceMusica.loop = true;
        audioSourceMusica.Play();
    }
}