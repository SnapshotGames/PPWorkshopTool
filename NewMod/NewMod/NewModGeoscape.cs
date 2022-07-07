using Base.Serialization.General;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Modding;
using System.Collections.Generic;

namespace NewMod
{
	/// <summary>
	/// Mod's custom save data for geoscape.
	/// </summary>
	[SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
	public class NewModGSInstanceData
	{
		public int ExampleData;
	}

	/// <summary>
	/// Represents a mod instance specific for Geoscape game.
	/// Each time Geoscape level is loaded, new mod's ModGeoscape is created.
	/// </summary>
	public class NewModGeoscape : ModGeoscape
	{
		/// <summary>
		/// Called when Geoscape starts.
		/// </summary>
		public override void OnGeoscapeStart() {
			/// Geoscape level controller is accessible at any time.
			GeoLevelController gsController = Controller;
			/// ModMain is accesible at any time
			NewModMain main = (NewModMain)Main;
		}
		/// <summary>
		/// Called when Geoscape ends.
		/// </summary>
		public override void OnGeoscapeEnd() {

		}

		/// <summary>
		/// Called when Geoscape save is going to be generated, giving mod option for custom save data.
		/// </summary>
		/// <returns>Object to serialize or null if not used.</returns>
		public override object RecordGeoscapeInstanceData() {
			return new NewModGSInstanceData() { ExampleData = 5 };
		}
		/// <summary>
		/// Called when Geoscape save is being process. At this point level is already created, but GeoscapeStart is not called.
		/// </summary>
		/// <param name="instanceData">Instance data serialized for this mod. Cannot be null.</param>
		public override void ProcessGeoscapeInstanceData(object instanceData) {
			NewModGSInstanceData data = (NewModGSInstanceData)instanceData;
		}

		/// <summary>
		/// Called when new Geoscape world is generating. This only happens on new game.
		/// Useful to modify initial spawned sites.
		/// </summary>
		/// <param name="setup">Main geoscape setup object.</param>
		/// <param name="worldSites">Sites to spawn and start simulating.</param>
		public override void OnGeoscapeNewWorldInit(GeoInitialWorldSetup setup, IList<GeoSiteSceneDef.SiteInfo> worldSites) {
		}
		/// <summary>
		/// Called when generated Geoscape world will pass through simulation step. This only happens on new game.
		/// Useful to modify game startup setup before simulation.
		/// </summary>
		/// <param name="setup">Main geoscape setup object.</param>
		/// <param name="context">Context for game setup.</param>
		public override void OnGeoscapeNewWorldSimulationStart(GeoInitialWorldSetup setup, GeoInitialWorldSetup.SimContext context) {
		}
	}
}
