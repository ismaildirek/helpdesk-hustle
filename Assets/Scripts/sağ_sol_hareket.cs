using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BoardInspector : MonoBehaviour
{
    [Header("Movement")]
    public float moveDistance = 2f;
    public float moveSpeed = 1f;

    [Header("Sprites")]
    public Sprite idleLeft;
    public Sprite idleRight;
    public Sprite walkLeft;
    public Sprite walkRight;

    private SpriteRenderer sr;

    private Vector2 startPos;
    private Vector2 targetPos;

    private bool goingRight = true;

    private bool walkFrame;
    private float animTimer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        startPos = transform.position;
        targetPos = startPos + Vector2.right * moveDistance;

        sr.sprite = idleRight;
    }

    void Update()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPos) < 0.05f)
        {
            goingRight = !goingRight;

            if (goingRight)
            {
                targetPos = startPos + Vector2.right * moveDistance;
                sr.sprite = idleRight;
            }
            else
            {
                targetPos = startPos + Vector2.left * moveDistance;
                sr.sprite = idleLeft;
            }
        }

        animTimer += Time.deltaTime;

        if (animTimer >= 0.25f)
        {
            animTimer = 0f;
            walkFrame = !walkFrame;

            if (goingRight)
                sr.sprite = walkFrame ? walkRight : idleRight;
            else
                sr.sprite = walkFrame ? walkLeft : idleLeft;
        }
    }
}