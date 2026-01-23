#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_231ac8b6_45eb_4867_8d44_fd60625b6591 : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _31bd498c_1f46_4919_a1c4_190633f97b83 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string _34680250_56e0_4441_ace7_88891390fc0e { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _996b432e_4bb4_4834_8664_a45f50899cd4 { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiText = "ATK :";
    private string _64615ca3_17d0_4ac7_9ad0_9905f6826a7e { get => uiText; set => uiText = value; }
    private string var_4 { get => uiText; set => uiText = value; }
    public float uiFontSize = 33f;
    private float _4b3b975e_ee7b_4be8_96a6_ef5a41b79206 { get => uiFontSize; set => uiFontSize = value; }
    private float var_5 { get => uiFontSize; set => uiFontSize = value; }
    public string uiColor = "#ffffff";
    private string _6df63fde_b37f_4d93_9a2b_93bee07050a2 { get => uiColor; set => uiColor = value; }
    private string var_6 { get => uiColor; set => uiColor = value; }

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
