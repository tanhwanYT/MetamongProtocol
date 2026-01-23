#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_03ea9706_076b_49a5_81f7_b3234c07ac94 : MonoBehaviour
{
    // === User Variables ===
    public Vector2 player = new Vector2(0f, 0f);
    private Vector2 d7f51123_9be1_4182_938c_ca8fa6b4ffdb { get => player; set => player = value; }
    private Vector2 var_1 { get => player; set => player = value; }
    public Vector2 enemy = new Vector2(0f, 0f);
    private Vector2 _9be3b29a_c0b8_4053_be8e_3e071e4d6d3f { get => enemy; set => enemy = value; }
    private Vector2 var_2 { get => enemy; set => enemy = value; }
    public int range = 0;
    private int fe8c5d8d_5770_4c00_8a4a_33d4719f8003 { get => range; set => range = value; }
    private int var_3 { get => range; set => range = value; }
    public int hpp = 500;
    private int e0a4cc42_df81_4a5d_8f92_dc5a13fd52c2 { get => hpp; set => hpp = value; }
    private int var_4 { get => hpp; set => hpp = value; }

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

    void Update()
    {
        CheckGrounded();

        // RunModule: 2984a942-fc17-4aa0-a937-77ee9ec8fbac
        if (true)
        {
            Debug.LogWarning("[Action] PlayAnimation: No animation name specified");
        }
        else
        {
            range = 0;
        }
        if (hpp <= 0f)
        {
            Debug.Log("[Action] Disable Self");
            gameObject.SetActive(false);
            Debug.Log("[Action] PlayParticle: smoke");
            ParticleManager.PlayStatic("smoke", _transform.position, 1f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        _isGrounded = true;
        if (true)
        {
            hpp = hpp - 20f;
            if (_animator != null)
            {
                if (_animator.runtimeAnimatorController == null)
                {
                    Debug.LogError($"[Action] PlayAnimation Failed: No AnimatorController assigned/ found on '쳐맞는마왕_default' request.");
                }
                else
                {
                    Debug.Log($"[Action] PlayAnimationRequest: '쳐맞는마왕_default' on {_animator.gameObject.name} (Controller: {_animator.runtimeAnimatorController.name})");
                    _animator.Play("쳐맞는마왕_default");
                }
            }
            else { Debug.LogError("[Action] PlayAnimation Failed: Animator component is null."); }
        }
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
