using UnityEngine;
public class DustParticle : MonoBehaviour
{
    public Vector2 velocity;
    float life = .65f;
    SpriteRenderer sprite;
    void Awake() { sprite = GetComponent<SpriteRenderer>(); }
    void Update()
    {
        life -= Time.deltaTime;
        velocity += Vector2.down * 6 * Time.deltaTime;
        transform.position += (Vector3)velocity * Time.deltaTime;
        Color c = sprite.color; c.a = Mathf.Clamp01(life / .65f); sprite.color = c;
        if (life <= 0) Destroy(gameObject);
    }
}
