using System.Collections.Generic;
using UnityEngine;

namespace TMG.RacingRoyale
{
    public class VisualVehicleMaterialChange : MonoBehaviour
    {
        [SerializeField] private Material _targetMaterial;
        [SerializeField] private MeshRenderer[] _meshRenderers;

        private void OnValidate()
        {
            SetMaterials();
        }

        public void SetNewMaterial(Material newMaterial)
        {
            _targetMaterial = newMaterial;
            SetMaterials();
        }
        
        private void SetMaterials()
        {
            if (_targetMaterial == null) return;
            foreach (var meshRenderer in _meshRenderers)
            {
                var materialList = new List<Material>();
                for (var i = 0; i < meshRenderer.materials.Length; i++)
                {
                    materialList.Add(_targetMaterial);
                }

                meshRenderer.SetMaterials(materialList);
            }
        }
    }
}