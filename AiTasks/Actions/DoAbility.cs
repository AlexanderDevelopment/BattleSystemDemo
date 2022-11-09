using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
	[TaskDescription("Способность")]
	[TaskCategory("AI")]
	public class DoAbility : BaseMonsterAction
	{
		public Ability Ability;


		public override TaskStatus OnUpdate()
		{
			Proxy.ActivatedAbilities.Add(Ability);

			return TaskStatus.Success;
		}
	}
}