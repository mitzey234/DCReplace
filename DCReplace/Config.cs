using Exiled.API.Interfaces;
using System.ComponentModel;

namespace DCReplace
{
	public class Config : IConfig
	{
		public bool IsEnabled { get; set; } = true;

		[Description("The message to show debug messages.")]
		public bool debugEnabled { get; set; } = false;
	}
}
