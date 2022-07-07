using System;
using Steamworks.Data;

namespace WorkshopTool
{
	public enum WorkshopItemVisibility
	{
		Private,
		FriendsOnly,
		Public,
	}
	
	[Serializable]
	public class WorkshopItem
	{
		public PublishedFileId FileId;
		public string Title;
		public string Description;
		public string Tags;
		public WorkshopItemVisibility Visibility = WorkshopItemVisibility.Private;
		public string ProjectPath;
	}
}
