﻿using Contralto.CPU;
using Contralto.Logging;

namespace Contralto.Memory
{
    public class Memory : IMemoryMappedDevice
    {
        public Memory()
        {
            // Set up handled addresses based on the system type.
            if (Configuration.SystemType == SystemType.AltoI)
            {
                _addresses = new MemoryRange[]
                {
                    new MemoryRange(0, _memTop),                                     // Main bank of RAM to 176777; IO page above this.                    
                };
            }
            else
            {
                _addresses = new MemoryRange[]
                {
                    new MemoryRange(0, _memTop),                                     // Main bank of RAM to 176777; IO page above this.
                    new MemoryRange(_xmBanksStart, (ushort)(_xmBanksStart + 16)),    // Memory bank registers
                };
            }

            Reset();
        }

        /// <summary>
        /// The top address of main memory (above which lies the I/O space)
        /// </summary>
        public static ushort RamTop
        {
            get { return _memTop; }
        }

        public void Reset()
        {
            // 4 64K banks, regardless of system type.  (Alto Is just won't use the extra memory.)
            _mem = new ushort[0x40000];
            _xmBanks = new ushort[16];
        }

        public ushort Read(int address, TaskType task, bool extendedMemory)
        {
            // Check for XM registers; this occurs regardless of XM flag since it's in the I/O page.
            if (address >= _xmBanksStart && address < _xmBanksStart + 16)
            {                
                return (ushort)(0xfff0 |_xmBanks[address - _xmBanksStart]);
            }
            else
            {
                /*
                if (extendedMemory)
                {
                    Log.Write(LogComponent.Memory, "Extended memory read, bank {0} address {1}, read {2}", GetBankNumber(task, extendedMemory), Conversion.ToOctal(address), Conversion.ToOctal(_mem[address + 0x10000 * GetBankNumber(task, extendedMemory)]));
                } */                
                address += 0x10000 * GetBankNumber(task, extendedMemory);
                return _mem[address];                
            }
        }

        public void Load(int address, ushort data, TaskType task, bool extendedMemory)
        {
            // Check for XM registers; this occurs regardless of XM flag since it's in the I/O page.
            if (address >= _xmBanksStart && address < _xmBanksStart + 16)
            {                
                _xmBanks[address - _xmBanksStart] = data;
                Log.Write(LogComponent.Memory, "XM register for task {0} set to bank {1} (normal), {2} (xm)",
                    (TaskType)(address - _xmBanksStart),
                    (data & 0xc) >> 2,
                    (data & 0x3));                
            }
            else
            {
                /*
                if (extendedMemory)
                {
                    Log.Write(LogComponent.Memory, "Extended memory write, bank {0} address {1}, data {2}", GetBankNumber(task, extendedMemory), Conversion.ToOctal(address), Conversion.ToOctal(data));
                } */
                address += 0x10000 * GetBankNumber(task, extendedMemory);              

                _mem[address] = data;               
            }
        }

        public MemoryRange[] Addresses
        {
            get { return _addresses; }
        }

        private int GetBankNumber(TaskType task, bool extendedMemory)
        {
            return extendedMemory ? (_xmBanks[(int)task]) & 0x3 : ((_xmBanks[(int)task]) & 0xc) >> 2;
        }

        private readonly MemoryRange[] _addresses;

        private static readonly ushort _memTop = 0xfdff;         // 176777
        private static readonly ushort _xmBanksStart = 0xffe0;   // 177740

        private ushort[] _mem;

        private ushort[] _xmBanks;
    }
}
