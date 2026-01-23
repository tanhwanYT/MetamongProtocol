#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_71f320d6_5649_42ad_a4c4_f6795f6151f1 : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool f06972e5_ed4c_4f9b_8b71_088608618dff { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "text";
    private string _21269e25_f5e0_4535_9154_7e458ab8bc84 { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _6fb08d1f_1588_407b_bdaa_4d3144fb2613 { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public string uiText = "LEVEL";
    private string _1803f953_cd8e_45da_9c8a_51e0ccd25d7e { get => uiText; set => uiText = value; }
    private string var_4 { get => uiText; set => uiText = value; }
    public float uiFontSize = 33f;
    private float e0a92a40_a39a_4075_b12a_b61ed2f89d2d { get => uiFontSize; set => uiFontSize = value; }
    private float var_5 { get => uiFontSize; set => uiFontSize = value; }
    public string uiSourceEntity = "1838d3a2-a2e4-481b-bd8b-85b2685c3dee";
    private string _78ea8547_36a4_4450_9d87_774b057b9d1f { get => uiSourceEntity; set => uiSourceEntity = value; }
    private string var_6 { get => uiSourceEntity; set => uiSourceEntity = value; }
    public string uiValueVar = "level";
    private string _6bac277b_8a84_4dee_9479_a70f697ee752 { get => uiValueVar; set => uiValueVar = value; }
    private string var_7 { get => uiValueVar; set => uiValueVar = value; }
    public string uiColor = "#ffffff";
    private string ed7a5d2a_75b9_4ff8_91bc_a1dcefbba5b8 { get => uiColor; set => uiColor = value; }
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
