using BehaviorDesigner.Runtime.Tasks;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai.Tasks
{
    public abstract class BaseMonsterAction : Action
    {
	    protected DecisionTreeProxy Proxy { get; private set; }
	    protected MonsterBattleEntity Entity { get; private set; }
	    protected GameState State { get; private set; }


        public override void OnStart()
        {
	        Proxy = GetComponent<DecisionTreeProxy>();
	        Entity = Proxy.MonsterBattleEntity;
	        State = Proxy.GameState;
        }
    }
}