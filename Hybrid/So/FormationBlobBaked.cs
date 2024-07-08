#if UNITY_EDITOR
using Core.Hybrid;
using Troupe.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Troupe.Hybrid
{
    [CreateAssetMenu(menuName = "Troupe/Formation ")]
    public class FormationBlobBaked : BakedScriptableObject<Formation.Blob>
    {
        [SerializeField] private Formation.Blob data_s = Formation.Blob.Default;
        public float baseSpeed => data_s.baseSpeed;

        public override void Bake(ref Formation.Blob data, ref BlobBuilder blobBuilder)
        {
            data = data_s;
        }
    }
}
#endif