using System;


namespace Toc.Battle
{
	public static class BattleSideExtensions
	{
		public static BattleSide Inverse(this BattleSide side)
			=> side switch
			{
				BattleSide.Player => BattleSide.Opponent,
				BattleSide.Opponent => BattleSide.Player,
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
			};
	}
}
