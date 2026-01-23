#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_935d62bc_d098_4c4f_bb4c_379315ef35ea : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _0b9e5944_782b_4821_ac5d_5637547d5c30 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string _650dcad1_431e_4bc6_9490_064dfbfe1cbd { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float d7054fcb_fcf2_43fe_b625_5dde04a9c6af { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiText = "New Text";
    private string ad1e3c4c_8112_4444_8c2d_481f87b172dc { get => uiText; set => uiText = value; }
    private string var_4 { get => uiText; set => uiText = value; }
    public string uiColor = "#ffffff";
    private string _3fbdf9da_6f30_439e_bf22_21d32805b6f5 { get => uiColor; set => uiColor = value; }
    private string var_5 { get => uiColor; set => uiColor = value; }
    public float uiFontSize = 33f;
    private float _9fce9803_f09e_4b81_ab9c_57926fa3bb74 { get => uiFontSize; set => uiFontSize = value; }
    private float var_6 { get => uiFontSize; set => uiFontSize = value; }
    public string uiSourceEntity = "1838d3a2-a2e4-481b-bd8b-85b2685c3dee";
    private string _709be80f_6397_475b_aad8_51ac0ed9a328 { get => uiSourceEntity; set => uiSourceEntity = value; }
    private string var_7 { get => uiSourceEntity; set => uiSourceEntity = value; }
    public string uiValueVar = "exp";
    private string d412d7a2_4df7_41d2_9d11_e4234d0ceba0 { get => uiValueVar; set => uiValueVar = value; }
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
