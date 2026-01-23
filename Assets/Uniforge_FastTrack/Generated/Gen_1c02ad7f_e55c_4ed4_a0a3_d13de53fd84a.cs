#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_1c02ad7f_e55c_4ed4_a0a3_d13de53fd84a : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _5e39d397_5dd8_4564_9983_5e5a7de9d437 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string fb947275_5d50_4451_ab61_8b93eb1fc79f { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _8ea25237_76f3_42b2_92be_295ebfcf55ae { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiColor = "#ffffff";
    private string _4bd9111d_8205_48e5_bee4_723b8c695893 { get => uiColor; set => uiColor = value; }
    private string var_4 { get => uiColor; set => uiColor = value; }
    public string uiText = "EXP : ";
    private string _0ac8e511_f76a_42a1_963d_82e0e861b5f2 { get => uiText; set => uiText = value; }
    private string var_5 { get => uiText; set => uiText = value; }
    public float uiFontSize = 33f;
    private float _59ebcd18_babc_41d1_aec1_f6a682dbf12f { get => uiFontSize; set => uiFontSize = value; }
    private float var_6 { get => uiFontSize; set => uiFontSize = value; }

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
