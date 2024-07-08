using System;
using System.Runtime.CompilerServices;
using BlobActor.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Troupe.Runtime
{
    public partial struct InFormation : IComponentData
    {
        public float3 destination;
        public bool isVelocity;
        public Entity formation;
        public ActorRuntime.Flag preferedFlags;
    }

    public partial struct LazyFormationTag : IComponentData
    {
        public int counter;
        public bool skipBind;
        public float3 extraSpawnExtents;
        public Quaternion defaultOrientation;
    }

    [InternalBufferCapacity(0)]
    public partial struct LazyFormationNode : IBufferElementData
    {
        public Entity entity;
        public int index;
    }

    public partial struct Formation : IComponentData
    {
        [Serializable]
        public struct Blob
        {
            public float elementMaxDeviation;
            public float2 minMaxDeviationRegroupTrigger;
            public float baseSpeed;

            [Tooltip("Enable pathfinding for unstable agents if invidivual deviation value>this")]
            public float pathfindingThreshold;

            public static Blob Default => new Blob()
            {
                baseSpeed = 3,
                minMaxDeviationRegroupTrigger = new float2(2f, 4f),
                pathfindingThreshold = 2f,
                elementMaxDeviation = 0.05f,
            };
        }

        public BlobAssetReference<Blob> blob;
    }

    [Serializable]
    public partial struct FormationRuntime : IComponentData
    {
        public enum State
        {
            Bind = 0,
            Reset = 1,
            Regroup,
            Move,
        }

        public float3 direction;
        public float speed;
        public float3 avgPosition;
        public State state;
        public float groupDeviation;

        public static FormationRuntime Default => new FormationRuntime()
        {
            state = State.Bind,
            groupDeviation = -1,
        };
    }


    public partial struct PredefinedFormation : IComponentData
    {
        [Serializable]
        public struct Node
        {
            public float3 offset;
        }

        [Serializable]
        public struct Blob
        {
            public BlobArray<Node> nodes;
            public static Blob Default => new Blob();
        }

        public BlobAssetReference<Blob> blob;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Bind(ref DynamicBuffer<FormationElement> elements, in PredefinedFormation formation)
        {
            ref var nodes = ref formation.blob.Value.nodes;
            var max = nodes.Length;
            for (int i = 0; i < math.min(elements.Length, max); i++)
            {
                var element = elements[i];
                if (element.entity.Equals(Entity.Null)) continue;
                element.index = i;
                elements[i] = element;
            }
        }
    }

    [InternalBufferCapacity(0)]
    public partial struct FormationElement : IBufferElementData
    {
        public Entity entity;
        public byte flag;
        public int index;
        public float deviation;
    }
}