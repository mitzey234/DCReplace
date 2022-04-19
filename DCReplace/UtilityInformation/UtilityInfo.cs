namespace DCReplace.UtilityInformation
{
	public class UtilityInfo
	{
		/// <summary>
		/// The type of player we are replacing
		/// </summary>
		public enum replacementType : ushort
		{
			Unknown = 0,
			NonUniqueScp = 1,
			Scp035 = 2,
			Serpents = 3,
			Spies = 4,
			Scp966 = 5
		}

	}
}
