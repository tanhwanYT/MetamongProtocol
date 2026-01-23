#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_5240c845_1354_4f5e_be0a_8dcb5fe8979a : MonoBehaviour
{
    // === User Variables ===
    public bool isUI = true;
    private bool _371f027b_02f8_4dee_a081_d23ff620476c { get => isUI; set => isUI = value; }
    private bool var_1 { get => isUI; set => isUI = value; }
    public string uiType = "bar";
    private string _4a95d7be_ebfe_435e_8c4f_7f9a691857e7 { get => uiType; set => uiType = value; }
    private string var_2 { get => uiType; set => uiType = value; }
    public float z = 100f;
    private float _89f677aa_b031_47f8_b313_e03b80cd85ed { get => z; set => z = value; }
    private float var_3 { get => z; set => z = value; }
    public float width = 200f;
    private float _4bd67def_b5ac_4c5d_9429_b2d152c7308c { get => width; set => width = value; }
    private float var_4 { get => width; set => width = value; }
    public float height = 20f;
    private float _055ee370_1caf_4abd_8ad3_5d08fa7e7094 { get => height; set => height = value; }
    private float var_5 { get => height; set => height = value; }
    public string uiBackgroundColor = "#333333";
    private string _8ca5d37b_c7c3_432f_8065_6c4cedb8b46e { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    private string var_6 { get => uiBackgroundColor; set => uiBackgroundColor = value; }
    public string uiBarColor = "#e74c3c";
    private string a229a499_ec74_49a1_bc46_bcb23b971de3 { get => uiBarColor; set => uiBarColor = value; }
    private string var_7 { get => uiBarColor; set => uiBarColor = value; }
    public string uiMaxVar = "hpmax";
    private string _0eb390df_2d9d_4d29_807e_f7b8e52427ab { get => uiMaxVar; set => uiMaxVar = value; }
    private string var_8 { get => uiMaxVar; set => uiMaxVar = value; }
    public string uiSourceEntity = "1838d3a2-a2e4-481b-bd8b-85b2685c3dee";
    private string bc60dc07_b403_49b6_a998_a06410e88461 { get => uiSourceEntity; set => uiSourceEntity = value; }
    private string var_9 { get => uiSourceEntity; set => uiSourceEntity = value; }
    public string uiValueVar = "hp";
    private string _6c7a15ae_b634_4c03_8750_b6bf7e77a206 { get => uiValueVar; set => uiValueVar = value; }
    private string var_10 { get => uiValueVar; set => uiValueVar = value; }

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
