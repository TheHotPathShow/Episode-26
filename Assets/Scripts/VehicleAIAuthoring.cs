using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace TMG.RacingRoyale
{
    public struct AIControlledTag : IComponentData {}
    public struct InitializeAIControlledTag : IComponentData {}
    
    public class VehicleAIAuthoring : MonoBehaviour
    {
        public class VehicleAIBaker : Baker<VehicleAIAuthoring>
        {
            public override void Bake(VehicleAIAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<AIControlledTag>(entity);
                AddComponent<InitializeAIControlledTag>(entity);
            }
        }
    }

    public partial struct InitializeVehicleAISystem : ISystem
    {
        private Random _random;

        public void OnCreate(ref SystemState state)
        {
            _random = Random.CreateFromIndex(1000);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (vehicleProperties, entity) in SystemAPI.Query<RefRW<VehicleProperties>>().WithAll<InitializeAIControlledTag>().WithEntityAccess())
            {
                vehicleProperties.ValueRW.TopSpeed += _random.NextFloat(-0.5f, 0.5f);
                vehicleProperties.ValueRW.Acceleration += _random.NextFloat(-5, 5);
                vehicleProperties.ValueRW.Steering += _random.NextFloat(-10, 10);
                ecb.RemoveComponent<InitializeAIControlledTag>(entity);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}