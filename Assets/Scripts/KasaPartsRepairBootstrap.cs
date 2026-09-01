using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class KasaPartsRepairBootstrap : MonoBehaviour
{
    [Header("Static Scene Visuals")]
    [SerializeField] private Sprite workshopBackground;
    [SerializeField] private Sprite intactCase;

    [Header("Clickable Damage Layers")]
    [SerializeField] private Sprite[] repairableSprites;

    private void Awake()
    {
        EnsurePartsInScene();
    }

    public void Configure(
        Sprite configuredBackground,
        Sprite configuredIntactCase,
        Sprite[] configuredSprites)
    {
        workshopBackground = configuredBackground;
        intactCase = configuredIntactCase;
        repairableSprites = configuredSprites ?? System.Array.Empty<Sprite>();
    }

    public void EnsurePartsInScene()
    {
        Camera gameCamera = GetComponent<Camera>();
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<KasaRepairPartButton> buttons = new();

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.gameObject.scene != gameObject.scene ||
                renderer.sprite == null ||
                !IsRepairableSprite(renderer.sprite))
            {
                continue;
            }

            KasaRepairPartButton button =
                renderer.GetComponent<KasaRepairPartButton>();
            if (button == null)
                button = renderer.gameObject.AddComponent<KasaRepairPartButton>();

            button.Configure(renderer);
            buttons.Add(button);
        }

        KasaPartsRepairController controller =
            GetComponent<KasaPartsRepairController>();
        if (controller == null)
            controller = gameObject.AddComponent<KasaPartsRepairController>();

        controller.Configure(gameCamera, buttons.ToArray());
    }

    private bool IsRepairableSprite(Sprite sprite)
    {
        if (repairableSprites == null)
            return false;

        foreach (Sprite repairableSprite in repairableSprites)
        {
            if (repairableSprite == sprite)
                return true;
        }

        return false;
    }

}
