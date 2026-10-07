using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelDesign.Systems.Enemy
{
    [DisallowMultipleComponent]
    public class EnemySenses : MonoBehaviour
    {
        [Header("Config")]
        public float viewDistance = 20f;
        [Range(1f, 180f)] public float viewAngle = 70f;
        public float awarenessDistance = 10f;
        public float memoryTime = 5f;
        public float eyeHeight = 1.5f;
        public Vector3 targetOffset = Vector3.up;
        public LayerMask obstacleMask = Physics.DefaultRaycastLayers;

        private float lastSensedTime = float.NegativeInfinity;

        public bool Sense(GameObject target)
        {
            if(target == null) { return false; }

            var to = target.transform.position - transform.position; to.y = 0f;
            var dist = to.magnitude;
            var fwd = transform.forward; fwd.y = 0f;

            var inRange = dist <= awarenessDistance || (dist <= viewDistance && Vector3.Angle(fwd, to) <= viewAngle);
            var eye = transform.position + Vector3.up * eyeHeight;

            if(inRange && EnemyUtil.HasLineOfSight(eye, target, targetOffset, obstacleMask.value)) {
                lastSensedTime = Time.time;
                return true;
            }
            return Time.time - lastSensedTime <= memoryTime;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var pos = transform.position;
            var fwd = transform.forward; fwd.y = 0f;
            if(fwd.sqrMagnitude < 0.0001f) { fwd = Vector3.forward; }
            fwd.Normalize();

            Handles.color = Color.cyan;
            Handles.DrawWireDisc(pos, Vector3.up, awarenessDistance);

            Handles.color = Color.yellow;
            var from = Quaternion.AngleAxis(-viewAngle, Vector3.up) * fwd;
            var to = Quaternion.AngleAxis(viewAngle, Vector3.up) * fwd;
            Handles.DrawWireArc(pos, Vector3.up, from, viewAngle * 2f, viewDistance);
            Handles.DrawLine(pos, pos + from * viewDistance);
            Handles.DrawLine(pos, pos + to * viewDistance);
        }
#endif
    }
}