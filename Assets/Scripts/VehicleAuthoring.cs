using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine;
using Unity.Burst;
using Unity.Transforms;
using Material = UnityEngine.Material;

namespace TMG.RacingRoyale 
{
    public struct VehicleInput : IComponentData
    {
        public float2 Movement;
    }
    
    public struct VehicleProperties : IComponentData
    {
        public float CurSteering;
        public float TopSpeed;
        public float Acceleration;
        public float Breaking;
        public float Friction;
        public float Steering;
    }

    public struct LapCounter : IComponentData
    {
        public int LastCheckpointIndex;
        public int LapNumber;
        public bool NewLap;
    }

    public struct RacePosition : IComponentData
    {
        public int Value;
    }

    public class VehicleVisuals : ICleanupComponentData
    {
        public GameObject Value;
    }
    
    public class VehicleAuthoring : MonoBehaviour
    {
        public float TopSpeed;
        public float Acceleration;
        public float Breaking;
        public float Friction;
        public float Steering;
        
        public class VehicleBaker : Baker<VehicleAuthoring>
        {
            public override void Bake(VehicleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VehicleProperties
                {
                    CurSteering = 0f,
                    TopSpeed = authoring.TopSpeed,
                    Acceleration = authoring.Acceleration,
                    Breaking = authoring.Breaking,
                    Friction = authoring.Friction,
                    Steering = authoring.Steering
                });
                
                AddComponent(entity, new LapCounter
                {
                    LastCheckpointIndex = -1,
                    LapNumber = 0
                });
                
                AddComponent<RacePosition>(entity);
                AddComponent<VehicleInput>(entity);
            }
        }
    }

    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct VehicleMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePlayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (vehicleProperties, velocity, vehicleInput, mass, transform) in SystemAPI.Query<RefRW<VehicleProperties>, RefRW<PhysicsVelocity>, VehicleInput, PhysicsMass, LocalTransform>())
            {
                if (math.abs(vehicleInput.Movement.x) > float.Epsilon)
                {
                    vehicleProperties.ValueRW.CurSteering += vehicleInput.Movement.x * vehicleProperties.ValueRO.Steering * deltaTime;
                }

                if (math.abs(vehicleInput.Movement.y) > float.Epsilon)
                {
                    var curAcceleration = vehicleProperties.ValueRO.Acceleration * vehicleInput.Movement.y * deltaTime;
                    var rotation = new float3(curAcceleration, 0f, 0f);
                    var steering = quaternion.AxisAngle(math.up(), math.radians(vehicleProperties.ValueRO.CurSteering));
                    rotation = math.rotate(steering, rotation);
                    var worldSpace = math.mul(transform.Rotation, mass.InertiaOrientation);
                    var roto = math.rotate(math.inverse(worldSpace), rotation);
                    velocity.ValueRW.ApplyAngularImpulse(mass, roto);
                }
                else
                {
                    var curVelocity = velocity.ValueRO.Angular;
                    var reverseVelocity = curVelocity * -1f;
                    var stepVelocity = reverseVelocity * deltaTime * vehicleProperties.ValueRO.Friction;
                    velocity.ValueRW.ApplyAngularImpulse(mass, stepVelocity);
                }

                if (math.length(velocity.ValueRO.Angular) > vehicleProperties.ValueRO.TopSpeed)
                {
                    var newAngularVelocity = math.normalize(velocity.ValueRO.Angular) * vehicleProperties.ValueRO.TopSpeed;
                    velocity.ValueRW.Angular = newAngularVelocity;
                }
            }
        }
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct VehicleRendererSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<RacingPrefabs>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in SystemAPI.Query<VehicleProperties>().WithNone<VehicleVisuals>().WithEntityAccess())
            {
                GameObject vehicleVisuals;
                Material vehicleMaterial;

                if (SystemAPI.HasComponent<PlayerControlledTag>(entity))
                {
                    VisualVehicleController.Instance.GetPlayerVehicle(out var vehicleVisualsPrefab, out vehicleMaterial);
                    vehicleVisuals = Object.Instantiate(vehicleVisualsPrefab);
                }
                else
                {
                    VisualVehicleController.Instance.GetNextAIVehicle(out var vehicleVisualsPrefab, out vehicleMaterial);
                    vehicleVisuals = Object.Instantiate(vehicleVisualsPrefab);
                }

                var asdf = vehicleVisuals.GetComponent<VisualVehicleMaterialChange>();
                asdf.SetNewMaterial(vehicleMaterial);
                ecb.AddComponent(entity, new VehicleVisuals { Value = vehicleVisuals });
            }

            foreach (var (vehicleProperties, transform, vehicleVisuals, entity) in SystemAPI.Query<VehicleProperties, LocalTransform, VehicleVisuals>().WithEntityAccess())
            {
                var vsteering = quaternion.AxisAngle(math.up(), math.radians(vehicleProperties.CurSteering));
                var visualTransform = vehicleVisuals.Value.transform;
                visualTransform.SetPositionAndRotation(math.lerp(visualTransform.position, transform.Position - (math.up() * 2.5f), 0.5f + deltaTime), math.slerp(visualTransform.rotation, vsteering, 0.5f + deltaTime));

                if (SystemAPI.HasComponent<CameraFollowTransform>(entity))
                {
                    var cameraFollowTransform = SystemAPI.GetComponent<CameraFollowTransform>(entity);
                    if (!cameraFollowTransform.Initialized) continue;
                    cameraFollowTransform.Value.Value.SetPositionAndRotation(visualTransform.position + (Vector3)math.up(), visualTransform.rotation);
                }
            }

            foreach (var (vehicleVisuals, entity) in SystemAPI.Query<VehicleVisuals>().WithNone<VehicleProperties>().WithEntityAccess())
            {
                Object.Destroy(vehicleVisuals.Value);
                ecb.RemoveComponent<VehicleVisuals>(entity);
            }
        }
    }
}