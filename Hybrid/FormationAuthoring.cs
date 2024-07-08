#if UNITY_EDITOR
using Troupe.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Troupe.Hybrid
{
    public class FormationAuthoring : MonoBehaviour
    {
        [SerializeField] private FormationBlobBaked blob_s;

        private class FormationBaker : Baker<FormationAuthoring>
        {
            public override void Bake(FormationAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<FormationElement>(e);
                AddComponent(
                    e,
                    new Formation()
                    {
                        blob = authoring.blob_s.Bake(this),
                    });
                var runtime = FormationRuntime.Default;
                runtime.speed = authoring.blob_s.baseSpeed;
                AddComponent(e, runtime);
            }
        }
    }
}
#endif