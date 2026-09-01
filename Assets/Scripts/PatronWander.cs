using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PatronWander : MonoBehaviour
{
    public LayerMask obstacleLayer;
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float wanderRadius = 5f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Sprites")]
    public Sprite idleDown;
    public Sprite idleUp;
    public Sprite idleLeft;
    public Sprite idleRight;

    public Sprite walkDown;
    public Sprite walkUp;
    public Sprite walkLeft;
    public Sprite walkRight;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector2 startPosition;
    private Vector2 targetPosition;

    private bool moving = false;
    private float waitTimer;

    private Sprite currentIdle;
    private Sprite currentWalk;

    private bool walkFrame;
    private float animTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

       
        startPosition = rb.position;

        currentIdle = idleDown;
        currentWalk = walkDown;

        sr.sprite = currentIdle;

        waitTimer = Random.Range(minWaitTime, maxWaitTime);

        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (moving)
        {
            Move();

           
           
            animTimer += Time.deltaTime;

            if (animTimer >= 0.25f)
            {
                animTimer = 0f;
                walkFrame = !walkFrame;
                sr.sprite = walkFrame ? currentWalk : currentIdle;
            }
        }
        else
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                ChooseTarget();
                moving = true;
            }
        }
    }
    void Move()
    {
        Vector2 direction = (targetPosition - rb.position).normalized;

        UpdateDirection(direction);

        float distance = Vector2.Distance(rb.position, targetPosition);

        if (distance <= 0.05f)
        {
            StopMoving();
            return;
        }

        // Önümüzde engel var mý?
        RaycastHit2D hit = Physics2D.CircleCast(
        rb.position,
        0.25f,
        direction,
        0.25f,
        obstacleLayer
);

        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            // Engel varsa yeni hedef seç
            ChooseTarget();
            return;
        }

        rb.MovePosition(
            rb.position + direction * moveSpeed * Time.deltaTime
        );
    }
    void StopMoving()
    {
        moving = false;
        walkFrame = false;
        animTimer = 0f;

        sr.sprite = currentIdle;

        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void ChooseTarget()
    {
        for (int i = 0; i < 30; i++)
        {
            Vector2 point = startPosition + Random.insideUnitCircle * wanderRadius;

            Vector2 dir = (point - rb.position).normalized;
            float dist = Vector2.Distance(rb.position, point);

            RaycastHit2D hit = Physics2D.CircleCast(
               rb.position,
               0.25f,
                dir,
                dist,
                obstacleLayer
             );

            if (hit.collider == null)
            {
                targetPosition = point;
                return;
            }
        }

        targetPosition = rb.position;
    }

    void UpdateDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x > 0)
            {
                currentIdle = idleRight;
                currentWalk = walkRight;
            }
            else
            {
                currentIdle = idleLeft;
                currentWalk = walkLeft;
            }
        }
        else
        {
            if (dir.y > 0)
            {
                currentIdle = idleUp;
                currentWalk = walkUp;
            }
            else
            {
                currentIdle = idleDown;
                currentWalk = walkDown;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 center = Application.isPlaying
            ? (Vector3)startPosition
            : transform.position;

        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}