using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class UIFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset;
    [SerializeField] private Vector2 screenOffset;
    [SerializeField] private Camera targetCamera;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (transform.parent) parentRect = transform.parent.GetComponent<RectTransform>();
        if (!targetCamera) targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!target) return;
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera) return;

        Vector3 screenPoint = targetCamera.WorldToScreenPoint(target.position + worldOffset);

        // Move off-screen if behind camera
        if (screenPoint.z < 0)
        {
            rectTransform.anchoredPosition = new Vector2(-9999, -9999);
            return;
        }

        if (parentRect)
        {
            Camera uiCam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCam, out Vector2 localPoint);
            rectTransform.anchoredPosition = localPoint + screenOffset;
        }
        else
        {
            rectTransform.position = screenPoint + (Vector3)screenOffset;
        }
    }

    public void SetTarget(Transform newTarget, Vector3? offset = null)
    {
        target = newTarget;
        if (offset.HasValue) worldOffset = offset.Value;
    }
}
