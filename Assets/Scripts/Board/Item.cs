using System;
using Core.Pooling;
using UnityEngine;
using DG.Tweening;

[Serializable]
public class Item
{
    public Cell Cell { get; private set; }

    public Transform View { get; private set; }

    private Vector3 m_defaultScale;

    private SpriteRenderer m_SprRenderer;

    public virtual void SetView(GameManager gm, Transform root)
    {
        if (gm.preloadResources.TryGetValue(GetPrefabName(), out var result))
        {
            m_defaultScale = result.transform.localScale;
            View = ObjectPool.Get(result, root).transform;
            m_SprRenderer = View.GetComponent<SpriteRenderer>();
        }
    }

    protected virtual string GetPrefabName() { return string.Empty; }

    public virtual void SetCell(Cell cell)
    {
        Cell = cell;
    }

    internal void AnimationMoveToPosition()
    {
        if (View == null) return;

        View.DOMove(Cell.transform.position, 0.2f);
    }

    public void SetViewPosition(Vector3 pos)
    {
        if (View)
        {
            View.position = pos;
        }
    }

    public void SetViewRoot(Transform root)
    {
        if (View)
        {
            View.SetParent(root);
        }
    }

    public void SetSortingLayerHigher()
    {
        if (View == null) return;

        if (m_SprRenderer)
        {
            m_SprRenderer.sortingOrder = 1;
        }
    }


    public void SetSortingLayerLower()
    {
        if (View == null) return;

        if (m_SprRenderer)
        {
            m_SprRenderer.sortingOrder = 0;
        }

    }

    internal void ShowAppearAnimation()
    {
        if (View == null) return;

        View.localScale = Vector3.one * 0.1f;
        View.DOScale(m_defaultScale, 0.1f);
    }

    internal virtual bool IsSameType(Item other)
    {
        return false;
    }

    internal virtual void ExplodeView()
    {
        if (View)
        {
            View.DOScale(0.1f, 0.1f).OnComplete(
                () =>
                {
                    ObjectPool.Recycle(View.gameObject);
                    View = null;
                }
            );
        }
    }



    internal void AnimateForHint()
    {
        if (View)
        {
            View.DOPunchScale(View.localScale * 0.1f, 0.1f).SetLoops(-1);
        }
    }

    internal void StopAnimateForHint()
    {
        if (View)
        {
            View.DOKill();
        }
    }

    internal void Clear()
    {
        Cell = null;

        if (View)
        {
            ObjectPool.Recycle(View.gameObject);
            View = null;
        }
    }
}
