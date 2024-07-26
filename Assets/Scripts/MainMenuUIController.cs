using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMG.RacingRoyale
{
    public class MainMenuUIController : MonoBehaviour
    {

        [Header("Camera Stuff")] 
        [SerializeField] private Transform _cameraPivotTransform;
        [SerializeField] private float _rotationSpeed;

        [Header("Visual Vehicle Controller")]
        [SerializeField] private VisualVehicleController _visualVehicleController;
        
        [Header("UI Stuff")] 
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TextMeshProUGUI _vehicleSelectText;
        [SerializeField] private Button _previousVehicleButton;
        [SerializeField] private Button _nextVehicleButton;
        [SerializeField] private TextMeshProUGUI _designSelectText;
        [SerializeField] private Button _previousDesignButton;
        [SerializeField] private Button _nextDesignButton;

        private VisualVehicleMaterialChange _currentVehicle;

        private int _vehicleIndex;

        private int _designIndex;

        private void OnEnable()
        {
            _playButton.onClick.AddListener(EnterScene);
            _quitButton.onClick.AddListener(Application.Quit);
            _previousVehicleButton.onClick.AddListener(() => ChangeVehicleModel(-1));
            _nextVehicleButton.onClick.AddListener(() => ChangeVehicleModel(1));
            _previousDesignButton.onClick.AddListener(() => ChangeVehicleDesign(-1));
            _nextDesignButton.onClick.AddListener(() => ChangeVehicleDesign(1));
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();
            _previousVehicleButton.onClick.RemoveAllListeners();
            _nextVehicleButton.onClick.RemoveAllListeners();
            _previousDesignButton.onClick.RemoveAllListeners();
            _nextDesignButton.onClick.RemoveAllListeners();
        }

        private void Start()
        {
            _visualVehicleController = VisualVehicleController.Instance;
            _visualVehicleController.GetPlayerIndices(out var initialVehicleIndex, out var initialDesignIndex);
            _visualVehicleController.ResetController();
            ChangeVehicleModel(initialVehicleIndex);
            ChangeVehicleDesign(initialDesignIndex);
        }

        private void ChangeVehicleModel(int i)
        {
            _vehicleIndex += i;
            if (_vehicleIndex < 0)
            {
                _vehicleIndex = _visualVehicleController.VehiclePrefabCount + _vehicleIndex;
            }

            _vehicleIndex %= _visualVehicleController.VehiclePrefabCount;
            if (_currentVehicle != null)
            {
                Destroy(_currentVehicle.gameObject);
            }
            var newVehicle = Instantiate(_visualVehicleController.VehiclePrefabs[_vehicleIndex], Vector3.zero, Quaternion.identity);
            _currentVehicle = newVehicle.GetComponent<VisualVehicleMaterialChange>();
            SetVehicleDesign();
            _vehicleSelectText.text = $"Vehicle {_vehicleIndex + 1}/{_visualVehicleController.VehiclePrefabCount}";
        }

        private void ChangeVehicleDesign(int i)
        {
            _designIndex += i;
            if (_designIndex < 0)
            {
                _designIndex = _visualVehicleController.VehicleMaterialCount + _designIndex;
            }

            _designIndex %= _visualVehicleController.VehicleMaterialCount;
            SetVehicleDesign();
            _designSelectText.text = $"Design {_designIndex + 1}/{_visualVehicleController.VehicleMaterialCount}";
        }

        private void SetVehicleDesign()
        {
            _currentVehicle.SetNewMaterial(_visualVehicleController.VehicleMaterials[_designIndex]);
        }
        
        private void EnterScene()
        {
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<InitializeCameraFollowSystem>().Enabled = true;
                World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<CountdownSystem>().Enabled = true;
                World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<VehicleEliminationSystem>().Enabled = true;
                World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<HUDControllerReferenceInitializationSystem>().Enabled = true;
                World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<StartLineInitializationSystem>().Enabled = true;
                World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<SpawnSystem>().Enabled = true;
            }

            _visualVehicleController.SetPlayerVehicle(_vehicleIndex, _designIndex);
            
            SceneManager.LoadScene(1);
        }

        private void Update()
        {
            _cameraPivotTransform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        }
    }
}