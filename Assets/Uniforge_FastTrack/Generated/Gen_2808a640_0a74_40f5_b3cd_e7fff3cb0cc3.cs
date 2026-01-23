#pragma warning disable 0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Uniforge.FastTrack.Runtime;

public class Gen_2808a640_0a74_40f5_b3cd_e7fff3cb0cc3 : MonoBehaviour
{
    // === User Variables ===
    public int player = 0;
    private int _641e72da_4fd7_489d_b233_8647e5eba885 { get => player; set => player = value; }
    private int var_1 { get => player; set => player = value; }
    public int enemy = 0;
    private int _752a74d3_ebce_438a_b670_94379a2e27b8 { get => enemy; set => enemy = value; }
    private int var_2 { get => enemy; set => enemy = value; }
    public int hpp = 100;
    private int ba13099d_0b09_4b81_bf3b_d76e5063e55a { get => hpp; set => hpp = value; }
    private int var_3 { get => hpp; set => hpp = value; }
    public int damage = 20;
    private int d5dcc77c_01e0_402e_9800_96fbee85c27e { get => damage; set => damage = value; }
    private int var_4 { get => damage; set => damage = value; }

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

        player = UniforgeEntity.FindById("1838d3a2-a2e4-481b-bd8b-85b2685c3dee")?.transform.position.x ?? 0f;
        enemy = player - _transform.position.x;
        if (enemy > 0f)
        {
            scaleX = 1f;
        }
        if (enemy < 0f)
        {
            scaleX = -1f;
        }
        if (hp > 0)
        {
            {
                Vector2 moveDir = UniforgeEntity.FindById("1838d3a2-a2e4-481b-bd8b-85b2685c3dee")?.transform.position.position ?? 0f;
                Vector3 dir3 = new Vector3(moveDir.x, moveDir.y, 0).normalized;
                _transform.Translate(dir3 * 2f * Time.deltaTime);
            }
            if (_animator != null)
            {
                if (_animator.runtimeAnimatorController == null)
                {
                    Debug.LogError($"[Action] PlayAnimation Failed: No AnimatorController assigned/ found on 'ZombieMove_default' request.");
                }
                else
                {
                    Debug.Log($"[Action] PlayAnimationRequest: 'ZombieMove_default' on {_animator.gameObject.name} (Controller: {_animator.runtimeAnimatorController.name})");
                    _animator.Play("ZombieMove_default");
                }
            }
            else { Debug.LogError("[Action] PlayAnimation Failed: Animator component is null."); }
        }
        if (hpp <= 0f)
        {
            Debug.Log("[Action] Disable Self");
            gameObject.SetActive(false);
            // SpawnEntity: 18f4e8a9-71de-4b1d-bd12-c31c32bb82d9
            {
                var spawnedObj = PrefabRegistry.SpawnStatic("18f4e8a9-71de-4b1d-bd12-c31c32bb82d9", _transform.position + new Vector3(0f, 0f, 0));
            }
            Debug.Log("[Action] PlayParticle: fire");
            ParticleManager.PlayStatic("fire", _transform.position, 1f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        _isGrounded = true;
        if (true)
        {
            hpp = hpp - damage;
        }
        if (true)
        {
            EventBus.Emit("playerattack");
            if (_animator != null)
            {
                if (_animator.runtimeAnimatorController == null)
                {
                    Debug.LogError($"[Action] PlayAnimation Failed: No AnimatorController assigned/ found on 'ZombieAttack_default' request.");
                }
                else
                {
                    Debug.Log($"[Action] PlayAnimationRequest: 'ZombieAttack_default' on {_animator.gameObject.name} (Controller: {_animator.runtimeAnimatorController.name})");
                    _animator.Play("ZombieAttack_default");
                }
            }
            else { Debug.LogError("[Action] PlayAnimation Failed: Animator component is null."); }
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
