using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMG.RacingRoyale
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance;

        [SerializeField] private TextMeshProUGUI _centerScreenMessageText;
        [SerializeField] private TextMeshProUGUI _lapCounterText;
        [SerializeField] private TextMeshProUGUI _positionText;
        [SerializeField] private TextMeshProUGUI _eliminationTimerText;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private GameObject _domImages;
        
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            _mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            _domImages.SetActive(false);
            ShowHideMenuButton(false);
        }

        private void OnDisable()
        {
            _mainMenuButton.onClick.RemoveAllListeners();
        }
        
        private void ReturnToMainMenu()
        {
            SceneManager.LoadScene(0);
        }

        public void SetCenterScreenMessageText(string text)
        {
            _centerScreenMessageText.text = text;
        }

        public void SetLapCounterText(int lapNumber)
        {
            _lapCounterText.text = $"Lap: {lapNumber}";
        }

        public void SetPositionText(int currentPosition, int totalPositions)
        {
            var textColor = currentPosition > totalPositions ? "<color=red>" : "<color=white>";
            _positionText.text = $"{textColor}{currentPosition}/{totalPositions}</color>";
        }

        public void SetEliminationTimerText(float timer, int minPosition, int maxPosition)
        {
            if (timer <= 0f)
            {
                _eliminationTimerText.text = "";
                return;
            }

            string positionText;
            string positionString;
            if (minPosition == maxPosition)
            {
                positionText = minPosition.ToString();
                positionString = "Position";
            }
            else
            {
                positionText = $"{minPosition} - {maxPosition}";
                positionString = "Positions";
            }

            _eliminationTimerText.text = $"Eliminating {positionString}\n{positionText} in 0:{math.ceil(timer):00}";
        }

        public void ShowHideMenuButton(bool shouldShow)
        {
            _mainMenuButton.gameObject.SetActive(shouldShow);
        }

        public void ShowDomImage()
        {
            _domImages.SetActive(true);
        }
    }

    public struct HUDControllerReference : IComponentData
    {
        public UnityObjectRef<HUDController> Value;
    }

    public partial struct HUDControllerReferenceInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HUDControllerReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (HUDController.Instance == null) return;
            state.Enabled = false;

            var hudControllerReference = SystemAPI.GetSingletonRW<HUDControllerReference>();
            hudControllerReference.ValueRW.Value = HUDController.Instance;

            HUDController.Instance.SetLapCounterText(1);
        }
    }
}