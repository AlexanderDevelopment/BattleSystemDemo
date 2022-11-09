using BehaviorDesigner.Runtime.Tasks;


namespace Toc.Battle.Ai.Tasks
{
    [TaskDescription("Атака")]
    [TaskCategory("AI")]
    public class DoAttack : BaseMonsterAction
    {
	    public override TaskStatus OnUpdate()
        {
	        Proxy.ActivatedAbilities.Add(Entity.BasicAttackAbility);

	        return TaskStatus.Success;
        }
    }
}