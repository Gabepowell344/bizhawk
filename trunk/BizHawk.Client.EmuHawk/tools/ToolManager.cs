﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;


namespace BizHawk.Client.EmuHawk
{
	public class ToolManager
	{
		// TODO: merge ToolHelper code where logical
		// For instance, add an IToolForm property called UsesCheats, so that a UpdateCheatRelatedTools() method can update all tools of this type
		// Also a UsesRam, and similar method
		private readonly List<IToolForm> _tools = new List<IToolForm>();

		/// <summary>
		/// Loads the tool dialog T, if it does not exist it will be created, if it is already open, it will be focused
		/// </summary>
		public IToolForm Load<T>() where T : IToolForm
		{
			return Load(typeof(T));
		}

		/// <summary>
		/// Loads a tool dialog of type toolType if it does not exist it will be
		/// created, if it is already open, it will be focused.
		/// </summary>
		public IToolForm Load(Type toolType)
		{
			if (!typeof(IToolForm).IsAssignableFrom(toolType))
				throw new ArgumentException(String.Format("Type {0} does not implement IToolForm.", toolType.Name));

			if (!ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, toolType))
				return null;

			var existingTool = _tools.FirstOrDefault(x => toolType.IsAssignableFrom(x.GetType()));

			if (existingTool != null)
			{
				if (existingTool.IsDisposed)
				{
					_tools.Remove(existingTool);
				}
				else
				{
					existingTool.Show();
					existingTool.Focus();
					return existingTool;
				}
			}

			var newTool = CreateInstance(toolType);

			ServiceInjector.UpdateServices(Global.Emulator.ServiceProvider, newTool);
			newTool.Restart();

			newTool.Show();
			return newTool;
		}

		/// <summary>
		/// Determines whether a given IToolForm is already loaded
		/// </summary>
		public bool IsLoaded<T>() where T : IToolForm
		{
			var existingTool = _tools.FirstOrDefault(x => x is T);
			if (existingTool != null)
			{
				return !existingTool.IsDisposed;
			}

			return false;
		}

		/// <summary>
		/// Returns true if an instance of T exists
		/// </summary>
		public bool Has<T>() where T : IToolForm
		{
			return _tools.Any(x => x is T && !x.IsDisposed);
		}

		/// <summary>
		/// Gets the instance of T, or creates and returns a new instance
		/// </summary>
		public IToolForm Get<T>() where T : IToolForm
		{
			var existingTool = _tools.FirstOrDefault(x => x is T);
			if (existingTool != null)
			{
				if (existingTool.IsDisposed)
				{
					Close<T>();
					return CreateInstance<T>();
				}
				else
				{
					return existingTool;
				}
			}
			else
			{
				return CreateInstance<T>();
			}
		}

		public void UpdateBefore()
		{
			var beforeList = _tools.Where(x => x.UpdateBefore);
			foreach (var tool in beforeList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.UpdateValues();
				}
			}
		}

		public void UpdateAfter()
		{
			var afterList = _tools.Where(x => !x.UpdateBefore);
			foreach (var tool in afterList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.UpdateValues();
				}
			}
		}

		/// <summary>
		/// Calls UpdateValues() on an instance of T, if it exists
		/// </summary>
		public void UpdateValues<T>() where T : IToolForm
		{
			CloseIfDisposed<T>();
			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				tool.UpdateValues();
			}
		}

		public void Restart()
		{
			// If Cheat tool is loaded, restarting will restart the list too anyway
			if (!GlobalWin.Tools.Has<Cheats>())
			{
				Global.CheatList.NewList(GenerateDefaultCheatFilename(), autosave: true);
			}

			var unavailable = new List<IToolForm>();

			foreach (var tool in _tools)
			{
				if (ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, tool.GetType()))
				{
					ServiceInjector.UpdateServices(Global.Emulator.ServiceProvider, tool);
					if ((tool.IsHandleCreated && !tool.IsDisposed) || tool is RamWatch) // Hack for Ram Watch - in display watches mode it wants to keep running even closed, it will handle disposed logic
					{
						tool.Restart();
					}
				}
				else
				{
					unavailable.Add(tool);
				}
			}

			foreach (var tool in unavailable)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		/// <summary>
		/// Calls Restart() on an instance of T, if it exists
		/// </summary>
		public void Restart<T>() where T : IToolForm
		{
			CloseIfDisposed<T>();
			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				tool.Restart();
			}
		}

		/// <summary>
		/// Runs AskSave on every tool dialog, false is returned if any tool returns false
		/// </summary>
		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) // User has elected to not be nagged
			{
				return true;
			}

			return _tools
				.Select(tool => tool.AskSaveChanges())
				.All(result => result);
		}

		/// <summary>
		/// Calls AskSave() on an instance of T, if it exists, else returns true
		/// The caller should interpret false as cancel and will back out of the action that invokes this call
		/// </summary>
		public bool AskSave<T>() where T : IToolForm
		{
			if (Global.Config.SupressAskSave) // User has elected to not be nagged
			{
				return true;
			}

			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				return tool.AskSaveChanges();
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// If T exists, this call will close the tool, and remove it from memory
		/// </summary>
		public void Close<T>() where T : IToolForm
		{
			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		public void Close(Type toolType)
		{
			var tool = _tools.FirstOrDefault(x => toolType.IsAssignableFrom(x.GetType()));

			if (tool != null)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		public void Close()
		{
			_tools.ForEach(x => x.Close());
			_tools.Clear();
		}

		private IToolForm CreateInstance<T>()
			where T: IToolForm
		{
			return CreateInstance(typeof(T));
		}

		private IToolForm CreateInstance(Type toolType)
		{
			var tool = (IToolForm)Activator.CreateInstance(toolType);

			// Add to our list of tools
			_tools.Add(tool);
			return tool;
		}

		private void CloseIfDisposed<T>() where T : IToolForm
		{
			var existingTool = _tools.FirstOrDefault(x => x is T);
			if (existingTool != null && existingTool.IsDisposed)
			{
				Close<T>();
			}
		}

		public void UpdateToolsBefore(bool fromLua = false)
		{
			if (Has<LuaConsole>())
			{
				if (!fromLua)
				{
					LuaConsole.StartLuaDrawing();
				}
			}

			UpdateBefore();
		}

		public void UpdateToolsAfter(bool fromLua = false)
		{
			if (!fromLua && Has<LuaConsole>())
			{
				LuaConsole.ResumeScripts(true);
			}

			GlobalWin.Tools.UpdateAfter();

			if (Has<LuaConsole>())
			{
				if (!fromLua)
				{
					LuaConsole.EndLuaDrawing();
				}
			}
		}

		public void FastUpdateBefore()
		{
			var beforeList = _tools.Where(x => x.UpdateBefore);
			foreach (var tool in beforeList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.FastUpdate();
				}
			}
		}

		public void FastUpdateAfter()
		{
			var afterList = _tools.Where(x => !x.UpdateBefore);
			foreach (var tool in afterList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.FastUpdate();
				}
			}
		}

		// Note: Referencing these properties creates an instance of the tool and persists it.  They should be referenced by type if this is not desired
		#region Tools

		public RamWatch RamWatch
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is RamWatch);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as RamWatch;
					}
				}
				
				var newTool = new RamWatch();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public RamSearch RamSearch
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is RamSearch);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as RamSearch;
					}
				}

				var newTool = new RamSearch();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public Cheats Cheats
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is Cheats);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as Cheats;
					}
				}

				var newTool = new Cheats();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public HexEditor HexEditor
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is HexEditor);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as HexEditor;
					}
				}

				var newTool = new HexEditor();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public VirtualpadTool VirtualPad
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is VirtualpadTool);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as VirtualpadTool;
					}
				}

				var newTool = new VirtualpadTool();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public SNESGraphicsDebugger SNESGraphicsDebugger
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is SNESGraphicsDebugger);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as SNESGraphicsDebugger;
					}
				}

				var newTool = new SNESGraphicsDebugger();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public LuaConsole LuaConsole
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is LuaConsole);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as LuaConsole;
					}
				}

				var newTool = new LuaConsole();
				_tools.Add(newTool);
				return newTool;
			}
		}

		#endregion

		#region Specialized Tool Loading Logic

		public void LoadRamWatch(bool loadDialog)
		{
			if (Global.Emulator.HasMemoryDomains())
			if (!IsLoaded<RamWatch>() && Global.Config.RecentWatches.AutoLoad && !Global.Config.RecentWatches.Empty)
			{
				GlobalWin.Tools.RamWatch.LoadFileFromRecent(Global.Config.RecentWatches.MostRecent);
			}

			if (loadDialog)
			{
				GlobalWin.Tools.Load<RamWatch>();
			}
		}

		public void LoadTraceLogger()
		{
			if (Global.Emulator.CpuTraceAvailable())
			{
				Load<TraceLogger>();
			}
		}

		public void LoadGameGenieEc()
		{
			if (Global.Emulator.SystemId == "NES")
			{
				Load<NESGameGenie>();
			}
			else if (Global.Emulator.SystemId == "SNES")
			{
				Load<SNESGameGenie>();
			}
			else if ((Global.Emulator.SystemId == "GB") || (Global.Game.System == "GG"))
			{
				Load<GBGameGenie>();
			}
			else if (Global.Emulator.SystemId == "GEN" && VersionInfo.DeveloperBuild)
			{
				Load<GenGameGenie>();
			}
		}

		#endregion

		public static string GenerateDefaultCheatFilename()
		{
			var pathEntry = Global.Config.PathEntries[Global.Game.System, "Cheats"]
			                ?? Global.Config.PathEntries[Global.Game.System, "Base"];

			var path = PathManager.MakeAbsolutePath(pathEntry.Path, Global.Game.System);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
			{
				f.Directory.Create();
			}

			return Path.Combine(path, PathManager.FilesystemSafeName(Global.Game) + ".cht");
		}
	}
}
