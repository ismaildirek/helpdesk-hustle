using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class KasaRepairPartButton : MonoBehaviour
{
    [SerializeField] private SpriteRenderer partRenderer;

    public Vector3 RestingScale { get; private set; }
    public bool IsBeingRepaired { get; private set; }

    public SpriteRenderer Renderer
    {
        get
        {
            if (partRenderer == null)
                partRenderer = GetComponent<SpriteRenderer>();

            return partRenderer;
        }
    }

    public bool IsVisible =>
        isActiveAndEnabled &&
        Renderer != null &&
        Renderer.enabled;

    private void Awake()
    {
        if (partRenderer == null)
            partRenderer = GetComponent<SpriteRenderer>();

        CacheRestingTransform();
    }

    public void Configure(SpriteRenderer renderer)
    {
        partRenderer = renderer;
        CacheRestingTransform();
    }

    public bool Contains(Vector2 worldPosition)
    {
        if (!IsVisible || IsBeingRepaired)
            return false;

        Bounds bounds = Renderer.bounds;
        return bounds.Contains(new Vector3(
            worldPosition.x,
            worldPosition.y,
            bounds.center.z));
    }

    public void Hide()
    {
        if (Renderer != null)
            Renderer.enabled = false;

        IsBeingRepaired = false;
    }

    public bool TryBeginRepair()
    {
        if (!IsVisible || IsBeingRepaired)
        {
            return false;
        }

        IsBeingRepaired = true;
        return true;
    }

    private void CacheRestingTransform()
    {
        RestingScale = transform.localScale;
        IsBeingRepaired = false;
    }
}
