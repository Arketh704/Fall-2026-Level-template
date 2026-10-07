using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using HeaderAttribute = ParadoxNotion.Design.HeaderAttribute;

namespace LevelDesign.Systems.Enemy
{
    [Category("Enemy")]
    [Description("True if the target is within range (horizontal distance).")]
    public class IsTargetInRange : ConditionTask<Transform>
    {
        [RequiredField] public BBParameter<GameObject> target;
        public BBParameter<float> range = 2f;

        protected override string info { get { return target + " within " + range + "m"; } }

        protected override bool OnCheck()
        {
            if(target.value == null) { return false; }
            return EnemyUtil.FlatDistance(agent.position, target.value.transform.position) <= range.value;
        }
    }

    [Category("Enemy")]
    [Description("Linecast from eye height to the target. True if nothing on the obstacle mask is in the way.")]
    public class HasLineOfSight : ConditionTask<Transform>
    {
        [RequiredField] public BBParameter<GameObject> target;
        public BBParameter<float> eyeHeight = 1.5f;
        public BBParameter<Vector3> targetOffset = Vector3.up;
        public BBParameter<LayerMask> obstacleMask = (LayerMask)Physics.DefaultRaycastLayers;

        protected override string info { get { return "LOS to " + target; } }

        protected override bool OnCheck()
        {
            return EnemyUtil.HasLineOfSight(agent.position + Vector3.up * eyeHeight.value, target.value, targetOffset.value, obstacleMask.value.value);
        }
    }

    [Category("Enemy")]
    [Description("True when Time.time has passed the stored next-attack time.")]
    public class AttackReady : ConditionTask
    {
        public BBParameter<float> nextAttackTime;

        protected override string info { get { return "Attack Ready (" + nextAttackTime + ")"; } }

        protected override bool OnCheck() { return Time.time >= nextAttackTime.value; }
    }

    [Category("Enemy")]
    [Description("Random roll. True with the given probability each time it is checked.")]
    public class Chance : ConditionTask
    {
        [SliderField(0f, 1f)] public BBParameter<float> probability = 0.35f;

        protected override string info { get { return Mathf.RoundToInt(probability.value * 100f) + "% Chance"; } }

        protected override bool OnCheck() { return Random.value <= probability.value; }
    }

    [Category("Enemy")]
    [Description("True while healthPercent (0-100) is below threshold. Feed healthPercent from your health script via the exposed blackboard variable.")]
    public class IsHealthBelow : ConditionTask
    {
        [RequiredField, BlackboardOnly] public BBParameter<float> healthPercent;
        [SliderField(0f, 100f)] public BBParameter<float> threshold = 35f;

        protected override string info { get { return "Health < " + threshold + "%"; } }

        protected override bool OnCheck() { return healthPercent.value < threshold.value; }
    }

    [Category("Enemy")]
    [Description("View cone + awareness radius + line of sight, with a short memory so the agent stays in combat " +
                 "while it turns away (e.g. walking to high ground) instead of dropping straight back to patrol.\n\n" +
                 "Defaults to using the Enemy Senses Component ")]
    public class CanSenseTarget : ConditionTask<Transform>
    {
        [RequiredField] public BBParameter<GameObject> target;

        [Header("Fallback Senses (used only without EnemySenses)")]
        public BBParameter<float> viewDistance = 20f;
        [SliderField(1f, 180f)] public BBParameter<float> viewAngle = 70f;
        public BBParameter<float> awarenessDistance = 10f;
        public BBParameter<float> memoryTime = 5f;

        [Header("Line Of Sight")]
        public BBParameter<float> eyeHeight = 1.5f;
        public BBParameter<Vector3> targetOffset = Vector3.up;
        public BBParameter<LayerMask> obstacleMask = (LayerMask)Physics.DefaultRaycastLayers;

        private EnemySenses senses;
        private float lastSensedTime = float.NegativeInfinity;

        protected override string info {
            get { return "Sense " + target + (senses != null ? " (EnemySenses)" : " (" + memoryTime + "s memory)"); }
        }

        protected override string OnInit()
        {
            senses = agent.GetComponent<EnemySenses>();
            if(senses == null) { senses = agent.GetComponentInParent<EnemySenses>(); }
            if(senses == null) { senses = agent.GetComponentInChildren<EnemySenses>(); }
            if(senses == null) {
                Debug.LogWarning("CanSenseTarget on '" + agent.name + "': no EnemySenses found, using the node's fallback values.", agent);
            }
            return null;
        }

        protected override bool OnCheck()
        {
            if(senses != null) { return senses.Sense(target.value); }

            var t = target.value;
            if(t == null) { return false; }

            var to = t.transform.position - agent.position; to.y = 0f;
            var dist = to.magnitude;
            var fwd = agent.forward; fwd.y = 0f;

            var inRange = dist <= awarenessDistance.value ||
                          (dist <= viewDistance.value && Vector3.Angle(fwd, to) <= viewAngle.value);

            if(inRange && EnemyUtil.HasLineOfSight(agent.position + Vector3.up * eyeHeight.value, t, targetOffset.value, obstacleMask.value.value)) {
                lastSensedTime = Time.time;
                return true;
            }
            return Time.time - lastSensedTime <= memoryTime.value;
        }
    }
}