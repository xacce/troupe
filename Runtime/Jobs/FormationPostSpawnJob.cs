using BlobActor.Runtime;
using Troupe.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace Jobs
{
    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct LazyFormationNavMeshInitializeJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public NavMeshQuery navMeshQuery;

        [ReadOnly] public ComponentLookup<Actor> actorRo;

        [BurstCompile]
        private void Execute([EntityIndexInQuery] int index, in LocalToWorld ltw, in FormationRuntime runtime, ref LazyFormationTag lazy, DynamicBuffer<LazyFormationNode> nodes,
            Entity entity)
        {
            if (lazy.counter >= nodes.Length)
            {
                ecb.RemoveComponent<LazyFormationTag>(index, entity);
                ecb.RemoveComponent<LazyFormationNode>(index, entity);
                var runtimeRo = runtime;
                runtimeRo.state = lazy.skipBind ? FormationRuntime.State.Reset : FormationRuntime.State.Bind;
                ecb.SetComponent(index, entity, runtimeRo);
                return;
            }
            ref var actor = ref actorRo[nodes[lazy.counter].entity].blob.Value;
            var location = navMeshQuery.MapLocation(ltw.Position, actor.spawnExtents + lazy.extraSpawnExtents, actor.agentTypeId, actor.areaMask);
            if (location.polygon.IsNull())
            {
                Debug.LogWarning("Cant spawn formation node, nav mesh bad>");
                return;
            }
            var element = ecb.Instantiate(index, nodes[lazy.counter].entity);
            ecb.SetComponent(
                index,
                element,
                new LocalTransform()
                {
                    Position = location.position,
                    Rotation = lazy.defaultOrientation,
                    Scale = 1f,
                });
            ecb.AppendToBuffer(
                index,
                entity,
                new FormationElement()
                {
                    entity = element,
                    index = nodes[lazy.counter].index,
                });

            lazy.counter++;
        }
    }
}