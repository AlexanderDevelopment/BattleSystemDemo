using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
	[TaskDescription("Сравниваем значение MaxHealth у монстра")]
	[TaskCategory("AI")]
	public class CompareMonsterMaxHealth : ComparisionCondition
	{
		protected override CommonNumber EvaluateCurrentValue()
			=> Proxy.MonsterBattleEntity.MaxHealth;
	}
}