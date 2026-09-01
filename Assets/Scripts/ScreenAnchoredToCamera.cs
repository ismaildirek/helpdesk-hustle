using UnityEngine;

[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public class ScreenAnchoredToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private Vector3 viewportPosition;
    private bool isAnchored;

    private void OnEnable()
    {
        Camera.onPreCull += AnchorBeforeCameraRenders;
    }

    private void OnDisable()
    {
        Camera.onPreCull -= AnchorBeforeCameraRenders;
    }

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("ScreenAnchoredToCamera için Main Camera bulunamadı.");
            enabled = false;
            return;
        }

        viewportPosition = targetCamera.WorldToViewportPoint(transform.position);
        isAnchored = true;
    }

    private void LateUpdate()
    {
        AnchorToViewportPosition();
    }

    private void AnchorBeforeCameraRenders(Camera renderingCamera)
    {
        if (renderingCamera == targetCamera)
        {
            AnchorToViewportPosition();
        }
    }

    private void AnchorToViewportPosition()
    {
        if (!isAnchored || targetCamera == null) return;
        transform.position = targetCamera.ViewportToWorldPoint(viewportPosition);
    }
}
