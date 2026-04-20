using System;
using UnityEngine;

namespace UnityStandardAssets.Utility
{
    [RequireComponent(typeof(Rigidbody))]
    public class AutoMoveAndRotateFixed : MonoBehaviour
    {
        public Vector3andSpaceFixed moveUnitsPerSecond;
        public Vector3andSpaceFixed rotateDegreesPerSecond;
        public bool ignoreTimescale;

        private Rigidbody rb;
        private float m_LastRealTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        private void Start()
        {
            m_LastRealTime = Time.realtimeSinceStartup;
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            if (ignoreTimescale)
            {
                float realDelta = Time.realtimeSinceStartup - m_LastRealTime;
                m_LastRealTime = Time.realtimeSinceStartup;
                deltaTime = realDelta;
            }

            Vector3 move = moveUnitsPerSecond.value * deltaTime;
            Vector3 rotate = rotateDegreesPerSecond.value * deltaTime;

            Vector3 worldMove =
                moveUnitsPerSecond.space == Space.Self
                    ? transform.TransformDirection(move)
                    : move;

            Quaternion deltaRotation =
                rotateDegreesPerSecond.space == Space.Self
                    ? transform.rotation * Quaternion.Euler(rotate) * Quaternion.Inverse(transform.rotation)
                    : Quaternion.Euler(rotate);

            rb.MovePosition(rb.position + worldMove);
            rb.MoveRotation(deltaRotation * rb.rotation);
        }

        [Serializable]
        public class Vector3andSpaceFixed
        {
            public Vector3 value;
            public Space space = Space.Self;
        }
    }
}