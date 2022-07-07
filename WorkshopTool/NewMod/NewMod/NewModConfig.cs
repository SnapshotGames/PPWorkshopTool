using PhoenixPoint.Modding;

namespace NewMod
{
	/// <summary>
	/// ModConfig is mod settings that players can change from within the game.
	/// Config is only editable from players in main menu.
	/// Only one config can exist per mod assembly.
	/// Config is serialized on disk as json.
	/// </summary>
	public class NewModConfig : ModConfig
	{
		/// Only public fields are serialized.
		/// Supported types for in-game UI are:
		public int IntegerValue;
		public float FloatValue;
		public bool BoolValue;

		public enum CustomEnum
		{
			A, B ,C
		}
		public CustomEnum CustomEnumValue;
	}
}
