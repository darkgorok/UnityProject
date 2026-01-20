using UnityEngine;
using DG.Tweening;

public class PlayerSquashAnimator : MonoBehaviour
{
    [SerializeField] private PlayerScaleController scaleController;
    private Tween _squashTween;

    public void Play(float targetMultiplier, float duration)
    {
        if (scaleController == null)
            return;

        if (duration <= 0f)
        {
            scaleController.SetVisualMultiplier(targetMultiplier);
            return;
        }

        if (_squashTween != null)
            _squashTween.Kill();

        _squashTween = DOTween.To(
                () => scaleController.VisualMultiplier,
                value => scaleController.SetVisualMultiplier(value),
                targetMultiplier,
                duration)
            .SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        if (_squashTween != null)
            _squashTween.Kill();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (scaleController == null)
            scaleController = GetComponent<PlayerScaleController>();
    }
#endif
}
