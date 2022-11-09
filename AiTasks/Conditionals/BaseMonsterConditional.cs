using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
	public abstract class BaseMonsterConditional : Conditional
	{
		protected DecisionTreeProxy Proxy { get; private set; }
		protected MonsterBattleEntity Entity { get; private set; }
		protected GameState State { get; private set; }
	}
}
