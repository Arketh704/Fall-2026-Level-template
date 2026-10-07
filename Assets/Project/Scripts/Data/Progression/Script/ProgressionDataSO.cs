using UnityEngine;
using System;
using LevelDesign.Systems.Player;

// Summary
// A really cheap and poor way to manage player progression.
// Modifying data here shouldn't save/load between plays which is a problem with most scriptable objects

namespace LevelDesign.Data
{
    [CreateAssetMenu(fileName = "ProgressionData", menuName = "ScriptableObjects/Config/ProgressionData", order = 1)]
    public class ProgressionDataSO : ScriptableObject
    {
        [Header("Spawning")]
        [NonSerialized] public int checkpoint;
        
/* Might be Ambitious but progression??? :D
        [Header("Skills")]
        [NonSerialized] public bool unlockedSaSPrimary;
        [NonSerialized] public bool unlockedSaSSecondary;
        [NonSerialized] public bool unlockedMagePrimary;
        [NonSerialized] public bool unlockedMageSecondary;
*/
    }
}