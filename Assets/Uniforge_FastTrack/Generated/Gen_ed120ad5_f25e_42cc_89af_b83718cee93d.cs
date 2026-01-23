#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_ed120ad5_f25e_42cc_89af_b83718cee93d : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _4fadeaa0_5004_4eb1_885e_9e75f5da48a3 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string _38c54b3d_181a_40ae_82cc_d319439c052e { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _09a05cdc_d542_4f13_96c5_d9fe709ce09d { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiText = "SPEED ";
    private string _83b4bf70_02d6_4a0c_8e34_5a928980c143 { get => uiText; set => uiText = value; }
    private string var_4 { get => uiText; set => uiText = value; }
    public float uiFontSize = 33f;
    private float e825ed86_6219_49cd_a3c6_0e3b5235aa23 { get => uiFontSize; set => uiFontSize = value; }
    private float var_5 { get => uiFontSize; set => uiFontSize = value; }
    public string uiSourceEntity = "1838d3a2-a2e4-481b-bd8b-85b2685c3dee";
    private string _34d5cff0_1812_4735_953d_36e98d048afb { get => uiSourceEntity; set => uiSourceEntity = value; }
    private string var_6 { get => uiSourceEntity; set => uiSourceEntity = value; }
    public string uiValueVar = "speed";
    private string d531ee70_3849_44bf_895e_c75129ca9761 { get => uiValueVar; set => uiValueVar = value; }
    private string var_7 { get => uiValueVar; set => uiValueVar = value; }
    public string uiColor = "#ffffff";
    private string _3da31b5a_5224_49d2_9bc3_84221685fbb8 { get => uiColor; set => uiColor = value; }
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
