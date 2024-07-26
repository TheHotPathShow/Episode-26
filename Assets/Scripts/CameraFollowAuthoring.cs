using Unity.Entities;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public struct CameraFollowTransform : IComponentData
    {
        public bool Initialized;
        public UnityObjectRef<Transform> Value;
    }
    
    public class CameraFollowAuthoring : MonoBehaviour
    {
        public class CameraBaker : Baker<CameraFollowAuthoring>
        {
            public override void Bake(CameraFollowAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CameraFollowTransform>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct InitializeCameraFollowSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraFollowTransform>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if (CameraTarget.Instance == null) return;
            state.Enabled = false;
            var cameraFollowTransform = SystemAPI.GetSingletonRW<CameraFollowTransform>();
            cameraFollowTransform.ValueRW.Value = CameraTarget.Instance.transform;
            cameraFollowTransform.ValueRW.Initialized = true;
        }
    }
}