using UnityEngine;

namespace Driving
{
    [RequireComponent(typeof(VehicleController))]
    public abstract class BaseDriverInput : MonoBehaviour
    {
        protected VehicleController VehicleController;

        protected virtual void Start()
        {
            VehicleController = GetComponent<VehicleController>();
        }

        protected virtual void FixedUpdate()
        {
            UpdateVehicle();
        }

        protected abstract void UpdateVehicle();
    }
}
