using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[Serializable]
public class Item
{
    public Cell Cell { get; private set; }

    public SpriteRenderer CachedSpriteRenderer { get; private set; }
    public Transform View => CachedSpriteRenderer.transform;

    public virtual void SetView(SpriteRenderer spriteRenderer)
    {
        CachedSpriteRenderer = spriteRenderer;
    }

    public virtual void ReactivateView()
    {
        View.gameObject.SetActive(true);
        View.localScale = Vector3.one;
    }

    public virtual void CreateView()
    {
        string prefabName = GetPrefabName();

        if (!string.IsNullOrEmpty(prefabName))
        {
            var prefab = Resources.Load<SpriteRenderer>(prefabName);
            if (prefab)
            {
                CachedSpriteRenderer = GameObject.Instantiate(prefab);
            }
        }
    }

    protected virtual string GetPrefabName() { return string.Empty; }

    public virtual void SetCell(Cell cell)
    {
        Cell = cell;
    }

    internal void AnimationMoveToPosition()
    {
        if (!View) return;

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
        if (!View) return;
        
        if (CachedSpriteRenderer)
        {
            CachedSpriteRenderer.sortingOrder = 1;
        }
    }


    public void SetSortingLayerLower()
    {
        if (!View) return;
        
        if (CachedSpriteRenderer)
        {
            CachedSpriteRenderer.sortingOrder = 0;
        }

    }

    internal void ShowAppearAnimation()
    {
        if (!View) return;

        Vector3 scale = View.localScale;
        View.localScale = Vector3.one * 0.1f;
        View.DOScale(scale, 0.1f);
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
                    View.gameObject.SetActive(false);
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
            View.gameObject.SetActive(false);
        }
    }
}
