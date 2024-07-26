using Unity.Entities;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public struct RacingPrefabs : IComponentData
    {
        public Entity AIVehicleEntity;
        public Entity PlayerVehicleEntity;
    }
    
    public class RacingPrefabsAuthoring : MonoBehaviour
    {
        public GameObject AIVehicleEntity;
        public GameObject PlayerVehicleEntity;

        public class RacingPrefabsBaker : Baker<RacingPrefabsAuthoring>
        {
            public override void Bake(RacingPrefabsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new RacingPrefabs
                {
                    AIVehicleEntity = GetEntity(authoring.AIVehicleEntity, TransformUsageFlags.Dynamic),
                    PlayerVehicleEntity = GetEntity(authoring.PlayerVehicleEntity, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}