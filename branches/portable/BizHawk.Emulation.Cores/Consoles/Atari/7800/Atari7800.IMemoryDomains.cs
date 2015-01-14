﻿using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using EMU7800.Core;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	public partial class Atari7800 : IMemoryDomains
	{
		private List<MemoryDomain> _MemoryDomains;

		public IMemoryDomainList MemoryDomains { get; private set; }

		public void SetupMemoryDomains(HSC7800 hsc7800)
		{
			// reset memory domains
			if (_MemoryDomains == null)
			{
				_MemoryDomains = new List<MemoryDomain>();
				if (theMachine is Machine7800)
				{
					_MemoryDomains.Add(new MemoryDomain(
						"RAM1", 0x800, MemoryDomain.Endian.Unknown,
						delegate(int addr)
						{
							if (addr < 0 || addr >= 0x800)
								throw new ArgumentOutOfRangeException();
							return ((Machine7800)theMachine).RAM1[(ushort)addr];
						},
						delegate(int addr, byte val)
						{
							if (addr < 0 || addr >= 0x800)
								throw new ArgumentOutOfRangeException();
							((Machine7800)theMachine).RAM1[(ushort)addr] = val;
						}));
					_MemoryDomains.Add(new MemoryDomain(
						"RAM2", 0x800, MemoryDomain.Endian.Unknown,
						delegate(int addr)
						{
							if (addr < 0 || addr >= 0x800)
								throw new ArgumentOutOfRangeException();
							return ((Machine7800)theMachine).RAM2[(ushort)addr];
						},
						delegate(int addr, byte val)
						{
							if (addr < 0 || addr >= 0x800)
								throw new ArgumentOutOfRangeException();
							((Machine7800)theMachine).RAM2[(ushort)addr] = val;
						}));
					_MemoryDomains.Add(new MemoryDomain(
						"BIOS ROM", bios.Length, MemoryDomain.Endian.Unknown,
						delegate(int addr)
						{
							return bios[addr];
						},
						delegate(int addr, byte val)
						{
						}));
					if (hsc7800 != null)
					{
						_MemoryDomains.Add(new MemoryDomain(
							"HSC ROM", hsbios.Length, MemoryDomain.Endian.Unknown,
							delegate(int addr)
							{
								return hsbios[addr];
							},
							delegate(int addr, byte val)
							{
							}));
						_MemoryDomains.Add(new MemoryDomain(
							"HSC RAM", hsram.Length, MemoryDomain.Endian.Unknown,
							delegate(int addr)
							{
								return hsram[addr];
							},
							delegate(int addr, byte val)
							{
								hsram[addr] = val;
							}));
					}
					_MemoryDomains.Add(new MemoryDomain(
						"System Bus", 65536, MemoryDomain.Endian.Unknown,
						delegate(int addr)
						{
							if (addr < 0 || addr >= 0x10000)
								throw new ArgumentOutOfRangeException();
							return theMachine.Mem[(ushort)addr];
						},
						delegate(int addr, byte val)
						{
							if (addr < 0 || addr >= 0x10000)
								throw new ArgumentOutOfRangeException();
							theMachine.Mem[(ushort)addr] = val;
						}));
				}
				else // todo 2600?
				{
				}
				MemoryDomains = new MemoryDomainList(_MemoryDomains);
			}
		}
	}
}
