using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public struct GameProperties : IComponentData
    {
        public float CountdownTimer;
        public int AICount;
        public float BeginNextEliminationTimer;
        public float EliminationTimer;
        public int EliminationIndex;
        // This number is indicative of the number of positions shown to the player at a given time, not the total number of racers on the track.
        public int MaxPositionCount;
    }

    [Serializable]
    public struct EliminationProperties
    {
        public int MaxPositions;
        public float BeginNextEliminationTimeout;
        public float EliminationTimeout;
    }

    public struct EliminationPropertiesBufferElement : IBufferElementData
    {
        public EliminationProperties Value;
    }
    
    public struct GamePlayingTag : IComponentData {}
    
    public class GameControllerAuthoring : MonoBehaviour
    {
        public int CountdownLength;
        public int AICount;
        public EliminationProperties[] EliminationProperties;
        
        public class GameControllerBaker : Baker<GameControllerAuthoring>
        {
            public override void Bake(GameControllerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GameProperties
                {
                    CountdownTimer = authoring.CountdownLength,
                    AICount = authoring.AICount,
                    BeginNextEliminationTimer = authoring.EliminationProperties[0].BeginNextEliminationTimeout,
                    EliminationTimer = authoring.EliminationProperties[0].EliminationTimeout
                });
                var eliminationPropertiesBuffer = AddBuffer<EliminationPropertiesBufferElement>(entity);

                foreach (var eliminationProperty in authoring.EliminationProperties)
                {
                    eliminationPropertiesBuffer.Add(new EliminationPropertiesBufferElement { Value = eliminationProperty });
                }
            }
        }
    }

    public partial struct CountdownSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameProperties>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if (HUDController.Instance == null) return;
            var deltaTime = SystemAPI.Time.DeltaTime;

            var gameProperties = SystemAPI.GetSingletonRW<GameProperties>();
            gameProperties.ValueRW.CountdownTimer -= deltaTime;
            var countdown = (int)math.ceil(gameProperties.ValueRO.CountdownTimer);

            var countdownString = "";
            switch (countdown)
            {
                case 3:
                    countdownString = $"<color=red>{countdown.ToString()}</color>";
                    break;
                
                case 2:
                    countdownString = $"<color=yellow>{countdown.ToString()}</color>";
                    break;
                
                case 1:
                    countdownString = $"<color=green>{countdown.ToString()}</color>";
                    break;
                
                case 0:
                    countdownString = "<color=red>G</color><color=yellow>O</color><color=green>!</color>";
                    if (!SystemAPI.HasSingleton<GamePlayingTag>())
                    {
                        var gameControllerEntity = SystemAPI.GetSingletonEntity<GameProperties>();
                        state.EntityManager.AddComponent<GamePlayingTag>(gameControllerEntity);
                    }
                    break;
                
                default:
                    state.Enabled = false;
                    break;
            }
            
            foreach (var hudControllerReference in SystemAPI.Query<HUDControllerReference>())
            {
                hudControllerReference.Value.Value.SetCenterScreenMessageText(countdownString);
            }
        }
    }

    public struct RacePositionHelper
    {
        public RefRW<RacePosition> RacePosition;
        public float TPosition;
        public int LapNumber;
    }
    
    public partial struct PositionDeterminationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldBlob>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var flowField = SystemAPI.GetSingleton<FlowFieldBlob>();

            var newPositionList = new NativeList<RacePositionHelper>(Allocator.Temp);

            foreach (var (racePosition, lapCounter, transform) in SystemAPI.Query<RefRW<RacePosition>, LapCounter, LocalTransform>())
            {
                var cell = flowField.GetCellAtPosition(transform.Position);
                var tPosition = cell.TPosition;

                var insertIndex = 0;
                for (; insertIndex < newPositionList.Length; insertIndex++)
                {
                    var racePositionHelper = newPositionList[insertIndex];
                    if (lapCounter.LapNumber > racePositionHelper.LapNumber || (lapCounter.LapNumber >= racePositionHelper.LapNumber && tPosition > racePositionHelper.TPosition))
                    {
                        break;
                    }
                }

                newPositionList.InsertRange(insertIndex, 1);
                newPositionList[insertIndex] = new RacePositionHelper
                {
                    RacePosition = racePosition,
                    LapNumber = lapCounter.LapNumber,
                    TPosition = tPosition
                };
            }

            for (var i = 0; i < newPositionList.Length; i++)
            {
                var racePositionHelper = newPositionList[i];
                racePositionHelper.RacePosition.ValueRW.Value = i + 1;
            }
        }
    }

    public partial struct VehicleEliminationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameProperties>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameProperties = SystemAPI.GetSingletonRW<GameProperties>();
            var gamePropertiesEntity = SystemAPI.GetSingletonEntity<GameProperties>();
            var eliminationBuffer = SystemAPI.GetBuffer<EliminationPropertiesBufferElement>(gamePropertiesEntity);
            var eliminationElement = eliminationBuffer[gameProperties.ValueRO.EliminationIndex];
            var nextEliminationIndex = math.min(gameProperties.ValueRO.EliminationIndex + 1, eliminationBuffer.Length - 1);
            var nextEliminationElement = eliminationBuffer[nextEliminationIndex];
            gameProperties.ValueRW.MaxPositionCount = eliminationElement.Value.MaxPositions;
            var deltaTime = SystemAPI.Time.DeltaTime;

            if (gameProperties.ValueRO.BeginNextEliminationTimer > 0f)
            {
                gameProperties.ValueRW.BeginNextEliminationTimer -= deltaTime;
                HUDController.Instance.SetEliminationTimerText(0f, 0, 0);
            }
            else
            {
                gameProperties.ValueRW.EliminationTimer -= deltaTime;
                var maxPositions = eliminationElement.Value.MaxPositions;
                var minPositions = nextEliminationElement.Value.MaxPositions + 1;
                
                HUDController.Instance.SetEliminationTimerText(gameProperties.ValueRO.EliminationTimer, minPositions, maxPositions);
            }

            if (gameProperties.ValueRO.EliminationTimer > 0) return;
            gameProperties.ValueRW.EliminationIndex++;
            eliminationElement = eliminationBuffer[gameProperties.ValueRO.EliminationIndex];

            var maxPosition = eliminationElement.Value.MaxPositions;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var hudUILookup = SystemAPI.GetComponentLookup<HUDControllerReference>();
            var leadEntity = Entity.Null;
            
            foreach (var (racePosition, entity) in SystemAPI.Query<RacePosition>().WithEntityAccess())
            {
                if (racePosition.Value == 1)
                {
                    leadEntity = entity;
                }
                
                if (racePosition.Value > maxPosition)
                {
                    ecb.AddComponent<DestroyEntityTag>(entity);
                    if (hudUILookup.TryGetComponent(entity, out var hudControllerReference))
                    {
                        hudControllerReference.Value.Value.SetCenterScreenMessageText($"<color=red>ELIMINATED @POS {racePosition.Value}</color>");
                        hudControllerReference.Value.Value.ShowHideMenuButton(true);
                    }
                }
            }
            
            if (eliminationElement.Value.MaxPositions == 1)
            {
                if (hudUILookup.TryGetComponent(leadEntity, out var hudControllerReference))
                {
                    hudControllerReference.Value.Value.SetCenterScreenMessageText("<color=green>FAMILY ROYALE</color>");
                    hudControllerReference.Value.Value.ShowHideMenuButton(true);
                    hudControllerReference.Value.Value.ShowDomImage();
                }
                gameProperties.ValueRW.MaxPositionCount = 1;
                state.Enabled = false;
            }

            gameProperties.ValueRW.BeginNextEliminationTimer = eliminationElement.Value.BeginNextEliminationTimeout;
            gameProperties.ValueRW.EliminationTimer = eliminationElement.Value.EliminationTimeout;
            
            ecb.Playback(state.EntityManager);
        }
    }
}