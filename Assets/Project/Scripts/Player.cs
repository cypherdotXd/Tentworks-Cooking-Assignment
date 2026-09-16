using UnityEngine;

public class ChefController : StateMachineBehaviour<ChefController>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

// public class IdleState : StateBase<ChefController>
// {
//     private float _currentForwardSpeed;
//     private ChefController _machine;
// 	
//     public IdleState(ChefController stateMachine) : base(stateMachine)
//     {
//         _machine = stateMachine;
//     }
//
//     public override void EnterState()
//     {
//         Debug.Log("Entering IdleState");
//         AddTransition(new MoveState(_machine), _ =>
//         {
//             var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
//             return input.sqrMagnitude > 0.001f;
//         });
//         var animationController = _machine.animationController;
//         animationController.PlayWalkRunAnimation(0, 0.4f);
// 		
//         InputSystem.actions.FindAction("jump").performed += SwitchToJumpState;
//         // InputSystem.actions.FindAction("jump").canceled += SwitchToJumpState;
//     }
//
//     public override void UpdateState()
//     {
//
//         // rb.linearVelocity = Vector3.zero;
//         var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
//         TryTransition(_machine);
//
//     }
//
//     public override void FixedUpdateState()
//     {
// 		
//     }
//
//     public override void ExitState()
//     {
//         InputSystem.actions.FindAction("jump").performed -= SwitchToJumpState;
//         // InputSystem.actions.FindAction("jump").canceled -= SwitchToJumpState;
//     }
// 	
//     private void SwitchToJumpState(InputAction.CallbackContext _)
//     {
//         _machine.SwitchState(new JumpState(_machine, 3.5f));
//     }
// }