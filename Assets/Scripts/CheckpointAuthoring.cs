using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public struct Checkpoint : IComponentData
    {
        public int Value;
    }

    public class CheckpointAuthoring : MonoBehaviour
    {
        public int Index;

        public class CheckpointBaker : Baker<CheckpointAuthoring>
        {
            public override void Bake(CheckpointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Checkpoint { Value = authoring.Index });
            }
        }
    }

    public partial struct CheckpointSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
                
            state.Dependency = new CheckpointTriggerJob
            {
                LapCounterLookup = SystemAPI.GetComponentLookup<LapCounter>(),
                CheckpointLookup = SystemAPI.GetComponentLookup<Checkpoint>(true)
            }.Schedule(simulationSingleton, state.Dependency);
            
            state.Dependency = new StartLineTriggerJob
            {
                LapCounterLookup = SystemAPI.GetComponentLookup<LapCounter>(),
                StartLineLookup = SystemAPI.GetComponentLookup<StartLine>(true),
                HUDControllerLookup = SystemAPI.GetComponentLookup<HUDControllerReference>(true)
            }.Schedule(simulationSingleton, state.Dependency);

            state.Dependency.Complete();
        }
    }

    [BurstCompile]
    public struct CheckpointTriggerJob : ITriggerEventsJob
    {
        public ComponentLookup<LapCounter> LapCounterLookup;

        [ReadOnly] public ComponentLookup<Checkpoint> CheckpointLookup;

        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            var vehicleEntity = Entity.Null;
            var checkpointEntity = Entity.Null;

            if (LapCounterLookup.HasComponent(entityA))
            {
                vehicleEntity = entityA;
            }
            else if (LapCounterLookup.HasComponent(entityB))
            {
                vehicleEntity = entityB;
            }

            if (CheckpointLookup.HasComponent(entityA))
            {
                checkpointEntity = entityA;
            }
            else if (CheckpointLookup.HasComponent(entityB))
            {
                checkpointEntity = entityB;
            }

            if (vehicleEntity == Entity.Null || checkpointEntity == Entity.Null) return;

            var checkpointIndex = CheckpointLookup[checkpointEntity].Value;
            var lapCounter = LapCounterLookup[vehicleEntity];

            if (lapCounter.LastCheckpointIndex == checkpointIndex - 1)
            {
                lapCounter.LastCheckpointIndex = checkpointIndex;
            }

            LapCounterLookup[vehicleEntity] = lapCounter;
        }
    }

    [BurstCompile]
    public struct StartLineTriggerJob : ITriggerEventsJob
    {
        public ComponentLookup<LapCounter> LapCounterLookup;

        [ReadOnly] public ComponentLookup<StartLine> StartLineLookup;
        [ReadOnly] public ComponentLookup<HUDControllerReference> HUDControllerLookup;
        
        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            var vehicleEntity = Entity.Null;
            var startLineEntity = Entity.Null;

            if (LapCounterLookup.HasComponent(entityA))
            {
                vehicleEntity = entityA;
            }
            else if (LapCounterLookup.HasComponent(entityB))
            {
                vehicleEntity = entityB;
            }

            if (StartLineLookup.HasComponent(entityA))
            {
                startLineEntity = entityA;
            }
            else if (StartLineLookup.HasComponent(entityB))
            {
                startLineEntity = entityB;
            }

            if (vehicleEntity == Entity.Null || startLineEntity == Entity.Null) return;

            var startLineIndex = StartLineLookup[startLineEntity].Index;
            var lapCounter = LapCounterLookup[vehicleEntity];

            if (lapCounter.LastCheckpointIndex == startLineIndex - 1)
            {
                lapCounter.LastCheckpointIndex = 0;
                lapCounter.LapNumber++;
                lapCounter.NewLap = true;
            }

            LapCounterLookup[vehicleEntity] = lapCounter;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct PlayerUISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameProperties>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var maxPositions = SystemAPI.GetSingleton<GameProperties>().MaxPositionCount;
            
            foreach (var (lapCounter, racePosition, hudControllerReference) in SystemAPI.Query<RefRW<LapCounter>, RacePosition, HUDControllerReference>())
            {
                hudControllerReference.Value.Value.SetPositionText(racePosition.Value, maxPositions);
                if (!lapCounter.ValueRO.NewLap) continue;
                hudControllerReference.Value.Value.SetLapCounterText(lapCounter.ValueRO.LapNumber);
                lapCounter.ValueRW.NewLap = false;
            }
        }
    }
}