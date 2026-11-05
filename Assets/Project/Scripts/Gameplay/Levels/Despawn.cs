using System;
using Unity.VisualScripting;
using UnityEngine;

namespace LevelDesign.Systems
{
    public class Despawn : MonoBehaviour
    {
        [SerializeField] private float despawnTime;

        void Update()
        {
            despawnTime -= Time.deltaTime;

            if(despawnTime <= 0)
            {
                Destroy(this);
            }
        }
    }
}
