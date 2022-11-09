using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;
using Toc.Utils;


namespace Toc.Battle.Ai.Tasks
{
	public abstract class ComparisionCondition : BaseMonsterConditional
	{
		protected abstract CommonNumber EvaluateCurrentValue();
		
		
		public CompareOperator CompareOperator;
		
		
		public CommonNumber CompareWith;
		
		
		public override TaskStatus OnUpdate() 
			=> Compare();
		
		
		private TaskStatus Compare()
			=> CompareCondition.Compare(CompareOperator, EvaluateCurrentValue(), CompareWith) 
				? TaskStatus.Success 
				: TaskStatus.Failure;
		
	}
}
