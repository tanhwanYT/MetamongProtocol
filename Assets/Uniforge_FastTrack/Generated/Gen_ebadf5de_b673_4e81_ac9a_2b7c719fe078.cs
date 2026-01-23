#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_ebadf5de_b673_4e81_ac9a_2b7c719fe078 : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool f6223510_6d29_4247_9204_f54eab541f62 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string _6b6c2554_8804_4fc3_9117_d06e3218e883 { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _98926ca3_478f_4fbc_aa4b_335de770bc6c { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiText = "New Text";
    private string _4d0b1494_00f1_4ed9_b367_536ff690ca47 { get => uiText; set => uiText = value; }
    private string var_4 { get => uiText; set => uiText = value; }
    public string uiColor = "#ffffff";
    private string e5f49d3d_e20e_440f_9102_d833fbb259b7 { get => uiColor; set => uiColor = value; }
    private string var_5 { get => uiColor; set => uiColor = value; }
    public string uiSourceEntity = "1838d3a2-a2e4-481b-bd8b-85b2685c3dee";
    private string _0abce497_d8c0_4e73_b984_e2a2b19add11 { get => uiSourceEntity; set => uiSourceEntity = value; }
    private string var_6 { get => uiSourceEntity; set => uiSourceEntity = value; }
    public float uiFontSize = 33f;
    private float f9582b7e_9cd5_49ed_b205_88885c2c27d6 { get => uiFontSize; set => uiFontSize = value; }
    private float var_7 { get => uiFontSize; set => uiFontSize = value; }
    public string uiValueVar = "hp";
    private string _0c879fef_3343_4f90_951b_9bd9d897d580 { get => uiValueVar; set => uiValueVar = value; }
    private string var_8 { get => uiValueVar; set => uiValueVar = value; }

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
