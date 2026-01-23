#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_65f297b9_5b92_408a_bf48_ffccff6e6fdf : MonoBehaviour
{
    // === User Variables ===
    public Vector2 var_1 = new Vector2(0f, 0f);
    private Vector2 ac1e9150_36b2_44af_b24a_8d371b24b6d3 { get => var_1; set => var_1 = value; }

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
        Debug.Log($"[GenScript] {gameObject.name} initialized.");
    }

    void Start()
    {
        var_1 = new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x - _transform.position.x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y - _transform.position.y);
        // RunModule: b367bcc2-d264-4aea-99a0-d5840fbbf8d8
    }

    void Update()
    {
        CheckGrounded();

        {
            Vector2 moveDir = var_1;
            Vector3 dir3 = new Vector3(moveDir.x, moveDir.y, 0).normalized;
            _transform.Translate(dir3 * 2f * Time.deltaTime);
        }
        Debug.Log("[Action] PlayParticle: dust");
        ParticleManager.PlayStatic("dust", _transform.position, 1f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        _isGrounded = true;
        if (true)
        {
            Debug.Log("[Action] Disable Self");
            gameObject.SetActive(false);
            Debug.Log("[Action] PlayParticle: explosion");
            ParticleManager.PlayStatic("explosion", _transform.position, 1f);
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
