using UnityEngine;

namespace VFX
{
    // For now only spheres
    public class DeformerVolume : MonoBehaviour
    {
        [Range(0f, 3f)]
        public float strength = 0.5f;

        [Range(0.1f, 5f)]
        public float radius = 1f;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = strength > 0
                ? new Color(1f, 0.5f, 0f, 0.7f)
                : new Color(0.5f, 0.5f, 0.5f, 0.3f);

            Gizmos.DrawWireSphere(transform.position, radius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            // Gizmos.DrawSphere(transform.position, radius);
        }
    }
}
