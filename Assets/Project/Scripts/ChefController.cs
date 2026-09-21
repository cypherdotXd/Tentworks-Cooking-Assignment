using System;
using UnityEngine;

public class ChefController : StateMachineBehaviour<ChefController>
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public float RotationSpeed
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }

    public Rigidbody Rigidbody { get; private set; }
    public InputActions InputActions { get; private set; }

    protected virtual void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        if (Rigidbody != null)
        {
            Rigidbody.freezeRotation = true;
        }

        InputActions = new InputActions();
    }

    protected virtual void OnEnable()
    {
        InputActions?.Player.Enable();
    }

    protected virtual void OnDisable()
    {
        InputActions?.Player.Disable();
    }

    protected virtual void OnDestroy()
    {
        InputActions?.Dispose();
    }

    protected override void Start()
    {
        if (CurrentState == null)
        {
            SwitchState(new IdleState(this));
        }

        base.Start();
    }

    void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused))
        {
            return;
        }

        UpdateState();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused))
        {
            if (Rigidbody != null)
            {
                Vector3 vel = Rigidbody.linearVelocity;
                Rigidbody.linearVelocity = new Vector3(0f, vel.y, 0f);
            }
            return;
        }

        FixedUpdateState();
    }

    public Vector2 GetMoveInput()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameActive || GameManager.Instance.IsPaused))
        {
            return Vector2.zero;
        }

        return InputActions != null ? InputActions.Player.Move.ReadValue<Vector2>() : Vector2.zero;
    }
}

public class IdleState : StateBase<ChefController>
{
    public IdleState(ChefController stateMachine) : base(stateMachine)
    {
    }

    public override void EnterState()
    {
        Vector3 vel = StateMachine.Rigidbody.linearVelocity;
        StateMachine.Rigidbody.linearVelocity = new Vector3(0f, vel.y, 0f);

        AddTransition(new MoveState(StateMachine), machine =>
        {
            return machine.GetMoveInput().sqrMagnitude > 0.001f;
        });
    }

    public override void UpdateState()
    {
        TryTransition(StateMachine);
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
    }
}

public class MoveState : StateBase<ChefController>
{
    public MoveState(ChefController stateMachine) : base(stateMachine)
    {
    }

    public override void EnterState()
    {
        AddTransition(new IdleState(StateMachine), machine =>
        {
            return machine.GetMoveInput().sqrMagnitude <= 0.001f;
        });
    }

    public override void UpdateState()
    {
        TryTransition(StateMachine);
    }

    public override void FixedUpdateState()
    {
        Vector2 input = StateMachine.GetMoveInput();
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        if (moveDir.sqrMagnitude < 0.001f) return;
        
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        // Rotate smoothly towards movement direction
        Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
        StateMachine.transform.rotation = Quaternion.Slerp(
            StateMachine.transform.rotation,
            targetRotation,
            StateMachine.RotationSpeed * Time.fixedDeltaTime
        );

        // Move via Rigidbody
        if (StateMachine.Rigidbody != null)
        {
            Vector3 newVelocity = moveDir * StateMachine.MoveSpeed;
            newVelocity.y = StateMachine.Rigidbody.linearVelocity.y;
            StateMachine.Rigidbody.linearVelocity = newVelocity;
        }
        else
        {
            StateMachine.transform.position += moveDir * (StateMachine.MoveSpeed * Time.fixedDeltaTime);
        }

    }

    public override void ExitState()
    {
        if (StateMachine.Rigidbody != null)
        {
            Vector3 vel = StateMachine.Rigidbody.linearVelocity;
            StateMachine.Rigidbody.linearVelocity = new Vector3(0f, vel.y, 0f);
        }
    }
}