using UnityEngine;

public class Movecontroller : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 7f;          // 할로우 나이트 스타일: 빠른 이동
    public bool airControl = true;

    [Header("Dash")]
    public float dashSpeed = 12f;         // 대쉬 속도 (낮춰서 이동거리 감소)
    public float dashDuration = 0.8f;     // 대쉬 지속 시간 (애니메이션 길이)
    public float dashCooldown = 1f;       // 대쉬 쿨다운

    [Header("Jump")]
    public float jumpForce = 15f;         // 할로우 나이트 스타일: 높은 점프
    public int extraJumps = 1;            // 더블 점프 활성화
    public float coyoteTime = 0.15f;      // 관대한 코요테 타임
    public float jumpBufferTime = 0.15f;  // 관대한 점프 버퍼
    public float shortJumpCut = 0.3f;     // 할로우 나이트 스타일: 정밀한 점프 제어
    public float fallMultiplier = 2.5f;   // 낙하 시 중력 배수
    public float lowJumpMultiplier = 2f;  // 낮은 점프 시 중력 배수

    [Header("Ground Check")]
    public LayerMask groundLayer;

    [Header("Attack")]
    public GameObject attackHitbox;       // 자식 오브젝트(Trigger Collider 2D 포함)
    public float attackDuration = 0.4f;   // 할로우 나이트 스타일: 빠른 공격
    public float attackActiveTime = 0.15f; // 히트박스 활성 시간
    public float attackCooldown = 0.1f;   // 약간의 쿨다운
    public bool canAttackInAir = true;    // 공중 공격 가능

    [Header("Attack2 (Projectile)")]
    public GameObject projectilePrefab;   // 발사할 투사체 프리팹
    public Transform projectileSpawnPoint; // 투사체 생성 위치 (없으면 캐릭터 위치)
    public float projectileSpeed = 10f;   // 투사체 속도
    public float attack2Duration = 0.5f;  // Attack2 애니메이션 지속 시간
    public float attack2Cooldown = 1f;    // Attack2 쿨다운
    public bool canAttack2InAir = true;   // 공중에서 Attack2 가능

    [Header("Animation Params")]
    public string speedParam = "Speed";
    public string groundedParam = "isGrounded";
    public string attackTrigger = "Attack";
    public string attack2Trigger = "Attack2";
    public string dashTrigger = "Dash";

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    // internal
    bool isGrounded;
    bool isFacingRight = true;
    float coyoteCounter;
    float jumpBufferCounter;
    int jumpsLeft;            // 남은 추가 점프 횟수
    bool isAttacking = false; // 공격 중인지 여부
    float attackEndTime = 0f; // 공격 종료 시간
    float nextAttackTime = 0f; // 다음 공격 가능 시간
    bool isAttacking2 = false; // Attack2 공격 중인지 여부
    float attack2EndTime = 0f; // Attack2 종료 시간
    float nextAttack2Time = 0f; // 다음 Attack2 가능 시간
    bool isDashing = false;   // 대쉬 중인지 여부
    float dashEndTime = 0f;   // 대쉬 종료 시간
    float nextDashTime = 0f;  // 다음 대쉬 가능 시간
    int groundContactCount = 0;  // 바닥과 접촉 횟수

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        if (attackHitbox) attackHitbox.SetActive(false);
    }

    void Update()
    {
        // --- 대쉬 상태 체크 ---
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
        }

        // --- 공격 상태 체크 ---
        if (isAttacking && Time.time >= attackEndTime)
        {
            isAttacking = false;
        }

        // --- Attack2 상태 체크 ---
        if (isAttacking2 && Time.time >= attack2EndTime)
        {
            isAttacking2 = false;
        }

        // --- 입력 ---
        float x = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpReleased = Input.GetButtonUp("Jump");
        bool attackPressed = Input.GetButtonDown("Fire1");
        bool attack2Pressed = Input.GetKeyDown(KeyCode.E);
        bool dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);

        // --- 땅 체크 (Collision 기반) ---
        isGrounded = groundContactCount > 0;

        // 코요테 / 점프버퍼 카운터 갱신
        coyoteCounter = isGrounded ? coyoteTime : Mathf.Max(0, coyoteCounter - Time.deltaTime);
        jumpBufferCounter = jumpPressed ? jumpBufferTime : Mathf.Max(0, jumpBufferCounter - Time.deltaTime);

        // 땅에 닿으면 추가점프 회복
        if (isGrounded) jumpsLeft = extraJumps;

        // --- 점프 처리 (공격 중에도 가능 - 할로우 나이트 스타일) ---
        if (jumpBufferCounter > 0f && (coyoteCounter > 0f || jumpsLeft > 0))
        {
            DoJump();
            jumpBufferCounter = 0f;
        }

        // 가변 점프: 키를 일찍 떼면 더 낮게 뜀
        if (jumpReleased && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * shortJumpCut);
        }

        // 할로우 나이트 스타일: 더 나은 점프 느낌
        BetterJump();

        // --- 대쉬 처리 ---
        if (dashPressed && !isDashing && Time.time >= nextDashTime)
        {
            StartCoroutine(PerformDash());
        }

        // --- 이동 처리 (대쉬 중이 아닐 때만) ---
        if (!isDashing)
        {
            bool canMove = isGrounded || airControl;
            float targetVelX = canMove ? x * moveSpeed : rb.velocity.x;
            rb.velocity = new Vector2(targetVelX, rb.velocity.y);
        }

        // --- 방향 전환 (공격 중이 아닐 때만) ---
        if (!isAttacking)
        {
            if (x > 0 && !isFacingRight) Flip();
            else if (x < 0 && isFacingRight) Flip();
        }

        // --- 애니메이션 파라미터 (대쉬 중이 아닐 때만) ---
        if (anim && !isDashing)
        {
            anim.SetFloat(speedParam, Mathf.Abs(rb.velocity.x));
            anim.SetBool(groundedParam, isGrounded);
        }

        // --- 공격 처리 ---
        if (attackPressed && !isAttacking && Time.time >= nextAttackTime)
        {
            if (canAttackInAir || isGrounded)
            {
                // 공격 즉시 시작
                isAttacking = true;
                attackEndTime = Time.time + attackDuration;
                nextAttackTime = Time.time + attackDuration + attackCooldown;

                // 애니메이션 및 히트박스 처리
                StartCoroutine(AttackAnimation());
            }
        }

        // --- Attack2 처리 (E키 - 투사체 발사) ---
        if (attack2Pressed && !isAttacking2 && Time.time >= nextAttack2Time)
        {
            if (canAttack2InAir || isGrounded)
            {
                // Attack2 시작
                isAttacking2 = true;
                attack2EndTime = Time.time + attack2Duration;
                nextAttack2Time = Time.time + attack2Duration + attack2Cooldown;

                // 애니메이션 및 투사체 발사
                StartCoroutine(Attack2Animation());
            }
        }
    }

    void DoJump()
    {
        // 코요테 점프 우선, 아니면 추가점프 소모
        if (coyoteCounter <= 0f && jumpsLeft > 0) jumpsLeft--;

        rb.velocity = new Vector2(rb.velocity.x, 0f); // 일관된 점프 위해 Y속도 리셋
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // 점프 직후 코요테/버퍼 무효화
        coyoteCounter = 0f;
    }

    void BetterJump()
    {
        // 할로우 나이트 스타일: 낙하 시 더 빠르게, 상승 시 더 부드럽게
        if (rb.velocity.y < 0)
        {
            // 낙하 중: 중력 증가 (더 빠른 낙하)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            // 점프 키를 떼고 상승 중: 중력 증가 (낮은 점프)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    System.Collections.IEnumerator PerformDash()
    {
        // 대쉬 시작
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashDuration + dashCooldown;

        // 대쉬 애니메이션 트리거
        if (anim && !string.IsNullOrEmpty(dashTrigger))
        {
            anim.SetTrigger(dashTrigger);
        }

        // 대쉬 방향 결정
        float dashDirection = isFacingRight ? 1f : -1f;

        // 대쉬 중 중력 무시
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        // 대쉬 실행
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        // 중력 복구
        rb.gravityScale = originalGravity;

        // 대쉬 종료는 Update에서 dashEndTime 체크로 처리
    }

    System.Collections.IEnumerator AttackAnimation()
    {
        // 애니메이션 트리거 (즉시 실행)
        if (anim && !string.IsNullOrEmpty(attackTrigger))
        {
            anim.SetTrigger(attackTrigger);
        }

        // 히트박스 즉시 활성화
        if (attackHitbox)
        {
            attackHitbox.SetActive(true);
            yield return new WaitForSeconds(attackActiveTime);
            attackHitbox.SetActive(false);
        }

        // 공격 종료는 Update에서 attackEndTime 체크로 처리
    }

    System.Collections.IEnumerator Attack2Animation()
    {
        // 애니메이션 트리거
        if (anim && !string.IsNullOrEmpty(attack2Trigger))
        {
            anim.SetTrigger(attack2Trigger);
        }

        // 약간의 딜레이 후 투사체 발사 (애니메이션과 맞추기)
        yield return new WaitForSeconds(0.2f);

        // 투사체 발사
        FireProjectile();
    }

    void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile Prefab이 할당되지 않았습니다!");
            return;
        }

        // 발사 위치 결정
        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        // 투사체 생성
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // 투사체에 Rigidbody2D가 있으면 속도 설정
        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            // 캐릭터가 보는 방향으로 발사
            float direction = isFacingRight ? 1f : -1f;
            projRb.velocity = new Vector2(direction * projectileSpeed, 0f);
        }

        // 투사체 방향 설정 (Sprite 뒤집기)
        if (!isFacingRight)
        {
            Vector3 scale = projectile.transform.localScale;
            scale.x *= -1f;
            projectile.transform.localScale = scale;
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    // Collision 기반 바닥 감지
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsGroundLayer(collision.gameObject))
        {
            groundContactCount++;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (IsGroundLayer(collision.gameObject))
        {
            groundContactCount--;
        }
    }

    bool IsGroundLayer(GameObject obj)
    {
        return ((1 << obj.layer) & groundLayer) != 0;
    }
}
