using Toc.PlayerProgression;


namespace Toc.Battle.Ai
{
	public class DodgeResult
	{
		public readonly bool IsSuccessfullyDodged;
		public readonly bool WasTryingToDodge;
		public readonly CommonNumber Multiplier;
		

		private DodgeResult(bool isDodged, bool wasTryingToDodge, CommonNumber dodgeMultiplier)
		{
			IsSuccessfullyDodged = isDodged;
			WasTryingToDodge = wasTryingToDodge;
			Multiplier = dodgeMultiplier;
		}
		
		
		private DodgeResult(bool isDodged, bool wasTryingToDodge, float dodgeMultiplier)
			: this(isDodged, wasTryingToDodge, (CommonNumber) dodgeMultiplier)
		{
		}

	
		public static DodgeResult NotTried()
			=> new DodgeResult(false, false, 1);
		
		
		public static DodgeResult Failed()
			=> new DodgeResult(false, true, 1);


		public static DodgeResult Successful(CommonNumber dodgeMultiplier)
			=> new DodgeResult(true, true, dodgeMultiplier);
	}
}
