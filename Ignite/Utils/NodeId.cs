﻿using static Ignite.Node;

namespace Ignite.Utils
{
    public class NodeId
    {
        public ulong Id { get; internal set; }
        private const ulong FlagSectionMask = 0xFFFF000000000000; // Higher 16 bits mask
        private const ulong RestOfBitsMask = 0x0000FFFFFFFFFFFF; // Lower 48 bits mask

        public void SetFlags(Node.Flags flags)
        {
            Id = (Id & RestOfBitsMask) | ((ulong)flags << 48);
        }

        public void RemoveFlags(Node.Flags flags)
        {
            Id ^= (ulong)flags;
        }

        public bool HasFlag(Node.Flags flags)
        {
            return (Id & (ulong)flags) == (ulong)flags; // Check if the flag is set using bitwise AND
        }

        public static ulong LastGeneratedId { get; private set; } = 0;

        private static uint CurrentId = 0;
        private static ushort CurrentGenerationId = 0;

        public static ulong Next(Node.Flags flags)
        {
            ulong id = (ulong)flags;

            if( ++CurrentId < UInt32.MaxValue )
                id += CurrentId;
            else
            {
                CurrentId = 0;
                if(++CurrentGenerationId < UInt16.MaxValue)
                    id += CurrentGenerationId;
                else
                    CurrentGenerationId = 0;
            }

            LastGeneratedId = id;
            return id;
        }
    }
}
