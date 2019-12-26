using System;
using EntitiesBT.Core;

namespace EntitiesBT.Nodes
{
    public class TimerNode : IBehaviorNode
    {
        public struct Data : INodeData
        {
            public TimeSpan Target;
            public TimeSpan Current;
            public NodeState ChildState;
            public NodeState BreakReturnState;
        }
        
        public void Reset(VirtualMachine vm, int index, IBlackboard blackboard)
        {
            ref var data = ref vm.GetNodeData<Data>(index);
            data.Current = TimeSpan.Zero;
            data.ChildState = NodeState.Running;
        }

        public NodeState Tick(VirtualMachine vm, int index, IBlackboard blackboard)
        {
            ref var data = ref vm.GetNodeData<Data>(index);

            if (data.Current >= data.Target)
                return data.ChildState == NodeState.Running ? data.BreakReturnState : data.ChildState;
            
            var childIndex = index + 1;
            if (data.ChildState == NodeState.Running && childIndex < vm.EndIndex(index))
            {
                var childState = vm.Tick(childIndex);
                data.ChildState = childState;
            }
            data.Current += ((TickDeltaTime)blackboard[typeof(TickDeltaTime)]).Value;
            return NodeState.Running;
        }
    }
}