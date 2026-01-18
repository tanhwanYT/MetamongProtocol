#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_9a7c69f1_c8bd_4c29_9184_e3fd5bb50a5e : MonoBehaviour
{
    // === User Variables ===

    // === System Fields ===
    private Transform _transform;
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Camera _mainCamera;

    public float hp = 100f;
    public float maxHp = 100f;
    private float _lastAttackTime = -999f;
    private bool _isGrounded = true;
    [SerializeField] private float _groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask _groundLayer = -1;
    private Dictionary<string, bool> _signalFlags = new Dictionary<string, bool>();
    private Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
    private Dictionary<string, Transform> _activeEmitters = new Dictionary<string, Transform>();

    void Awake()
    {
        Uniforge.FastTrack.Runtime.UniforgeRuntime.EnsureExists();
        _transform = transform;
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = gameObject.AddComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;
    }

    void Update()
    {
        CheckGrounded();

        if (Input.GetKeyDown(KeyCode.A))
        {
            // Attack: range=100, damage=10, cooldown=0.5s
            if (Time.time >= _lastAttackTime + 0.5f)
            {
                var hits = Physics2D.OverlapCircleAll(_transform.position, 1f);
                foreach (var hit in hits)
                {
                    if (hit.gameObject != gameObject)
                    {
                        hit.SendMessage("OnTakeDamage", 10f, SendMessageOptions.DontRequireReceiver);
                        ParticleManager.PlayStatic("blood", hit.transform.position, 1f);
                    }
                }
                _lastAttackTime = Time.time;
            }
            if (_animator != null) { Debug.Log("[Action] PlayAnimation: PlayerAttack_default"); _animator.Play("PlayerAttack_default"); }
            else { Debug.LogWarning("[Action] PlayAnimation Failed: Animator is null for PlayerAttack_default"); }
        }
    }

    // === Helper Methods ===

    private Vector3 GetMouseWorldPosition()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return Vector3.zero;
        return _mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    private Vector2 GetMouseRelativePosition()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();
        return new Vector2(mouseWorld.x - _transform.position.x, mouseWorld.y - _transform.position.y);
    }

    private void CheckGrounded()
    {
        if (_rigidbody == null) return;
        var hit = Physics2D.Raycast(_transform.position, Vector2.down, _groundCheckDistance, _groundLayer);
        _isGrounded = hit.collider != null;
    }

    private bool IsCooldownReady(string id) => !_cooldowns.ContainsKey(id) || Time.time >= _cooldowns[id];
    private void StartCooldown(string id, float duration) => _cooldowns[id] = Time.time + duration;

    private void OnDeath() { Debug.Log($"[{gameObject.name}] Died"); }
    public void OnTakeDamage(float damage) { hp -= damage; if (hp <= 0) { hp = 0f; OnDeath(); } }
    public void TakeDamage(float damage) => OnTakeDamage(damage);
}
