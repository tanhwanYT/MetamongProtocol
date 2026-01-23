#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_68a6ca8a_5c46_4307_a2e4_39509b74706c : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool a580ba18_e26d_4ce3_913e_96ac12a3256a { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "button";
    private string a280515f_5f87_473a_b1af_bdb2e771fd6e { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float ad4e26c1_d168_4280_8421_028dae6b7366 { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public float width = 120f;
    private float _836e05e5_8c1c_446e_aecf_0e70e1768e57 { get => width; set => width = value; }
    private float var_4 { get => width; set => width = value; }
    public float height = 40f;
    private float _43804930_3a80_4a70_9157_549768e5f330 { get => height; set => height = value; }
    private float var_5 { get => height; set => height = value; }
    public string uiText = "";
    private string _9c012456_97ee_4e5e_bfc5_c93c707ff607 { get => uiText; set => uiText = value; }
    private string var_6 { get => uiText; set => uiText = value; }
    public string uiBackgroundColor = "#3498db";
    private string _13558e6c_3a27_41e9_84d6_89b07f8dbe83 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    private string var_7 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    public string uiColor = "#ffffff";
    private string _2db8ad91_1e55_43c6_8394_289c1a9a9476 { get => uiColor; set => uiColor = value; }
    private string var_8 { get => uiColor; set => uiColor = value; }
    public string uiFontSize = "16px";
    private string acde48e8_91e7_49b7_8fe5_9ef7d5e76a3d { get => uiFontSize; set => uiFontSize = value; }
    private string var_9 { get => uiFontSize; set => uiFontSize = value; }
    public string texture = "나만무 SpeedButton_5241";
    private string cdf96a6b_3c4e_4d3e_a1c4_42ffd6b94fb7 { get => texture; set => texture = value; }
    private string var_10 { get => texture; set => texture = value; }
    public int levelpoint = 0;
    private int c90121d6_26a9_4d2b_8865_b96b4a393494 { get => levelpoint; set => levelpoint = value; }
    private int var_11 { get => levelpoint; set => levelpoint = value; }

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
            EventBus.Emit("speedup");
            levelpoint = levelpoint - 1f;
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
