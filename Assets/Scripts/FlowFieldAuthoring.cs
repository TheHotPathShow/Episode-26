using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace TMG.RacingRoyale
{
    public struct FlowFieldCell
    {
        public float3 Position;
        public float3 Direction;
        public float TPosition;
    }

    public struct FlowField
    {
        public BlobArray<FlowFieldCell> Cells;
    }

    public struct FlowFieldBlob : IComponentData
    {
        public BlobAssetReference<FlowField> Value;
        public int2 GridSize;
        public float CellSize;
        public float2 Center;
        public float MaxDistance;

        private float2 HalfGridDimensions => new(GridSize.x * CellSize * 0.5f, GridSize.y * CellSize * 0.5f);

        private readonly FlowFieldCell this[int2 coords] => Value.Value.Cells[GridSize.y * coords.y + coords.x];

        public FlowFieldCell GetCellAtPosition(float3 position)
        {
            var normalizedPosition = position.xz - Center + HalfGridDimensions;
            var cellCoordinates = new int2
            {
                x = (int)math.floor(normalizedPosition.x / CellSize),
                y = (int)math.floor(normalizedPosition.y / CellSize)
            };

            return this[cellCoordinates];
        }
    }

    public class FlowFieldAuthoring : MonoBehaviour
    {
        public float2 MapDimensions;
        public float CellSize;
        public SplineContainer Spline;
        public float MaxDistance;
        
        public class FlowFieldBaker : Baker<FlowFieldAuthoring>
        {
            public override void Bake(FlowFieldAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var gridSize = new int2
                {
                    x = (int)math.ceil(authoring.MapDimensions.x / authoring.CellSize),
                    y = (int)math.ceil(authoring.MapDimensions.y / authoring.CellSize)
                };
                
                var totalSize = new float2
                {
                    x = gridSize.x * authoring.CellSize,
                    y = gridSize.y * authoring.CellSize
                };

                var halfGridSize = new float3(totalSize.x * 0.5f, 0f, totalSize.y * 0.5f);
                var halfCellSize = new float3(authoring.CellSize * 0.5f, 0f, authoring.CellSize * 0.5f);
            
                var builder = new BlobBuilder(Allocator.Temp);
                ref var flowField = ref builder.ConstructRoot<FlowField>();
                
                var arrayBuilder = builder.Allocate(ref flowField.Cells, gridSize.x * gridSize.y);
                for (var x = 0; x < gridSize.x; x++)
                {
                    for (var y = 0; y < gridSize.y; y++)
                    {
                        var position = new float3(x * authoring.CellSize, 0f, y * authoring.CellSize);
                        position -= halfGridSize;
                        position += halfCellSize;
                                            
                        var distanceToClosestPoint = SplineUtility.GetNearestPoint(authoring.Spline.Spline, position, out var closestPoint, out var t, 64, 64);
                        var closestSplineDirection = math.normalize(authoring.Spline.Spline.EvaluateTangent(t));
                        var directionToClosestPoint = math.normalize(closestPoint - position);

                        var distanceInterpolationUnit = math.unlerp(0f, authoring.MaxDistance, distanceToClosestPoint);
                        distanceInterpolationUnit = math.clamp(distanceInterpolationUnit, 0, 1);
                        var cellDirection = math.lerp(closestSplineDirection, directionToClosestPoint, distanceInterpolationUnit);
                        
                        arrayBuilder[gridSize.y * y + x] = new FlowFieldCell
                        {
                            Position = position,
                            Direction = cellDirection,
                            TPosition = t
                        };
                    }
                }

                var reference = builder.CreateBlobAssetReference<FlowField>(Allocator.Persistent);
                builder.Dispose();

                AddComponent(entity, new FlowFieldBlob
                {
                    Value = reference,
                    GridSize = gridSize,
                    CellSize = authoring.CellSize,
                    Center = ((float3)authoring.transform.position).xz,
                    MaxDistance = authoring.MaxDistance
                });
            }
        }
    }
}