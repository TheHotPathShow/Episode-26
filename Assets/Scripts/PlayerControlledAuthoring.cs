using Unity.Entities;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public struct PlayerControlledTag : IComponentData {}
    
    public class PlayerControlledAuthoring : MonoBehaviour
    {
        public class PlayerControlledBaker : Baker<PlayerControlledAuthoring>
        {
            public override void Bake(PlayerControlledAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<PlayerControlledTag>(entity);
                AddComponent<HUDControllerReference>(entity);
            }
        }
    }
}