using PaintedAlive.Figures.Impact;
using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class SpongeBurstImpactProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.5f)]
        private float maximumLifetime = 5f;

        private Rigidbody body;
        private float destroyTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            destroyTime = Time.time + maximumLifetime;
        }

        private void Update()
        {
            if (Time.time >= destroyTime)
            {
                Destroy(gameObject);
            }
        }

        public void Launch(Vector3 velocity)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.linearVelocity = velocity;
            body.angularVelocity =
                new Vector3(4.2f, 6.8f, -3.5f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null ||
                collision.collider == null)
            {
                Destroy(gameObject);
                return;
            }

            FigureImpactSensor sensor =
                collision.collider.GetComponentInParent<
                    FigureImpactSensor>();

            if (sensor != null)
            {
                Vector3 point = transform.position;
                Vector3 normal = Vector3.up;

                if (collision.contactCount > 0)
                {
                    ContactPoint contact = collision.GetContact(0);
                    point = contact.point;
                    normal = contact.normal;
                }

                sensor.ReportImpact(
                    collision.relativeVelocity.magnitude,
                    point,
                    normal,
                    this);
            }

            Destroy(gameObject);
        }

        private void OnValidate()
        {
            maximumLifetime =
                Mathf.Max(0.5f, maximumLifetime);
        }
    }
}
