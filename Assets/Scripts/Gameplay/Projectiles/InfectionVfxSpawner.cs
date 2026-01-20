using System.Collections.Generic;
using UnityEngine;

public class InfectionVfxSpawner : MonoBehaviour, IProjectileVfx
{
    [SerializeField] private GameObject infectionVfxPrefab;
    [SerializeField] private float infectionVfxDuration = 0.25f;
    [SerializeField] private float infectionVfxScaleMultiplier = 2f;
    [SerializeField] private int prewarmCount = 8;
    [SerializeField] private bool allowExpand = true;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private ProjectileConfig config;
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();

    private void Awake()
    {
        ApplyTuning();
        InitializePool();
    }

    public void PlayInfection(Vector3 center, float radius)
    {
        var scale = radius * infectionVfxScaleMultiplier;
        if (scale <= 0f)
            return;

        if (infectionVfxPrefab == null)
            return;

        var vfxInstance = GetInstance();
        if (vfxInstance == null)
            return;

        vfxInstance.transform.SetParent(null, false);
        vfxInstance.transform.SetPositionAndRotation(center, Quaternion.identity);
        vfxInstance.transform.localScale = Vector3.one * scale;
        vfxInstance.SetActive(true);
        StartCoroutine(ReturnAfter(vfxInstance, Mathf.Max(0.05f, infectionVfxDuration)));
    }

    private void InitializePool()
    {
        if (poolRoot == null)
            poolRoot = transform;

        for (var i = 0; i < Mathf.Max(0, prewarmCount); i++)
        {
            var instance = CreateInstance();
            if (instance == null)
                break;

            ReturnInstance(instance);
        }

    }

    private GameObject GetInstance()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        if (!allowExpand)
            return null;

        return CreateInstance();
    }

    private GameObject CreateInstance()
    {
        if (infectionVfxPrefab == null)
            return null;

        var instance = Instantiate(infectionVfxPrefab, poolRoot);
        instance.SetActive(false);
        return instance;
    }


    private System.Collections.IEnumerator ReturnAfter(GameObject instance, float duration)
    {
        yield return new WaitForSeconds(duration);
        ReturnInstance(instance);
    }

    private void ReturnInstance(GameObject instance)
    {
        if (instance == null)
            return;

        instance.SetActive(false);
        instance.transform.SetParent(poolRoot, false);
        _pool.Enqueue(instance);
    }

    private void ApplyTuning()
    {
        if (config == null)
            return;

        infectionVfxDuration = config.infectionVfxDuration;
        infectionVfxScaleMultiplier = config.infectionVfxScaleMultiplier;
    }
}
