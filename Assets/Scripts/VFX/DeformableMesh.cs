using UnityEngine;
using UnityEngine.Serialization;

namespace VFX
{
    [RequireComponent(typeof(MeshRenderer))]
    public class DeformableMesh : MonoBehaviour
    {
        [SerializeField] private DeformerVolume deformer;
        
        private static readonly int DeformerPos = Shader.PropertyToID("_DeformerPos");
        private static readonly int DeformerRadius = Shader.PropertyToID("_DeformerRadius");
        private static readonly int DeformerStrength = Shader.PropertyToID("_DeformerStrength");
        private MeshRenderer _meshRenderer;
        private Material[] _materials;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _materials = _meshRenderer.materials; // Get ALL materials (creates instances)
        }

        private void LateUpdate()
        {
            if (deformer)
            {
                // Send deformer data to ALL materials
                foreach (var mat in _materials)
                {
                    mat.SetVector(DeformerPos, deformer.transform.position);
                    mat.SetFloat(DeformerRadius, deformer.radius);
                    mat.SetFloat(DeformerStrength, deformer.strength);
                }
            }
            else
            {
                // No deformer - disable deformation
                foreach (var mat in _materials)
                {
                    mat.SetFloat(DeformerStrength, 0f);
                }
            }
        }
    }
}
