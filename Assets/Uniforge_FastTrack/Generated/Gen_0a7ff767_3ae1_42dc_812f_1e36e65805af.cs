#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_0a7ff767_3ae1_42dc_812f_1e36e65805af : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool f435b2ad_c364_4083_b099_3514972e2750 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "panel";
    private string fad236cb_9097_4506_a636_916ca3f801e9 { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _511ba68d_f183_4ab0_9e09_9c17e08fc43d { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiBackgroundColor = "#e1e6ea";
    private string _589e92ea_75f2_4d26_80dc_d6086c094849 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    private string var_4 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    public float width = 334f;
    private float d7c23ef6_ff21_4c5d_9809_da296e0085e8 { get => width; set => width = value; }
    private float var_5 { get => width; set => width = value; }
    public float height = 171f;
    private float a9b870d7_206b_43a8_a2aa_6a616af4f53f { get => height; set => height = value; }
    private float var_6 { get => height; set => height = value; }
    public string texture = "스탯창_0403";
    private string _58f2cb3b_04e1_4d16_ac6d_4b910622f152 { get => texture; set => texture = value; }
    private string var_7 { get => texture; set => texture = value; }

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
