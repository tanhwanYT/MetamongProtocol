#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_dbf52489_f5ec_4ef5_a725_586c04563c87 : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _5a069412_fee6_4aba_9771_a212e59cca22 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string cb41a1b9_d357_4f61_b16b_04ee5917b6d6 { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _3bd06f2f_d81a_457e_9188_decb36f44c4a { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiColor = "#ffffff";
    private string _3a852784_1b8f_46fe_af1d_89869b4eae57 { get => uiColor; set => uiColor = value; }
    private string var_4 { get => uiColor; set => uiColor = value; }
    public string uiText = "HP : ";
    private string _241353d6_6942_4d3b_890e_c7e82a6cd2b6 { get => uiText; set => uiText = value; }
    private string var_5 { get => uiText; set => uiText = value; }
    public float uiFontSize = 33f;
    private float _972883ec_7d65_4c42_bf0b_afd77a52e853 { get => uiFontSize; set => uiFontSize = value; }
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
