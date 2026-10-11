using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    // Actual array entries drive walking, jumping and dropping (HW2 Vector + Array).
    public Vector2[] moveDirections = { Vector2.right, Vector2.up, Vector2.down };
    public float moveSpeed = 5f;
    public float jumpSpeed = 8.6f;
    public LayerMask platformMask;
    public SpriteRenderer artwork;
    public Animator animator;
    public GameManager game;
    public List<Vector3> pathHistory = new List<Vector3>();
    public bool Grounded { get; private set; }
    public int JumpCount { get; private set; }
    public int DropCount { get; private set; }
    public Rigidbody2D Body { get; private set; }
    public Collider2D Ground { get; private set; }
    CapsuleCollider2D capsule;
    Collider2D ignoredPlatform;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    float horizontal, coyote, buffer, sampleTime, ignoreTime;
    bool dropQueued, wasGrounded, controlledExternally;
    Vector3 visualScale;

    void Awake()
    {
        Body = GetComponent<Rigidbody2D>(); capsule = GetComponent<CapsuleCollider2D>();
        visualScale = artwork.transform.localScale;
    }
    void Start() { pathHistory.Add(transform.position); }
    void Update()
    {
        if (!controlledExternally)
        {
            horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) buffer = .14f;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dropQueued = true;
        }
        RefreshVisual();
    }
    public void RefreshVisual()
    {
        if (game.State != GameState.Playing) return;
        if (Mathf.Abs(horizontal) > .01f) artwork.flipX = horizontal < 0;
        animator.SetInteger("Pose", !Grounded ? (Body.velocity.y > .05f ? 2 : 3) : (Mathf.Abs(horizontal) > .01f ? 1 : 0));
        float stretch = !Grounded ? 1.035f : 1f;
        artwork.transform.localScale = new Vector3(visualScale.x / stretch, visualScale.y * stretch, 1);
    }
    public void SetInput(float move, bool jump = false, bool drop = false)
    {
        controlledExternally = true; horizontal = Mathf.Clamp(move,-1,1);
        if (jump) buffer = .14f; if (drop) dropQueued = true;
    }
    void FixedUpdate() { Tick(Time.fixedDeltaTime); }
    public void Tick(float dt)
    {
        if (game.State != GameState.Playing) { Body.velocity = Vector2.zero; return; }
        RestorePlatform(dt);
        ProbeGround();
        if (Grounded) coyote = .12f; else coyote -= dt;
        buffer -= dt;
        Vector2 walk = moveDirections[0] * horizontal * moveSpeed;
        Body.velocity = new Vector2(walk.x, Body.velocity.y);
        if (dropQueued)
        {
            dropQueued = false;
            if (Grounded && Ground != null && Ground.GetComponent<PlatformEffector2D>() != null)
            {
                ignoredPlatform = Ground; Physics2D.IgnoreCollision(capsule, ignoredPlatform, true);
                ignoreTime = 0; Body.velocity = walk + moveDirections[2] * 3f;
                Grounded = false; coyote = 0; buffer = 0; DropCount++;
                game.PlayCue("drop"); game.Emit(transform.position + Vector3.down * .65f, game.accent, 9);
            }
            else if (!Grounded) Body.velocity = new Vector2(walk.x, Mathf.Min(Body.velocity.y,-7f));
        }
        if (buffer > 0 && coyote > 0 && ignoredPlatform == null)
        {
            Body.velocity = walk + moveDirections[1] * jumpSpeed;
            buffer = 0; coyote = 0; Grounded = false; JumpCount++;
            game.PlayCue("jump"); game.Emit(transform.position + Vector3.down * .65f, game.accent, 8);
        }
        if (Grounded && !wasGrounded && Body.velocity.y <= .1f)
        { game.PlayCue("land"); game.Emit(transform.position + Vector3.down * .65f, new Color(.8f,.94f,1), 6); }
        wasGrounded = Grounded;
        sampleTime += dt;
        if (sampleTime >= .2f) { sampleTime = 0; if (pathHistory.Count < 3000) pathHistory.Add(transform.position); }
        if (Body.position.y < -5f) game.Fail();
        game.CheckFinish();
        RefreshVisual();
    }
    void ProbeGround()
    {
        Grounded = false; Ground = null;
        if (Body.velocity.y > .15f) return;
        ContactFilter2D filter = new ContactFilter2D(); filter.SetLayerMask(platformMask); filter.useTriggers = false;
        int count = capsule.Cast(Vector2.down, filter, groundHits, .085f);
        for (int i=0; i<count; i++)
        {
            if (groundHits[i].normal.y > .5f && groundHits[i].collider != ignoredPlatform)
            { Grounded = true; Ground = groundHits[i].collider; break; }
        }
    }
    void RestorePlatform(float dt)
    {
        if (ignoredPlatform == null) return;
        ignoreTime += dt;
        if (capsule.bounds.max.y < ignoredPlatform.bounds.min.y - .04f || ignoreTime > 2f)
        { Physics2D.IgnoreCollision(capsule, ignoredPlatform, false); ignoredPlatform = null; }
    }
    public void ResetPlayer(Vector2 start)
    {
        if (ignoredPlatform != null) Physics2D.IgnoreCollision(capsule, ignoredPlatform, false);
        ignoredPlatform = null; Body.simulated = true; Body.position = start; Body.velocity = Vector2.zero;
        horizontal = coyote = buffer = sampleTime = 0; dropQueued = Grounded = wasGrounded = false;
        JumpCount = DropCount = 0; pathHistory.Clear(); pathHistory.Add(start);
        artwork.flipX = false; artwork.transform.localScale = visualScale; animator.SetInteger("Pose",0);
    }
}
