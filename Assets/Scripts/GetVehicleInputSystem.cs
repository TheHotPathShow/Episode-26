using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.RacingRoyale
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class GetVehicleInputSystem : SystemBase
    {
        private RacingInputActions _inputActions;
        
        protected override void OnCreate()
        {
            RequireForUpdate<FlowFieldBlob>();
            RequireForUpdate<GamePlayingTag>();
            
            _inputActions = new RacingInputActions();
            _inputActions.Enable();
        }

        protected override void OnUpdate()
        {
            // Get Player Input from input actions
            var moveInput = _inputActions.Gameplay.Movement.ReadValue<Vector2>();
            
            foreach (var vehicleInput in SystemAPI.Query<RefRW<VehicleInput>>().WithAll<PlayerControlledTag>())
            {
                vehicleInput.ValueRW.Movement = moveInput;
            }

            // Get AI controlled input from flow field
            var flowField = SystemAPI.GetSingleton<FlowFieldBlob>();

            foreach (var (vehicleInput, vehicleProperties, transform) in SystemAPI.Query<RefRW<VehicleInput>, VehicleProperties, LocalTransform>().WithAll<AIControlledTag>())
            {
                var cell = flowField.GetCellAtPosition(transform.Position);
                var cellForward = cell.Direction;

                var steer = quaternion.AxisAngle(math.up(), math.radians(vehicleProperties.CurSteering));
                var steerDirection = math.rotate(steer, math.forward());

                var angle = math.cross(steerDirection, cellForward);
                vehicleInput.ValueRW.Movement = new float2(math.sign(angle.y), 1f);
            }
        }
        
        protected override void OnDestroy()
        {
            _inputActions.Disable();
        }
    }
}