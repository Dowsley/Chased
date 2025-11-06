using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace Driving.AI
{
    public class PoliceSirenFlasher : MonoBehaviour
    {
        [Header("Siren Settings")]
        [Tooltip("Flash rate in flashes per second")]
        [SerializeField] private float flashRate = 2f;

        [Tooltip("Material indices for the red and blue sirens")]
        [SerializeField] private int redSirenIndex = 2;
        [SerializeField] private int blueSirenIndex = 6;

        [Header("Siren Materials")]
        [SerializeField] private Material redSirenNormal;
        [SerializeField] private Material redSirenActive;
        [SerializeField] private Material blueSirenNormal;
        [SerializeField] private Material blueSirenActive;

        [Header("Point lights")]
        [SerializeField] private float lightStrength = 20f;
        [SerializeField] private Light blueLight;
        [SerializeField] private Light redLight;

        private MeshRenderer _bodyRenderer;
        private Material[] _materials;
        private float _timer;
        private bool _redOn = true;

        private void Start()
        {
            // Get the Body child's MeshRenderer
            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform == null)
            {
                Debug.LogError("PoliceSirenFlasher: Could not find Body child object!");
                enabled = false;
                return;
            }

            _bodyRenderer = bodyTransform.GetComponent<MeshRenderer>();
            if (_bodyRenderer == null)
            {
                Debug.LogError("PoliceSirenFlasher: Body has no MeshRenderer!");
                enabled = false;
                return;
            }

            // Get materials array
            _materials = _bodyRenderer.materials;

            if (_materials.Length <= redSirenIndex || _materials.Length <= blueSirenIndex)
            {
                Debug.LogError($"PoliceSirenFlasher: Not enough materials! Has {_materials.Length}, needs at least {Mathf.Max(redSirenIndex, blueSirenIndex) + 1}");
                enabled = false;
                return;
            }

            // Check if materials are assigned
            if (!redSirenNormal || !redSirenActive || !blueSirenNormal || !blueSirenActive)
            {
                Debug.LogError("PoliceSirenFlasher: Please assign all siren materials in the inspector!");
                enabled = false;
                return;
            }

            // Initialize with red on, blue off
            _materials[redSirenIndex] = redSirenActive;
            _materials[blueSirenIndex] = blueSirenNormal;
            _bodyRenderer.materials = _materials;

            // Initialize lights
            if (redLight)
                redLight.intensity = lightStrength;
            if (blueLight)
                blueLight.intensity = 0f;
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            // Check if it's time to switch
            if (_timer >= 1f / flashRate)
            {
                _timer = 0f;
                _redOn = !_redOn;

                // Toggle sirens
                if (_redOn)
                {
                    _materials[redSirenIndex] = redSirenActive;
                    _materials[blueSirenIndex] = blueSirenNormal;

                    if (redLight)
                        redLight.intensity = lightStrength;
                    if (blueLight)
                        blueLight.intensity = 0f;
                }
                else
                {
                    _materials[redSirenIndex] = redSirenNormal;
                    _materials[blueSirenIndex] = blueSirenActive;

                    if (redLight)
                        redLight.intensity = 0f;
                    if (blueLight)
                        blueLight.intensity = lightStrength;
                }

                _bodyRenderer.materials = _materials;
            }
        }
    }
}
