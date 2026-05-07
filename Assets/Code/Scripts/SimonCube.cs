using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class SimonCube : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip audioClip;

    private Renderer cachedRenderer;
    private Material runtimeMaterial;
    private AudioSource cachedAudioSource;
    private Coroutine highlightRoutine;

    private Color baseColor;
    private Color baseEmissionColor;
    private bool hasEmission;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        runtimeMaterial = cachedRenderer.material;

        cachedAudioSource = GetComponent<AudioSource>();
        if (cachedAudioSource == null)
        {
            cachedAudioSource = gameObject.AddComponent<AudioSource>();
        }

        cachedAudioSource.playOnAwake = false;
        cachedAudioSource.loop = false;

        SetupStandardFadeMode(runtimeMaterial);

        if (runtimeMaterial.HasProperty("_Color"))
        {
            baseColor = runtimeMaterial.color;
        }
        else
        {
            baseColor = Color.white;
        }

        hasEmission = runtimeMaterial.HasProperty("_EmissionColor");
        if (hasEmission)
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            baseEmissionColor = runtimeMaterial.GetColor("_EmissionColor");
        }
    }

    public void Highlight()
    {
        if (!isActiveAndEnabled || runtimeMaterial == null)
        {
            return;
        }

        if (highlightRoutine != null)
        {
            StopCoroutine(highlightRoutine);
        }

        PlayAudio();

        highlightRoutine = StartCoroutine(HighlightRoutine());
    }

    public void Flash()
    {
        Highlight();
    }

    private IEnumerator HighlightRoutine()
    {
        const float duration = 0.35f;

        Color lighterColor = Color.Lerp(baseColor, Color.white, 0.4f);

        yield return AnimateHighlightPhase(baseColor, lighterColor, 1f, 1.15f, duration);

        yield return AnimateHighlightPhase(lighterColor, baseColor, 1.15f, 1f, duration);

        ApplyVisual(baseColor, 1f);

        highlightRoutine = null;
    }

    private IEnumerator AnimateHighlightPhase(Color startColor, Color endColor, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            Color currentColor = Color.Lerp(startColor, endColor, t);
            float currentAlpha = Mathf.Clamp01(Mathf.Lerp(startAlpha, endAlpha, t));

            ApplyVisual(currentColor, currentAlpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyVisual(endColor, Mathf.Clamp01(endAlpha));
    }

    private void ApplyVisual(Color color, float alpha)
    {
        Color finalColor = color;
        finalColor.a = alpha;

        if (runtimeMaterial.HasProperty("_Color"))
        {
            runtimeMaterial.color = finalColor;
        }

        if (hasEmission)
        {
            float emissionIntensity = Mathf.Lerp(1f, 1.15f, Mathf.InverseLerp(1f, 1.15f, Mathf.Max(1f, alpha)));
            runtimeMaterial.SetColor("_EmissionColor", baseEmissionColor * emissionIntensity);
        }
    }

    private void PlayAudio()
    {
        if (audioClip == null || cachedAudioSource == null)
        {
            return;
        }

        cachedAudioSource.PlayOneShot(audioClip);
    }

    private void OnMouseEnter()
    {
        if (SimonSays.Instance != null && SimonSays.Instance.IsPlayerTurn)
        {
            Highlight();
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"[SimonCube] Click/Tap detected on: {gameObject.name}");

        if (SimonSays.Instance != null)
        {
            Debug.Log($"[SimonCube] Forwarding to SimonSays. IsPlayerTurn={SimonSays.Instance.IsPlayerTurn}");
            SimonSays.Instance.OnCubeClicked(this);
        }
        else
        {
            Debug.LogWarning("[SimonCube] SimonSays.Instance is null.");
        }
    }

    private void SetupStandardFadeMode(Material material)
    {
        if (material == null || !material.HasProperty("_Mode"))
        {
            return;
        }

        material.SetFloat("_Mode", 2f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}