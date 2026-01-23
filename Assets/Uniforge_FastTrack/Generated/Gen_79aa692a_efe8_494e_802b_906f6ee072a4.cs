#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_79aa692a_efe8_494e_802b_906f6ee072a4 : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool ef2fd2f9_a6a4_456b_8f29_479cf9dd5b1b { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "panel";
    private string _9a97022b_e5f6_4c45_96a5_e1d00b15640b { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _8286981f_5cb0_4fbb_af59_ebc3af72b30b { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiBackgroundColor = "#e1e6ea";
    private string c2814492_461b_4789_9dab_822fa5ca5145 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    private string var_4 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    public float width = 334f;
    private float _5909a2b5_0022_4b08_8ad1_3796be339913 { get => width; set => width = value; }
    private float var_5 { get => width; set => width = value; }
    public float height = 171f;
    private float _6b3cd174_4496_4378_bbc2_f7a9e0d3b42a { get => height; set => height = value; }
    private float var_6 { get => height; set => height = value; }
    public string texture = "스탯창_0403";
    private string f51b5d12_22bc_4b0f_bf37_6694c4a05999 { get => texture; set => texture = value; }
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
