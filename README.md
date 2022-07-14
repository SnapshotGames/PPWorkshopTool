# Phoenix Point Steam Workshop Tool
### Abstract
This tool is used to create mods for Phoenix Point and to upload them to Steam Workshop. The latest build of the tool can always be found on Steam as "Phoenix Point Workshop Tool". The tool is supported on Windows only. You need to be logged in to the Steam client and you need to own Phoenix Point in order to use the tool.

### Phoenix Point Mods and Mod Projects
A mod for Phoenix Point consists of a text file (meta.js) with some meta data and a managed .NET library that contains the mod code. You can use the Workshop Tool to create a standard MSBuild C# project which you can use to edit the mod meta data and to create the managed library. You can open and build the project with any IDE or editor that supports MSBuild projects - Visual Studio, VS Code, Rider, etc. After the mod is built you can test it locally in Phoenix Point. When you are ready with your changes you can use the tool to upload the mod to Steam Workshop.

### Basic Usage
In order to use the tool you need to own Phoenix Point on Steam and you need the Steam client running in the background. You can start the tool from Steam (search for "Phoenix Point Workshop Tool"), download and run a build from the [releases](https://github.com/SnapshotGames/PPWorkshopTool/releases) page or build and run it from source.
Main window looks like this:

![image](https://user-images.githubusercontent.com/2659777/178978945-06c3ff57-2727-4f50-99d2-44e26c8e242d.png)

In this window you will see a list of the mod projects that are on your computer and also your created Steam workshop items. There are several steps in creating a mod for Phoenix Point:

#### 1. Create a Mod Project

Select **Project -> New Mod Project...** from the menu. The "Create New Mod Project dialog appears:

![image](https://user-images.githubusercontent.com/2659777/178980557-5761e7fc-ce50-4863-b2e8-47d9e0c96ce1.png)

Fill in the metadata for your mod project. You can change everything later if you want.

- **Id** - The id of the mod. This is used to identify your mod. If some other mod depends on your mod the dependency will be tracked by this id. You shouldn't change this once you upload your mod to Steam Workshop
- **Name, Author, Description** - The name, author and description of the mod that will be shown in Phoenix Point when the mod is installed
- **Project Path** - The directory in which the mod project will be created. The mod project will be in a subdirectory within the Project Path

Click OK and the mod project will be created and added to the list:

![image](https://user-images.githubusercontent.com/2659777/178981667-eb39beeb-8219-43a0-877f-3bbb51290bdb.png)

Select the project from the list and select **Project -> Open Mod Project** from the menu. This will open the project solution file (.sln) in the default program that's associated with it:

![image](https://user-images.githubusercontent.com/2659777/178983753-9033e2a7-b153-49ef-a58c-be1d645ad6e0.png)

#### 2. Edit, Build and Test The Mod Project

The mod project contains a README.txt file that contains information how to write the mod logic and how the mods are loaded and initialized withing the game. There are comments in the project .cs files that explain some additional conceps. You can edit the project's metadata by editing the meta.js file. Anything you put in the **/Data** directory within your project will go into the output folder of the project unchanged. Put your mod's config and data here. The final build of the mod is copied to the **/Dist** directory.
Get familiar with the project, make your changes and when you are ready to test - build it using your IDE (for Visual Studio use **Build -> Build Solution** from the menu or press **F7**). The build process will deploy the mod for testing. If the game is running already you need to restart it in order for mod to be loaded. You can use the **Project -> (Re)Start the game** from the Workshop Tool menu to do that. Your mod should be visible in the game's MODS list. You can enable it by clicking in the check box next to the name:

![image](https://user-images.githubusercontent.com/2659777/178985772-9f47b363-9748-470a-94d4-7f4e9fd6883b.png)

Since .NET does not support unloading of managed libraries you need to restart the game every time you make a new build for your mod. We may find a workaround for this in the future.
When you build the project the mod and its data is copied to the following location: **%UserProfile%\AppData\LocalLow\Snapshot Games Inc\Phoenix Point\Steam\WorkshopTool\TestMod**. When the game is started if there's a mod there the game will load it. If you want to remove the current test mod you can use the **Project -> Remove Test Mod** menu item to delete everything in that directory.

#### 3. Create Steam Workshop Item

You need to create a Steam Workshop item before being able to upload data to Steam Workshop. Select your project from the list and then select **Workshop -> New Workshop Item..** from the menu. The "Create New Workshop Item" dialog appears. Fill in the metadata for your workshop item:

![image](https://user-images.githubusercontent.com/2659777/178986850-225f49e2-7150-477a-9ee6-d1cab518555d.png)

- **Title, Description** - The title and description of your Workshop item.
- **Tags** - Tags for easier grouping of mods within the Workshop store. These are free text, separated by comma. This field can be empty.
- **Thumbnail Path** - The main image of your Workshop Item. The image must be below 1MB and in .png, .jpg or .gif format.
- **Visibility** - The initial visibility of your Workshop item.

You can change everything except the thumbnail image later from the Workshop web interface.

Click OK and enter a log message for this change. Those messages will be visible in the change log in your Workshop item. If everything's fine your Workshop item will be created and linked to your mod project. You can use the **Workshop -> Open Workshop Item in Steam** menu item to open your new workshop item in the Steam client.

#### 4. Upload Data to Steam Workshop

When you are ready to upload your mod to Steam Workshop select your project from the list and then select **Workshop -> Upload Data to Workshop...** from the menu. Confirm the upload and enter the change log message. Everything that's currently in your project's **/Dist** directory will be uploaded to your Steam Workshop item.
