using UnityEngine;
using UnityEngine.EventSystems;

namespace FletchersForge;

/// Drag the left grip on the closed-inventory quiver bar to reposition it.
internal sealed class QuiverHudMover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform target;
    private RectTransform parentRect;
    private Vector2 grabOffset;
    private bool dragging;

    private void Awake()
    {
        target = transform.parent as RectTransform;
        parentRect = target != null ? target.parent as RectTransform : null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || target == null)
        {
            return;
        }

        dragging = true;
        QuiverHud.BeginHudDrag();
        Camera camera = eventData.pressEventCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect != null ? parentRect : target,
            eventData.position,
            camera,
            out Vector2 local);
        grabOffset = target.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || target == null)
        {
            return;
        }

        Camera camera = eventData.pressEventCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect != null ? parentRect : target,
            eventData.position,
            camera,
            out Vector2 local);
        target.anchoredPosition = local + grabOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging || target == null)
        {
            return;
        }

        dragging = false;
        QuiverHud.SaveHudPosition(target.anchoredPosition);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            QuiverHud.ResetHudPosition();
        }
    }
}
