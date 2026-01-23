#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_ecdac723_d5e6_4922_8df5_6903bad69dac : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _719fa278_7d29_4be1_b24d_9a4b3a01e1d5 { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "button";
    private string _1fb0ddc1_2852_4753_b9fe_e63ce74fce3c { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _00ccef75_2fbe_4242_b7c5_fc81658774d1 { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public float width = 120f;
    private float _16bcc6ff_481c_4d54_99f8_481998272e9f { get => width; set => width = value; }
    private float var_4 { get => width; set => width = value; }
    public float height = 40f;
    private float _7f0e6430_3a11_4f4e_b7b8_af621b69da60 { get => height; set => height = value; }
    private float var_5 { get => height; set => height = value; }
    public string uiText = "";
    private string _64a96e36_fb79_46be_b2a0_fb020dceeeae { get => uiText; set => uiText = value; }
    private string var_6 { get => uiText; set => uiText = value; }
    public string uiBackgroundColor = "#3498db";
    private string _6474eb68_1272_4b7c_a701_f031d1ed2ff3 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    private string var_7 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    public string uiColor = "#ffffff";
    private string _63e4c137_cd63_456a_ae2e_a2ded83beb5f { get => uiColor; set => uiColor = value; }
    private string var_8 { get => uiColor; set => uiColor = value; }
    public string uiFontSize = "16px";
    private string b7a8c3cd_494b_4654_ba9f_db7004f9093e { get => uiFontSize; set => uiFontSize = value; }
    private string var_9 { get => uiFontSize; set => uiFontSize = value; }
    public int levelpoint = 0;
    private int eb27643a_700a_41fd_98fe_72b306df0a23 { get => levelpoint; set => levelpoint = value; }
    private int var_10 { get => levelpoint; set => levelpoint = value; }
    public string texture = "공격력증가_1029";
    private string ace6bd0f_9a50_41db_b9b5_189ef66c9710 { get => texture; set => texture = value; }
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
            EventBus.Emit("attackup");
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
