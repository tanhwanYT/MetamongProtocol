#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_e69e96b5_e4e6_4cdc_9c02_5a098e332886 : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _931f2a30_f8a1_4cad_95c5_3c01f4747701 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "bar";
    private string _5aaed30a_b858_4e2b_8d30_089c32511c4c { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float dbaea4d8_3834_42f4_baa0_a928a0bcdc1e { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public float width = 200f;
    private float _63ae25fc_c920_4808_b8b7_f8b30ce0af54 { get => width; set => width = value; }
    private float var_4 { get => width; set => width = value; }
    public float height = 20f;
    private float _74e74e81_d725_408d_bfce_ddac9843f5ca { get => height; set => height = value; }
    private float var_5 { get => height; set => height = value; }
    public string uiBackgroundColor = "#333333";
    private string _867c22c1_6862_4f29_a536_83a9031003e9 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    private string var_6 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    public string uiSourceEntity = "1838d3a2-a2e4-481b-bd8b-85b2685c3dee";
    private string _8ca0f542_7d3d_4c39_a466_d46c0423333d { get => uiSourceEntity; set => uiSourceEntity = value; }
    private string var_7 { get => uiSourceEntity; set => uiSourceEntity = value; }
    public string uiValueVar = "exp";
    private string eb22dea9_512e_4c32_8557_89de83f0ce15 { get => uiValueVar; set => uiValueVar = value; }
    private string var_8 { get => uiValueVar; set => uiValueVar = value; }
    public string uiMaxVar = "expmax";
    private string e17cc9f3_aeac_4517_b19d_ee75a5d04266 { get => uiMaxVar; set => uiMaxVar = value; }
    private string var_9 { get => uiMaxVar; set => uiMaxVar = value; }
    public string uiBarColor = "#80e5ff";
    private string _20ff40a4_97c3_45dc_a9bd_2eda0afa1f0c { get => uiBarColor; set => uiBarColor = value; }
    private string var_10 { get => uiBarColor; set => uiBarColor = value; }

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
