using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using log4net;
using log4net.Config;
using Steamworks;
using Steamworks.Data;

namespace WorkshopTool
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static string PhoenixPointAppInstallDir { get; private set; }
		public static string LocalDataPath { get; private set; }

		public static readonly string DefaultProjectPath = 
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		public static readonly string DefaultAuthor = Environment.UserName;
		
		public const int PhoenixPointAppSteamId = 839770;
		public const int WorkshopToolSteamId = 1996080;
		
		public static LocalAppData LocalAppData { get; private set; }
		
		private static readonly ILog Log = LogManager.GetLogger(typeof(App));
		private static readonly string LocalDataPathExt = "Snapshot Games Inc/Phoenix Point/Steam/WorkshopTool/Data.xml";
		private static readonly string TemplateProjectPath = "NewMod";
		private static readonly string ModMetaJsonPath = "meta.json";
		private static readonly string[] ExtensionsToReplaceProjectName = { ".cs", ".csproj", ".json", ".sln", ".txt" };
		private static readonly string[] FileNamesToIgnore = { "msbuildcantcopyemptydirs" };
		
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			XmlConfigurator.Configure();
			
			try {
				SteamClient.Init(WorkshopToolSteamId);
			} catch (Exception ex) {
				Log.Fatal("Cannot initialize Steam", ex);
				
				MessageBox.Show(
					"Steam client is not running or you don't own Phoenix Point on Steam",
					"Steam Required",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
				
				Shutdown();
				return;
			}

			if (SteamApps.IsSubscribedToApp(PhoenixPointAppSteamId)) {
				PhoenixPointAppInstallDir = SteamApps.AppInstallDir(PhoenixPointAppSteamId);
			}

			string localLowPath = 
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
					.Replace("Roaming","LocalLow");

			LocalDataPath = Path.Combine(localLowPath, LocalDataPathExt);
			
			try {
				LocalAppData = ReadLocalData();
			} catch (Exception ex) {
				Log.Error("Cannot read application local data", ex);
				
				MessageBox.Show(
					$"Cannot read application local data: {ex.Message}",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}

			if (LocalAppData == null) {
				LocalAppData = new LocalAppData();
			}
			
			// WriteLocalData(new LocalAppData {
			// 	WorkshopItems = new List<WorkshopItem>() {
			// 		new WorkshopItem {
			// 			FileId = 5,
			// 			Title = "Blah",
			// 			ProjectPath = "Path",
			// 		}
			// 	}
			// });

			// Editor.NewCommunityFile
			// 	.ForAppId(PhoenixPointAppSteamId)
			// 	.WithTitle("Another PP Hat")
			// 	.WithDescription("The hat is very PP")
			// 	.WithPreviewFile("D:/Projects/Gallium/SteamSDK/tools/ContentBuilder/WorkshopTest/Preview.png")
			// 	// .WithContent("D:/Projects/Gallium/SteamSDK/tools/ContentBuilder/WorkshopTest")
			// 	.SubmitAsync().ContinueWith(result => {
			// 		Log.Debug(result);
			// 	});

			// CreateModProject("id.id", "Name Mod", "Author is me", "Description will be provided later", "Brum");
		}

		public static void WriteLocalData()
		{
			XmlSerializer ser = new XmlSerializer(typeof(LocalAppData));

			try {
				String dirName = Path.GetDirectoryName(LocalDataPath) ??
				                 throw new InvalidOperationException($"Invalid directory name: {LocalDataPath}");

				Directory.CreateDirectory(dirName);

				using (TextWriter writer = new StreamWriter(LocalDataPath)) {
					ser.Serialize(writer, LocalAppData);
				}
			} catch (Exception e) {
				Log.Error($"Cannot write local data to: {LocalDataPath}", e);
			}
		}
		
		public static void OpenItemPageInSteam(PublishedFileId fileId)
		{
			Process.Start($"steam://url/CommunityFilePage/{fileId}");
		}

		public static string GetShortName(string name)
		{
			return Regex.Replace(name, @"\s+", "");
		}

		public static async Task<WorkshopItem> CreateModProject(string id, string name, string author, string description,
			string targetDir)
		{
			Log.Info(
				$"Creating mod project: Id '{id}' Name '{name}' Author '{author}' Description '{description}' TargetDir '{targetDir}");

			// Copy the template project to the target dir
			var pathsToProcess = new List<string> {
				TemplateProjectPath,
			};

			string shortName = GetShortName(name);

			while (pathsToProcess.Count > 0) {
				string sourcePath = pathsToProcess[pathsToProcess.Count - 1];
				pathsToProcess.RemoveAt(pathsToProcess.Count - 1);
				string targetPath = Path.Combine(targetDir, sourcePath.Replace(TemplateProjectPath, shortName));

				if (File.GetAttributes(sourcePath).HasFlag(FileAttributes.Directory)) {
					pathsToProcess.AddRange(Directory.EnumerateFileSystemEntries(sourcePath));
					Directory.CreateDirectory(targetPath);
					continue;
				}

				// Ignore some files
				if (FileNamesToIgnore.Contains(Path.GetFileName(sourcePath).ToLowerInvariant())) {
					continue;
				}

				// Copy text file and replace the name of the project
				if (ExtensionsToReplaceProjectName.Contains(Path.GetExtension(sourcePath).ToLowerInvariant())) {
					using (StreamReader reader = File.OpenText(sourcePath)) {
						string contents = await reader.ReadToEndAsync();
						contents = contents.Replace(TemplateProjectPath, shortName);
						
						if (Path.GetFileName(sourcePath) == ModMetaJsonPath) {
							string assemblyName = shortName + ".dll";
							
							contents = contents
								.Replace("{Id}", id)
								.Replace("{AssemblyName}", assemblyName)
								.Replace("{Name}", name)
								.Replace("{Author}", author)
								.Replace("{Description}", description);
						}

						using (StreamWriter writer = File.CreateText(targetPath)) {
							await writer.WriteAsync(contents);
						}
					}

					continue;
				}

				// Just copy the file
				using (FileStream source = File.Open(sourcePath, FileMode.Open)) {
					using (FileStream target = File.Create(targetPath)) {
						await source.CopyToAsync(target);
					}
				}
			}
			
			string projectPath = Path.GetFullPath(Path.Combine(targetDir, shortName));
			
			return new WorkshopItem {
				Title = name,
				ProjectPath = projectPath,
			};
		}

		private static LocalAppData ReadLocalData()
		{
			if (!File.Exists(LocalDataPath)) {
				return null;
			}
			
			XmlSerializer ser = new XmlSerializer(typeof(LocalAppData));

			using (TextReader reader = new StreamReader(LocalDataPath)) {
				return (LocalAppData) ser.Deserialize(reader);
			}
		}

		public static void OpenModProject(string projectPath)
		{
			string solutionFile = Path.GetFileName(projectPath) + ".sln";
			string solutionPath = Path.Combine(projectPath, solutionFile);

			if (!File.Exists(solutionPath)) {
				Log.Error($"Cannot find project solution file: '{solutionPath}'");
				throw new FileNotFoundException($"Cannot find project solution file: '{solutionPath}'");
			}
			
			Process.Start(solutionPath);
		}

		public static void OpenModProjectInExplorer(string projectPath)
		{
			if (!Directory.Exists(projectPath)) {
				Log.Error($"Cannot find project path: '{projectPath}'");
				throw new FileNotFoundException($"Cannot find project path: '{projectPath}'");
			}
			
			Process.Start(projectPath);
		}
		
		public static void LaunchGame()
		{
			Process.Start($"steam://run/{PhoenixPointAppSteamId}");
		}
		
		public static string GetProjectDistDirectory(string projectDir)
		{
			return Path.Combine(projectDir, "Dist");
		}
	}
}
