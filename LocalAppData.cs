using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Steamworks.Data;

namespace WorkshopTool
{
	[Serializable]
	public class LocalAppData
	{
		public string DefaultProjectPath;
		public string DefaultAuthor;
		public List<WorkshopItem> WorkshopItems = new List<WorkshopItem>();

		public WorkshopItem GetById(PublishedFileId id)
		{
			return WorkshopItems.FirstOrDefault(wi => wi.FileId == id);
		}

		public void Add(WorkshopItem item)
		{
			WorkshopItems.Add(item);
		}

		public string GetDefaultProjectPath()
		{
			return string.IsNullOrWhiteSpace(DefaultProjectPath)
				? App.DefaultProjectPath
				: DefaultProjectPath;
		}

		public string GetDefaultAuthor()
		{
			return string.IsNullOrWhiteSpace(DefaultAuthor)
				? App.DefaultAuthor
				: DefaultAuthor;
		}
	}
}
