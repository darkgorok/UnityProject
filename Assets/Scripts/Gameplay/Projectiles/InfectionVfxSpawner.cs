using UnityEngine;

public class InfectionVfxSpawner : MonoBehaviour, IProjectileVfx
{
    [SerializeField] private GameObject infectionVfxPrefab;
    [SerializeField] private float infectionVfxDuration = 0.25f;
    [SerializeField] private float infectionVfxScaleMultiplier = 2f;
    [SerializeField] private ProjectileConfig config;

    private void Awake()
    {
        ApplyTuning();
    }

    public void PlayInfection(Vector3 center, float radius)
    {
        var scale = radius * infectionVfxScaleMultiplier;
        if (scale <= 0f)
            return;

        if (infectionVfxPrefab == null)
            return;

        var vfxInstance = Instantiate(infectionVfxPrefab, center, Quaternion.identity);

        vfxInstance.transform.localScale = Vector3.one * scale;
        Destroy(vfxInstance, Mathf.Max(0.05f, infectionVfxDuration));
    }

    private void ApplyTuning()
    {
        if (config == null)
            return;

        infectionVfxDuration = config.infectionVfxDuration;
        infectionVfxScaleMultiplier = config.infectionVfxScaleMultiplier;
    }
}
