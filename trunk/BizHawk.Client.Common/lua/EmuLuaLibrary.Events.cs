﻿using System;
using System.Collections.Generic;
using System.Linq;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class EventLuaLibrary : LuaLibraryBase
	{
		public EventLuaLibrary(Action<string> logOutputCallback)
			: base()
		{
			LogOutputCallback = logOutputCallback;
		}

		public override string Name { get { return "event"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"onframeend",
					"onframestart",
					"oninputpoll",
					"onloadstate",
					"onmemoryread",
					"onmemorywrite",
					"onsavestate",
					"unregisterbyid",
					"unregisterbyname",
				};
			}
		}

		public Action<string> LogOutputCallback = null;

		#region Events Library Helpers

		private readonly LuaFunctionList _luaFunctions = new LuaFunctionList();

		public LuaFunctionList RegisteredFunctions { get { return _luaFunctions; } }

		public void CallSaveStateEvent(string name)
		{
			List<NamedLuaFunction> lfs = _luaFunctions.Where(x => x.Event == "OnSavestateSave").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call(name);
					}
				}
				catch (SystemException e)
				{
					LogOutputCallback(
						"error running function attached by lua function event.onsavestate" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		public void CallLoadStateEvent(string name)
		{
			List<NamedLuaFunction> lfs = _luaFunctions.Where(x => x.Event == "OnSavestateLoad").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call(name);
					}
				}
				catch (SystemException e)
				{
					LogOutputCallback(
						"error running function attached by lua function event.onloadstate" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		public void CallFrameBeforeEvent()
		{
			List<NamedLuaFunction> lfs = _luaFunctions.Where(x => x.Event == "OnFrameStart").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call();
					}
				}
				catch (SystemException e)
				{
					LogOutputCallback(
						"error running function attached by lua function event.onframestart" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		public void CallFrameAfterEvent()
		{
			List<NamedLuaFunction> lfs = _luaFunctions.Where(x => x.Event == "OnFrameEnd").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call();
					}
				}
				catch (SystemException e)
				{
					LogOutputCallback(
						"error running function attached by lua function event.onframeend" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		#endregion

		public string event_onframeend(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnFrameEnd", LogOutputCallback, name);
			_luaFunctions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public string event_onframestart(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnFrameStart", LogOutputCallback, name);
			_luaFunctions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public void event_oninputpoll(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnInputPoll", LogOutputCallback, name);
			_luaFunctions.Add(nlf);
			Global.Emulator.CoreComm.InputCallback.Add(nlf.Callback);
		}

		public string event_onloadstate(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateLoad", LogOutputCallback, name);
			_luaFunctions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public string event_onmemoryread(LuaFunction luaf, object address = null, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnMemoryRead", LogOutputCallback, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddRead(nlf.Callback, (address != null ? LuaUInt(address) : (uint?)null));
			return nlf.GUID.ToString();
			
		}

		public string event_onmemorywrite(LuaFunction luaf, object address = null, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnMemoryWrite", LogOutputCallback, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddWrite(nlf.Callback, (address != null ? LuaUInt(address) : (uint?)null));
			return nlf.GUID.ToString();
		}

		public string event_onsavestate(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateSave", LogOutputCallback, name);
			_luaFunctions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public bool event_unregisterbyid(object guid)
		{
			foreach (NamedLuaFunction nlf in _luaFunctions)
			{
				if (nlf.GUID.ToString() == guid.ToString())
				{
					_luaFunctions.RemoveFunction(nlf);
					return true;
				}
			}

			return false;
		}

		public bool event_unregisterbyname(object name)
		{
			foreach (NamedLuaFunction nlf in _luaFunctions)
			{
				if (nlf.Name == name.ToString())
				{
					_luaFunctions.RemoveFunction(nlf);
					return true;
				}
			}

			return false;
		}
	}
}
