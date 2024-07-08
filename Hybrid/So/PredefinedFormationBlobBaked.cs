#if UNITY_EDITOR
using System;
using Core.Hybrid;
using Troupe.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Troupe.Hybrid
{
    [CreateAssetMenu(menuName = "Troupe/Predefined formation")]
    public class PredefinedFormationBlobBaked : BakedScriptableObject<PredefinedFormation.Blob>
    {
        [SerializeField] private PredefinedFormation.Blob data_s = PredefinedFormation.Blob.Default;
        [SerializeField] private PredefinedFormation.Node[] nodes_s = Array.Empty<PredefinedFormation.Node>();
        public override void Bake(ref PredefinedFormation.Blob data, ref BlobBuilder blobBuilder)
        {
            data = data_s;
            var arr = blobBuilder.Allocate(ref data.nodes, nodes_s.Length);
            for (int i = 0; i < nodes_s.Length; i++)
            {
                arr[i] = nodes_s[i];
            }
        }
    }
}
#endif