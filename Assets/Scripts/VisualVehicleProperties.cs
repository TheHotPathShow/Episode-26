using UnityEngine;

namespace TMG.RacingRoyale
{
    [CreateAssetMenu(fileName = "VisualVehicleProperties", menuName = "Visual Vehicle Properties", order = 0)]
    public class VisualVehicleProperties : ScriptableObject
    {
        public GameObject[] VehiclePrefabs;
        public Material[] VehicleMaterials;

        public int VehiclePrefabCount => VehiclePrefabs.Length;
        public int VehicleMaterialCount => VehicleMaterials.Length;
    }
}