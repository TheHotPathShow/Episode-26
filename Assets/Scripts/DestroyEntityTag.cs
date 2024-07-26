using Unity.Entities;

namespace TMG.RacingRoyale
{
    public struct DestroyEntityTag : IComponentData {}

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach (var (_, entity) in SystemAPI.Query<DestroyEntityTag>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}