using Toc.Battle.Ai;


namespace Toc.PlayerProgression
{
	public interface IBattleEntityInfo
	{
		CommonNumber MaxHealth { get; }


		CommonNumber CurrentHealth { get; }


		bool IsAlive { get; }


		bool IsDead { get; }

	}
}
