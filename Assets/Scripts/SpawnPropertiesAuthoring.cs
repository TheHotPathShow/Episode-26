using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public struct SpawnProperties : IComponentData
    {
        public int ColumnCount;
        public float3 BaseOffset;
        public float3 IntermediateOffset;
    }
    
    public class SpawnPropertiesAuthoring : MonoBehaviour
    {
        public int ColumnCount;
        public float3 BaseOffset;
        public float3 IntermediateOffset;
        
        public class SpawnPropertiesBaker : Baker<SpawnPropertiesAuthoring>
        {
            public override void Bake(SpawnPropertiesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnProperties
                {
                    ColumnCount = authoring.ColumnCount,
                    BaseOffset = authoring.BaseOffset,
                    IntermediateOffset = authoring.IntermediateOffset
                });
            }
        }
    }

    public partial struct SpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RacingPrefabs>();
            state.RequireForUpdate<GameProperties>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var aiCount = SystemAPI.GetSingleton<GameProperties>().AICount;
            var aiVehicleEntityPrefab = SystemAPI.GetSingleton<RacingPrefabs>().AIVehicleEntity;
            var playerVehicleEntityPrefab = SystemAPI.GetSingleton<RacingPrefabs>().PlayerVehicleEntity;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            foreach (var spawnProperties in SystemAPI.Query<SpawnProperties>())
            {
                var curPoint = spawnProperties.BaseOffset;
                var colCount = 0;
                for (var i = 0; i < aiCount; i++)
                {
                    var newVehicleEntity = ecb.Instantiate(aiVehicleEntityPrefab);
                    var newVehicleTransform = LocalTransform.FromPosition(curPoint);
                    ecb.SetComponent(newVehicleEntity, newVehicleTransform);

                    curPoint += spawnProperties.IntermediateOffset;
                    colCount++;
                    if (colCount >= spawnProperties.ColumnCount)
                    {
                        curPoint.x = spawnProperties.BaseOffset.x;
                        colCount = 0;
                    }
                }

                var playerVehicleEntity = ecb.Instantiate(playerVehicleEntityPrefab);
                var playerVehicleTransform = LocalTransform.FromPosition(curPoint);
                ecb.SetComponent(playerVehicleEntity, playerVehicleTransform);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}