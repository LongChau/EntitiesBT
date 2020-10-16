using EntitiesBT.Components;
using EntitiesBT.Core;
using EntitiesBT.DebugView;
using EntitiesBT.Entities;
using EntitiesBT.Variable;
using Unity.Entities;
using UnityEngine;

namespace EntitiesBT.Sample
{
    public class BTVariablesTest : BTNode<VariablesTestNode>
    {
        [SerializeReference, SerializeReferenceButton] public Int64PropertyReader LongVariable;
        public string String;
        public int[] IntArray;
        [SerializeReference, SerializeReferenceButton] public Int32PropertyWriter DestVariable;
        [SerializeReference, SerializeReferenceButton] public ISinglePropertyReader SrcVariable;
        public long LongValue;

        protected override void Build(ref VariablesTestNode data, BlobBuilder builder, ITreeNode<INodeDataBuilder>[] tree)
        {
            LongVariable.Allocate(ref builder, ref data.LongVariable, this, tree);
            builder.AllocateString(ref data.String, String);
            builder.AllocateArray(ref data.IntArray, IntArray);
            DestVariable.Allocate(ref builder, ref data.DestVariable, this, tree);
            SrcVariable.Allocate(ref builder, ref data.SrcVariable, this, tree);
            data.Long = LongValue;
        }
    }
    
    [BehaviorNode("867BFC14-4293-4D4E-B3F0-280AD4BAA403")]
    public struct VariablesTestNode : INodeData
    {
        [Optional] public BlobVariableReader<long> LongVariable;
        public BlobString String;
        public BlobArray<int> IntArray;
        public BlobVariableWriter<int> DestVariable;
        public BlobVariableReader<float> SrcVariable;
        public long Long;

        public NodeState Tick<TNodeBlob, TBlackboard>(int index, ref TNodeBlob blob, ref TBlackboard bb)
            where TNodeBlob : struct, INodeBlob
            where TBlackboard : struct, IBlackboard
        {
            DestVariable.Write(index, ref blob, ref bb, (int)SrcVariable.Read(index, ref blob, ref bb));
            return NodeState.Success;
        }

        public void Reset<TNodeBlob, TBlackboard>(int index, ref TNodeBlob blob, ref TBlackboard bb)
            where TNodeBlob : struct, INodeBlob
            where TBlackboard : struct, IBlackboard
        {}
    }

    [BehaviorTreeDebugView(typeof(VariablesTestNode))]
    public class BlobStringDebugView : BTDebugView
    {
        public long LongVariable;
        public string String;
        public int[] IntArray;
        // public int IntVariable;
        public float FloatVariable;
        public long LongValue;

        public override void Tick()
        {
            var blob = Blob;
            var bb = Blackboard.Value;
            ref var data = ref blob.GetNodeData<VariablesTestNode, NodeBlobRef>(Index);
            LongVariable = data.LongVariable.Read(Index, ref blob, ref bb);
            // IntVariable = data.DestVariable.Read(Index, ref blob, ref bb);
            FloatVariable = data.SrcVariable.Read(Index, ref blob, ref bb);
            String = data.String.ToString();
            IntArray = data.IntArray.ToArray();
            LongValue = data.Long;
        }
    }
}
