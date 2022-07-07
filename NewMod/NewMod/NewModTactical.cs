using Base.Serialization.General;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Levels;

namespace NewMod
{
	/// <summary>
	/// Mod's custom save data for tactical.
	/// </summary>
	[SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
	public class NewModTacInstanceData
	{
		public int ExampleData;
	}

	/// <summary>
	/// Represents a mod instance specific for Tactical game.
	/// Each time Tactical level is loaded, new mod's ModTactical is created. 
	/// </summary>
	public class NewModTactical : ModTactical
	{
		/// <summary>
		/// Called when Tactical starts.
		/// </summary>
		public override void OnTacticalStart() {
			/// Tactical level controller is accessible at any time.
			TacticalLevelController tacController = Controller;
			/// ModMain is accesible at any time
			NewModMain main = (NewModMain)Main;
		}
		/// <summary>
		/// Called when Tactical ends.
		/// </summary>
		public override void OnTacticalEnd() {
			base.OnTacticalEnd();
		}
		/// <summary>
		/// Called when Tactical save is being process. At this point level is already created, but TacticalStart is not called.
		/// </summary>
		/// <param name="data">Instance data serialized for this mod. Cannot be null.</param>
		public override void ProcessTacticalInstanceData(object instanceData) {
			NewModTacInstanceData data = (NewModTacInstanceData)instanceData;
		}
		/// <summary>
		/// Called when Tactical save is going to be generated, giving mod option for custom save data.
		/// </summary>
		/// <returns>Object to serialize or null if not used.</returns>
		public override object RecordTacticalInstanceData() {
			 return new NewModTacInstanceData() { ExampleData = 5 };
		}
		/// <summary>
		/// Called when new turn starts in tactical. At this point all factions must play in their order.
		/// </summary>
		/// <param name="turnNumber">Current turn number</param>
		public override void OnNewTurn(int turnNumber) {
		}
	}
}
