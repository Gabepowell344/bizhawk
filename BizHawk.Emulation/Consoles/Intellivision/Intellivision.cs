﻿using System;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.CP1610;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision : IEmulator
	{
		byte[] Rom;
		GameInfo Game;

		CP1610 Cpu;
		ICart Cart;
		STIC Stic;
		PSG Psg;

		public void Connect()
		{
			Cpu.SetIntRM(Stic.GetSr1());
			Cpu.SetBusRq(Stic.GetSr2());
			Stic.SetSst(Cpu.GetBusAk());
		}

		public void LoadExecutiveRom(string path)
		{
			var erom = File.ReadAllBytes(path);
			if (erom.Length != 8192)
			{
				throw new ApplicationException("EROM file is wrong size - expected 8192 bytes");
			}
			int index = 0;
			// Combine every two bytes into a word.
			while (index + 1 < erom.Length)
			{
				ExecutiveRom[index / 2] = (ushort)((erom[index++] << 8) | erom[index++]);
			}
		}

		public void LoadGraphicsRom(string path)
		{
			GraphicsRom = File.ReadAllBytes(path);
			if (GraphicsRom.Length != 2048)
			{
				throw new ApplicationException("GROM file is wrong size - expected 2048 bytes");
			}
		}

		public Intellivision(CoreComm comm, GameInfo game, byte[] rom)
		{
			CoreComm = comm;

			Rom = rom;
			Game = game;
			Cart = new Intellicart();
			if (Cart.Parse(Rom) == -1)
			{
				Cart = new Cartridge();
				Cart.Parse(Rom);
			}

			Cpu = new CP1610();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.Reset();

			Stic = new STIC();
			Stic.ReadMemory = ReadMemory;
			Stic.WriteMemory = WriteMemory;
			Stic.Reset();

			Psg = new PSG();
			Psg.ReadMemory = ReadMemory;
			Psg.WriteMemory = WriteMemory;

			Connect();

			Cpu.LogData();
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			Frame++;
			Cpu.AddPendingCycles(14934);
			while (Cpu.GetPendingCycles() > 0)
			{
				int cycles = Cpu.Execute();
				Stic.Execute(cycles);
				Connect();
				Cpu.LogData();
			}
		}

		public IVideoProvider VideoProvider { get { return Stic; } }
		public ISoundProvider SoundProvider { get { return NullSound.SilenceProvider; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(NullSound.SilenceProvider, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public static readonly ControllerDefinition IntellivisionController =
			new ControllerDefinition
			{
				Name = "Intellivision Controller",
				BoolButtons = {
					"P1 Up", "P1 Down", "P1 Left", "P1 Right",
					"P1 L", "P1 R",
					"P1 Key0", "P1 Key1", "P1 Key2", "P1 Key3", "P1 Key4", "P1 Key5",
					"P1 Key6", "P1 Key7", "P1 Key8", "P1 Key9", "P1 Enter", "P1 Clear",

					"P2 Up", "P2 Down", "P2 Left", "P2 Right",
					"P2 L", "P2 R",
					"P2 Key0", "P2 Key1", "P2 Key2", "P2 Key3", "P2 Key4", "P2 Key5",
					"P2 Key6", "P2 Key7", "P2 Key8", "P2 Key9", "P2 Enter", "P2 Clear"
				}
			};

		public ControllerDefinition ControllerDefinition
		{
			get { return IntellivisionController; }
		}

		public IController Controller { get; set; }
		public int Frame { get; set; }

		public int LagCount
		{
			get { return 0; }
			set { }
		}

		public bool IsLagFrame { get { return false; } }

		public string SystemId
		{
			get { return "INTV"; }
		}

		public bool DeterministicEmulation { get { return true; } }


		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified
		{
			get { return false; }
			set { }
		}

		public void ResetFrameCounter()
		{
			Frame = 0;
			LagCount = 0;
			//IsLagFrame = false;
		}

		public void SaveStateText(TextWriter writer)
		{
			throw new NotImplementedException();
		}

		public void LoadStateText(TextReader reader)
		{
			throw new NotImplementedException();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public bool BinarySaveStatesPreferred { get { return false; } }

		public CoreComm CoreComm { get; private set; }

		public IList<MemoryDomain> MemoryDomains
		{
			get { throw new NotImplementedException(); }
		}

		public MemoryDomain MainMemory
		{
			get { throw new NotImplementedException(); }
		}

		public void Dispose()
		{
		}
	}
}