#if UNITY_EDITOR
using System;
using Troupe.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Troupe.Hybrid
{
    public class LazyFormationAuthoring : MonoBehaviour
    {
        [SerializeField] private bool skipBind_s;
        [SerializeField] private float3 extraSpawnExtents_s;
        [SerializeField] private Quaternion defaultOrientation_s;
        [SerializeField] private GameObject[] lazied_s = Array.Empty<GameObject>();

        private class LazyFormationBaker : Baker<LazyFormationAuthoring>
        {
            public override void Bake(LazyFormationAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(
                    e,
                    new LazyFormationTag()
                    {
                        defaultOrientation = authoring.defaultOrientation_s,
                        extraSpawnExtents = authoring.extraSpawnExtents_s,
                        skipBind = authoring.skipBind_s,
                    });
                var nodes = AddBuffer<LazyFormationNode>(e);
                foreach (var lazy in authoring.lazied_s)
                {
                    nodes.Add(
                        new LazyFormationNode()
                        {
                            entity = GetEntity(lazy, TransformUsageFlags.None),
                        });
                }
            }
        }
    }
}
#endif