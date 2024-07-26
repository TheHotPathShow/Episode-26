using System.Collections.Generic;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public class VisualVehicleController : MonoBehaviour
    {
        public static VisualVehicleController Instance;

        [SerializeField] private VisualVehicleProperties _visualVehicleProperties;

        public GameObject[] VehiclePrefabs => _visualVehicleProperties.VehiclePrefabs;
        public Material[] VehicleMaterials => _visualVehicleProperties.VehicleMaterials;
        
        public int VehiclePrefabCount => VehiclePrefabs.Length;
        public int VehicleMaterialCount => VehicleMaterials.Length;

        private int _playerVehicle;
        private HashSet<int> _existingVehicles;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _existingVehicles = new HashSet<int>();
        }

        public void SetPlayerVehicle(int vehicleIndex, int materialIndex)
        {
            _playerVehicle = ConvertToHash(vehicleIndex, materialIndex);
        }

        public void GetPlayerIndices(out int vehicleIndex, out int materialIndex)
        {
            ConvertFromHash(_playerVehicle, out vehicleIndex, out materialIndex);
        }
        
        public void GetPlayerVehicle(out GameObject vehiclePrefab, out Material vehicleMaterial)
        {
            ConvertFromHash(_playerVehicle, out var vehicleIndex, out var materialIndex);
            vehiclePrefab = VehiclePrefabs[vehicleIndex];
            vehicleMaterial = VehicleMaterials[materialIndex];
        }

        public void GetNextAIVehicle(out GameObject vehiclePrefab, out Material vehicleMaterial)
        {
            int vehicleIndex, materialIndex;
            do
            {
                vehicleIndex = Random.Range(0, VehiclePrefabCount);
                materialIndex = Random.Range(0, VehicleMaterialCount);
            } while (DoesVehicleExist(vehicleIndex, materialIndex));
            
            var hash = ConvertToHash(vehicleIndex, materialIndex);
            _existingVehicles.Add(hash);
            
            vehiclePrefab = VehiclePrefabs[vehicleIndex];
            vehicleMaterial = VehicleMaterials[materialIndex];
        }

        private bool DoesVehicleExist(int vehicleIndex, int materialIndex)
        {
            var hash = ConvertToHash(vehicleIndex, materialIndex);
            return hash == _playerVehicle || _existingVehicles.Contains(hash);
        }

        private int ConvertToHash(int vehicleIndex, int materialIndex)
        {
            vehicleIndex *= 100;
            return vehicleIndex + materialIndex;
        }

        private void ConvertFromHash(int hash, out int vehicleIndex, out int materialIndex)
        {
            vehicleIndex = Mathf.FloorToInt(hash / 100f);
            materialIndex = hash - vehicleIndex * 100;
        }

        public void ResetController()
        {
            _playerVehicle = 0;
            _existingVehicles.Clear();
        }
    }
}