#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_f6b83544_6805_4602_828e_b8efa90ec64e : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _7334c7a5_59a9_4854_b3ad_51f64a4e8114 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "button";
    private string c2fa6e87_d66d_4820_ae37_74b95bbd0fe6 { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float c2e2bfb7_459c_4ec8_88ab_16cc50d4f665 { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public float width = 120f;
    private float a5aac925_2525_4b84_819c_698593c04bed { get => width; set => width = value; }
    private float var_4 { get => width; set => width = value; }
    public float height = 40f;
    private float ec8f4721_97ef_410b_9b62_a68231614ce0 { get => height; set => height = value; }
    private float var_5 { get => height; set => height = value; }
    public string uiText = "";
    private string _99b2b0c0_1638_4b95_ba69_91e7726f72b8 { get => uiText; set => uiText = value; }
    private string var_6 { get => uiText; set => uiText = value; }
    public string uiBackgroundColor = "#3498db";
    private string b2391492_6b8c_4314_9d75_dd649e927897 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    private string var_7 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    public string uiColor = "#ffffff";
    private string d760fa2c_c76b_4096_900f_ce42dfb27805 { get => uiColor; set => uiColor = value; }
    private string var_8 { get => uiColor; set => uiColor = value; }
    public string uiFontSize = "16px";
    private string _9af6fe82_4aeb_4ef8_aa08_74d9f5d87d88 { get => uiFontSize; set => uiFontSize = value; }
    private string var_9 { get => uiFontSize; set => uiFontSize = value; }
    public int levelpoint = 0;
    private int a8c62ee1_8595_4906_b9ad_82f39f572934 { get => levelpoint; set => levelpoint = value; }
    private int var_10 { get => levelpoint; set => levelpoint = value; }
    public string texture = "체력증가_0652";
    private string _706f3bd0_1931_4db9_81c0_1f403860dc6e { get => texture; set => texture = value; }
    private string var_11 { get => texture; set => texture = value; }

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

    void OnMouseDown()
    {
        if (levelpoint >= 1f)
        {
            EventBus.Emit("hpup");
            EventBus.Emit("leveldown");
            AudioManager.PlayStatic("");
        }
    }

    // TODO: Unhandled trigger: OnSignalReceive
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
