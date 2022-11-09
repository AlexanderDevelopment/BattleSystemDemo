using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Toc.Battle;
using Toc.PlayerProgression;


namespace Toc.Battle.Ai
{
	[Serializable]
	public class RoundDiffs<TDiff, TDamage>
		where TDiff : BaseSideDiff<TDamage>
	{
		[ShowInInspector]
		[HorizontalGroup]
		private readonly TDiff _playerSide;
		
		[ShowInInspector]
		[HorizontalGroup]
		private readonly TDiff _opponentSide;


		public RoundDiffs(TDiff player, TDiff opponent)
		{
			if (player.IsOnActiveSide == opponent.IsOnActiveSide)
				throw new ArgumentException($"Both sides .{nameof(player.IsOnActiveSide)} are {player.IsOnActiveSide}", nameof(player.IsOnActiveSide));

			_playerSide = player;
			_opponentSide = opponent;
		}


		public TDiff this[BattleSide side]
			=> side switch
			{
				BattleSide.Player => _playerSide,
				BattleSide.Opponent => _opponentSide,
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
			};
	}
}
