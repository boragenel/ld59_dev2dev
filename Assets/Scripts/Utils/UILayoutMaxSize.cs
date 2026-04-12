using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Internal.LayoutComponents
{
/// <summary>
/// Extends an existing layout element to enforce maximum width and height constraints
/// Can be used with layout, text, image and basic LayoutElement
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UILayoutMaxSize : UIBehaviour, ILayoutElement
{
    [SerializeField] private float m_MaxHeight = -1;
    [SerializeField] private float m_MaxWidth = -1;
    [SerializeField] private int m_Priority = 2;

    private float m_PreferredHeight;
    private float m_PreferredWidth;

    public virtual float maxWidth
    {
        get => m_MaxWidth;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (m_MaxWidth != value)
            {
                SetDirty();
            }
        }
    }

    public virtual float maxHeight
    {
        get => m_MaxHeight;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (m_MaxHeight != value)
            {
                SetDirty();
            }
        }
    }

    public void CalculateLayoutInputHorizontal()
    {
        m_PreferredWidth = -1;
        if (m_MaxWidth < 0 || !IsActive())
        {
            return;
        }

        float neighborPreferredWidth = LayoutUtility.GetPreferredWidth(transform as RectTransform);
        if (neighborPreferredWidth >= 0 && neighborPreferredWidth > m_MaxWidth)
        {
            m_PreferredWidth = m_MaxWidth;
        }
    }

    public void CalculateLayoutInputVertical()
    {
        m_PreferredHeight = -1;
        if (m_MaxHeight < 0 || !IsActive())
        {
            return;
        }

        float neighborPreferredHeight = LayoutUtility.GetPreferredHeight(transform as RectTransform);
        if (neighborPreferredHeight >= 0 && neighborPreferredHeight > m_MaxHeight)
        {
            m_PreferredHeight = m_MaxHeight;
        }
    }

    public float minWidth => -1;

    public float preferredWidth => m_PreferredWidth;

    public float flexibleWidth => m_MaxHeight < 0 ? -1 : 0;

    public float minHeight => -1;

    public float preferredHeight => m_PreferredHeight;

    public float flexibleHeight => m_MaxHeight < 0 ? -1 : 0;
    public int layoutPriority => m_Priority;

    protected void SetDirty()
    {
        if (!IsActive())
        {
            return;
        }
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }
#endif
}
}