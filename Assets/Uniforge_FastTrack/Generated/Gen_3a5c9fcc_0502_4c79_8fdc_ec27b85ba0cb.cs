#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_3a5c9fcc_0502_4c79_8fdc_ec27b85ba0cb : MonoBehaviour
{
    // === User Variables ===
    public int hp = 100;
    private int _4162eee8_0e1a_4a7b_8e3e_744d6c3a252c { get => hp; set => hp = value; }
    private int var_1 { get => hp; set => hp = value; }
    public int hpmax = 100;
    private int _5530d30f_4f0c_442d_9702_5f5426c1e0d7 { get => hpmax; set => hpmax = value; }
    private int var_2 { get => hpmax; set => hpmax = value; }
    public int exp = 0;
    private int _41d4d794_19b9_4611_a242_a096a8a8107c { get => exp; set => exp = value; }
    private int var_3 { get => exp; set => exp = value; }
    public int expmax = 100;
    private int _27128147_95e6_42b2_8820_fb17db4d5b71 { get => expmax; set => expmax = value; }
    private int var_4 { get => expmax; set => expmax = value; }
    public int speed = 200;
    private int _679679eb_9670_4dad_bfa4_62711196ea6a { get => speed; set => speed = value; }
    private int var_5 { get => speed; set => speed = value; }
    public int level = 0;
    private int _2c447dc5_2417_4372_a02e_61f29e4dc5fe { get => level; set => level = value; }
    private int var_6 { get => level; set => level = value; }

    // === System Fields ===
    private Transform _transform;
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Camera _mainCamera;

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

    void Update()
    {
        CheckGrounded();

        if (Input.GetKeyDown(KeyCode.W))
        {
            _transform.Translate(new Vector3(0f, -1f, 0).normalized * 2f * Time.deltaTime);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            _transform.Translate(new Vector3(-1f, 0f, 0).normalized * 2f * Time.deltaTime);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            _transform.Translate(new Vector3(0f, 1f, 0).normalized * 2f * Time.deltaTime);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            _transform.Translate(new Vector3(1f, 0f, 0).normalized * 2f * Time.deltaTime);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            scaleX = -1f;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            scaleX = 1f;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // SpawnEntity: f83f8085-ae7b-487a-9dc4-f29a803a1314
            {
                var spawnedObj = PrefabRegistry.SpawnStatic("f83f8085-ae7b-487a-9dc4-f29a803a1314", _transform.position + new Vector3(0f, 0f, 0));
            }
        }
        if (exp >= 100f)
        {
            exp = 0;
            EventBus.Emit("levelup");
            Debug.Log("[Action] PlayParticle: confetti");
            ParticleManager.PlayStatic("confetti", _transform.position, 1f);
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
    public void OnTakeDamage(float damage) { hp -= (int)damage; if (hp <= 0) { hp = 0; OnDeath(); } }
    public void TakeDamage(float damage) => OnTakeDamage(damage);
}
