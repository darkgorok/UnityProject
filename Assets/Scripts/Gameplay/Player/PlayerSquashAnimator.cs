using UnityEngine;
using DG.Tweening;

public class PlayerSquashAnimator : MonoBehaviour
{
    [SerializeField] private PlayerScaleController scaleController;
    private Tween _squashTween;

    private void Awake()
    {
        _squashTween = DOTween.Sequence();
        _squashTween.Pause();
    }

    public void Play(float targetMultiplier, float duration)
    {
        if (duration <= 0f)
        {
            scaleController.SetVisualMultiplier(targetMultiplier);
            return;
        }

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
        _squashTween.Kill();
    }
}
