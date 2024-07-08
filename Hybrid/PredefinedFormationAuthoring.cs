#if UNITY_EDITOR
using Troupe.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Troupe.Hybrid
{
    [RequireComponent(typeof(FormationAuthoring))]
    public class PredefinedFormationAuthoring : MonoBehaviour
    {
        [SerializeField] private PredefinedFormationBlobBaked blob_s;

        private class PredefinedFormationBaker : Baker<PredefinedFormationAuthoring>
        {
            public override void Bake(PredefinedFormationAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(
                    e,
                    new PredefinedFormation()
                    {
                        blob = authoring.blob_s.Bake(this)
                    });
            }
        }
    }
}
#endif