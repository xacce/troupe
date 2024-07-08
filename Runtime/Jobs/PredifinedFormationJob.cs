using System.Runtime.CompilerServices;
using BlobActor.Runtime;
using GameReady.Runtime;
using Troupe.Runtime;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Jobs
{
    [BurstCompile]
    [WithAll(typeof(Simulate))]
    [WithNone(typeof(LazyFormationTag))]
    public partial struct PredifinedFormationJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public float deltaTime;

        [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldRo;

        [ReadOnly] public ComponentLookup<Dead> deadRo;
        public EntityCommandBuffer.ParallelWriter ecb;
        [NativeDisableParallelForRestriction] public ComponentLookup<InFormation> informationLookupRw;
        private int _index;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TakeControl(ref InFormation formation, float3 velocity)
        {
            formation.preferedFlags = ActorRuntime.Flag.AvoidanceEnabled | ActorRuntime.Flag.AllowMove | ActorRuntime.Flag.AllowTurning;
            formation.isVelocity = true;
            formation.destination = velocity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OutOfControl(ref InFormation formation, in float3 destination)
        {
            formation.preferedFlags = ActorRuntime.Flag.AllowMove | ActorRuntime.Flag.AllowTurning | ActorRuntime.Flag.PathSeeekerEnabled | ActorRuntime.Flag.AvoidanceEnabled |
                                      ActorRuntime.Flag.AutoRepath;
            formation.isVelocity = false;
            formation.destination = destination;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySetFirstStable(in PredefinedFormation predefinedFormation, float maxDeviation, ref FormationRuntime formationRuntime,
            ref ComponentLookup<LocalToWorld> ltwLookup,
            ref DynamicBuffer<FormationElement> elements)
        {
            ref var nodes = ref predefinedFormation.blob.Value.nodes;
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                if (element.deviation > maxDeviation) continue;
                var ltw = ltwLookup[element.entity];
                formationRuntime.avgPosition = ltw.Position - nodes[element.index].offset;

                return true;
            }
            return false;
        }

        [BurstCompile]
        private void Execute(ref FormationRuntime formationRuntime, ref LocalTransform transform, in Formation formation, DynamicBuffer<FormationElement> elements,
            in PredefinedFormation predefinedFormation, in Entity formationEntity)
        {
            ref var blob = ref formation.blob.Value;
            ref var nodes = ref predefinedFormation.blob.Value.nodes;
            switch (formationRuntime.state)
            {
                case FormationRuntime.State.Bind:
                    PredefinedFormation.Bind(ref elements, predefinedFormation);
                    formationRuntime.state = FormationRuntime.State.Reset;
                    break;
                case FormationRuntime.State.Reset:
                    float3 sum = new float3();
                    for (int i = 0; i < elements.Length; i++)
                    {
                        var element = elements[i];
                        if (!deadRo.IsComponentEnabled(element.entity) && !element.entity.Equals(Entity.Null) && localToWorldRo.TryGetComponent(element.entity, out var ltw))
                            sum += ltw.Position;
                        else
                        {
                            elements.RemoveAt(i);
                            i--;
                        }
                    }
                    formationRuntime.avgPosition = sum / elements.Length;
                    transform.Position = formationRuntime.avgPosition;
                    formationRuntime.state = FormationRuntime.State.Regroup;
                    break;
                case FormationRuntime.State.Move when TrySetFirstStable(predefinedFormation, blob.elementMaxDeviation, ref formationRuntime, ref localToWorldRo, ref elements):
                {
                    transform.Position = formationRuntime.avgPosition;
                    float groupDeviation = 0f;
                    for (int i = 0; i < elements.Length; i++)
                    {
                        var element = elements[i];
                        if (element.entity.Equals(Entity.Null) || deadRo.IsComponentEnabled(element.entity) || !localToWorldRo.TryGetComponent(element.entity, out var ltw))
                        {
                            elements.RemoveAt(i);
                            i--;
                            continue;
                        }
                        var positionInFormation = formationRuntime.avgPosition + nodes[element.index].offset;
                        var dir = positionInFormation - ltw.Position;
                        groupDeviation += math.lengthsq(dir);
                        element.deviation = math.lengthsq(dir);
                        var inFormationRwo = informationLookupRw.GetRefRW(element.entity);
                        var inFormationRo = inFormationRwo.ValueRO;
                        if (element.deviation > blob.elementMaxDeviation)
                        {
                            if (element.deviation >= blob.pathfindingThreshold)
                            {
                                OutOfControl(ref inFormationRo, positionInFormation);
                            }
                            else
                            {
                                // ActorRuntime.ComputeNewVelocity(ref agentBlob, ref agentRo, math.normalize(positionInFormation - ltw.Position) * formationRuntime.speed, deltaTime);
                                TakeControl(ref inFormationRo, math.normalize(positionInFormation - ltw.Position) * formationRuntime.speed);
                            }
                        }
                        else
                        {
                            TakeControl(ref inFormationRo, formationRuntime.direction * formationRuntime.speed);
                        }
                        elements[i] = element;
                        inFormationRwo.ValueRW = inFormationRo;
                    }
                    if (groupDeviation > blob.minMaxDeviationRegroupTrigger.y) formationRuntime.state = FormationRuntime.State.Regroup;
                    formationRuntime.groupDeviation = groupDeviation;
                    break;
                }
                case FormationRuntime.State.Regroup when TrySetFirstStable(predefinedFormation, blob.elementMaxDeviation, ref formationRuntime, ref localToWorldRo, ref elements):
                {
                    transform.Position = formationRuntime.avgPosition;
                    float groupDeviation = 0f;
                    for (int i = 0; i < elements.Length; i++)
                    {
                        var element = elements[i];
                        if (element.entity.Equals(Entity.Null) || deadRo.IsComponentEnabled(element.entity) || !localToWorldRo.TryGetComponent(element.entity, out var ltw))
                        {
                            elements.RemoveAt(i);
                            i--;
                            continue;
                        }
                        var inFormationRwo = informationLookupRw.GetRefRW(element.entity);
                        var inFormationRo = inFormationRwo.ValueRO;
                        var positionInFormation = formationRuntime.avgPosition + nodes[element.index].offset;
                        var dir = positionInFormation - ltw.Position;
                        groupDeviation += math.lengthsq(dir);
                        element.deviation = math.lengthsq(dir);
                        if (element.deviation > blob.elementMaxDeviation)
                        {
                            OutOfControl(ref inFormationRo, positionInFormation);
                        }
                        else
                        {
                            TakeControl(ref inFormationRo, float3.zero);
                        }
                        elements[i] = element;
                        inFormationRwo.ValueRW = inFormationRo;
                    }
                    if (groupDeviation < blob.minMaxDeviationRegroupTrigger.x) formationRuntime.state = FormationRuntime.State.Move;
                    formationRuntime.groupDeviation = groupDeviation;
                    break;
                }
                default:
                {
                    formationRuntime.state = FormationRuntime.State.Reset;
                    break;
                }
            }

            if (formationRuntime.state > FormationRuntime.State.Bind && elements.Length < 2)
            {
                for (int i = 0; i < elements.Length; i++)
                {
                    ecb.RemoveComponent<InFormation>(_index, elements[i].entity);
                }
                ecb.DestroyEntity(_index, formationEntity);
            }

        }
        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            _index = unfilteredChunkIndex;
            return true;
        }
        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
        }
    }
}