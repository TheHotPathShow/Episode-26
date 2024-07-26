using Unity.Entities;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public struct StartLine : IComponentData
    {
        public int Index;
    }
    
    public class StartLineAuthoring : MonoBehaviour
    {
        public class StartLineBaker : Baker<StartLineAuthoring>
        {
            public override void Bake(StartLineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<StartLine>(entity);
            }
        }
    }

    public partial struct StartLineInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StartLine>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var maxIndex = -1;
            foreach (var checkpoint in SystemAPI.Query<Checkpoint>())
            {
                if (checkpoint.Value > maxIndex)
                {
                    maxIndex = checkpoint.Value;
                } 
            }

            var startLine = SystemAPI.GetSingletonRW<StartLine>();
            startLine.ValueRW.Index = maxIndex + 1;

            foreach (var lapCounter in SystemAPI.Query<RefRW<LapCounter>>())
            {
                lapCounter.ValueRW.LastCheckpointIndex = maxIndex;
            }
        }
    }
}