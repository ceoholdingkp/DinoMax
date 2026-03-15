using UnityEngine;

/// <summary>
/// DinoMax — Controlador de movimiento del dinosaurio jugable.
///
/// Soporta dos perfiles: T-Rex (tanque lento y poderoso)
/// y Velociraptor (ágil y rápido). Los stats se aplican
/// automáticamente según el campo <see cref="dinoType"/>.
///
/// Requisitos en el GameObject:
///   - CharacterController
///   - Animator  (con parámetros: Speed, IsGrounded, Attack, Roar)
///   - Tag "Player"
///
/// Controles:
///   WASD          → moverse
///   Shift         → correr
///   Click izquierdo → atacar
///   R             → rugir
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class DinoController : MonoBehaviour
{
    // ─── Tipo de dinosaurio ────────────────────────────────────────
    public enum DinoType { TRex, Velociraptor }

    [Header("Dinosaurio")]
    public DinoType dinoType = DinoType.TRex;

    // ─── Movimiento ────────────────────────────────────────────────
    [Header("Movimiento")]
    [Tooltip("Velocidad caminando")]
    public float walkSpeed = 5f;

    [Tooltip("Velocidad corriendo (Shift)")]
    public float runSpeed = 12f;

    [Tooltip("Velocidad de rotación en grados/segundo")]
    public float rotationSpeed = 120f;

    [Tooltip("Gravedad extra (negativo = hacia abajo)")]
    public float gravity = -20f;

    // ─── Combate ───────────────────────────────────────────────────
    [Header("Combate")]
    public float attackDamage   = 25f;
    public float attackRange    = 2.5f;
    public float attackCooldown = 1.2f;
    public LayerMask enemyLayer;

    [Tooltip("Transform vacío posicionado en la boca del dino")]
    public Transform attackPoint;

    [Tooltip("Partículas al golpear")]
    public ParticleSystem attackEffect;

    // ─── Componentes (privados) ────────────────────────────────────
    private CharacterController _cc;
    private Animator            _anim;
    private DinoHealth          _health;

    // ─── Estado interno ────────────────────────────────────────────
    private Vector3 _vertVelocity;      // gravedad acumulada
    private float   _lastAttackTime;
    private bool    _isGrounded;

    // ─── Hashes de parámetros del Animator (eficiencia) ───────────
    private static readonly int HashSpeed    = Animator.StringToHash("Speed");
    private static readonly int HashGround   = Animator.StringToHash("IsGrounded");
    private static readonly int HashAttack   = Animator.StringToHash("Attack");
    private static readonly int HashRoar     = Animator.StringToHash("Roar");

    // ══════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _cc     = GetComponent<CharacterController>();
        _anim   = GetComponent<Animator>();
        _health = GetComponent<DinoHealth>();

        ApplyDinoProfile();
    }

    private void Update()
    {
        if (_health != null && _health.IsDead) return;

        UpdateGrounded();
        HandleMovement();
        HandleAttack();
        HandleRoar();
        ApplyGravity();
    }

    // ══════════════════════════════════════════════════════════════
    // Métodos privados
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Aplica stats según el tipo de dinosaurio seleccionado.
    /// Se llama en Awake para que los valores del Inspector queden sobrescritos.
    /// </summary>
    private void ApplyDinoProfile()
    {
        switch (dinoType)
        {
            case DinoType.TRex:
                walkSpeed      = 4f;
                runSpeed       = 10f;
                attackDamage   = 40f;
                attackRange    = 3f;
                attackCooldown = 1.5f;
                break;

            case DinoType.Velociraptor:
                walkSpeed      = 7f;
                runSpeed       = 18f;
                attackDamage   = 20f;
                attackRange    = 2f;
                attackCooldown = 0.7f;
                break;
        }
    }

    private void UpdateGrounded()
    {
        _isGrounded = _cc.isGrounded;
        if (_isGrounded && _vertVelocity.y < 0f)
            _vertVelocity.y = -2f;          // pequeño push hacia el suelo

        _anim.SetBool(HashGround, _isGrounded);
    }

    private void HandleMovement()
    {
        // ── Leer ejes ──────────────────────────────────────────────
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool  run = Input.GetKey(KeyCode.LeftShift) && v > 0.1f;

        // ── Dirección relativa a la cámara ─────────────────────────
        Vector3 move = Vector3.zero;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 fwd   = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.transform.right,   Vector3.up).normalized;
            move = fwd * v + right * h;
        }
        else
        {
            move = new Vector3(h, 0f, v);
        }

        // ── Rotación suave hacia la dirección de movimiento ────────
        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // ── Aplicar desplazamiento ─────────────────────────────────
        float speed = run ? runSpeed : walkSpeed;
        _cc.Move(move.normalized * (speed * Time.deltaTime));

        // ── Animator: Speed normalizado [0, 1] ────────────────────
        float speedNorm = Mathf.Clamp01(move.magnitude) * (run ? 1f : 0.5f);
        _anim.SetFloat(HashSpeed, speedNorm, 0.1f, Time.deltaTime);
    }

    private void HandleAttack()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time - _lastAttackTime < attackCooldown) return;

        _lastAttackTime = Time.time;
        _anim.SetTrigger(HashAttack);

        // Detectar objetivos en el radio de ataque
        if (attackPoint != null)
        {
            Collider[] hits = Physics.OverlapSphere(
                attackPoint.position, attackRange, enemyLayer);

            foreach (Collider col in hits)
            {
                if (col.TryGetComponent<DinoHealth>(out var hp))
                    hp.TakeDamage(attackDamage);
            }
        }

        if (attackEffect != null)
            attackEffect.Play();
    }

    private void HandleRoar()
    {
        if (Input.GetKeyDown(KeyCode.R))
            _anim.SetTrigger(HashRoar);
    }

    private void ApplyGravity()
    {
        _vertVelocity.y += gravity * Time.deltaTime;
        _cc.Move(_vertVelocity * Time.deltaTime);
    }

    // ══════════════════════════════════════════════════════════════
    // Gizmos (visibles solo en el Editor)
    // ══════════════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
