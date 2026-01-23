#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_f4807a1a_59d1_43f2_8258_9557010bf115 : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _0abaaeb8_aa16_4f07_819c_07d677233465 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string ad929ee8_1a0d_431f_a91d_0afb5a8630e9 { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _7197f9fb_111e_4d8f_ad18_f7ccadd747c1 { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiText = "ATK";
    private string e0912055_ad31_4e07_83a3_4c0ecbb8d02d { get => uiText; set => uiText = value; }
    private string var_4 { get => uiText; set => uiText = value; }
    public float uiFontSize = 33f;
    private float _4b3e29a8_e9d0_4a17_a81c_86e9a8b57463 { get => uiFontSize; set => uiFontSize = value; }
    private float var_5 { get => uiFontSize; set => uiFontSize = value; }
    public string uiSourceEntity = "1838d3a2-a2e4-481b-bd8b-85b2685c3dee";
    private string d35ed32c_a099_4c03_a79a_6a9bcba08456 { get => uiSourceEntity; set => uiSourceEntity = value; }
    private string var_6 { get => uiSourceEntity; set => uiSourceEntity = value; }
    public string uiValueVar = "damage";
    private string e95381b8_1edb_4780_a59a_6a1b367c0541 { get => uiValueVar; set => uiValueVar = value; }
    private string var_7 { get => uiValueVar; set => uiValueVar = value; }
    public string uiColor = "#ffffff";
    private string _4aeedc70_f3dd_43fd_9fae_3389d5a1ab25 { get => uiColor; set => uiColor = value; }
    private string var_8 { get => uiColor; set => uiColor = value; }

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
