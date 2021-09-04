/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IGenericWriter.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.IO;
using System.Net;

namespace Server
{
    public interface IGenericWriter
    {
        long Position { get; }
        void Close();
        void Write(string value);
        void Write(long value);
        void Write(ulong value);
        void Write(int value);
        void Write(uint value);
        void Write(short value);
        void Write(ushort value);
        void Write(double value);
        void Write(float value);
        void Write(byte value);
        void Write(sbyte value);
        void Write(bool value);
        void Write(Serial serial);

        void Write(DateTime value)
        {
            var ticks = (value.Kind switch
            {
                DateTimeKind.Local       => value.ToUniversalTime(),
                DateTimeKind.Unspecified  => value.ToLocalTime().ToUniversalTime(),
                _                        => value
            }).Ticks;

            Write(ticks);
        }
        void WriteDeltaTime(DateTime value)
        {
            if (value == DateTime.MinValue)
            {
                Write(long.MinValue);
                return;
            }

            if (value == DateTime.MaxValue)
            {
                Write(long.MaxValue);
            }

            var ticks = (value.Kind switch
            {
                DateTimeKind.Local       => value.ToUniversalTime(),
                DateTimeKind.Unspecified  => value.ToLocalTime().ToUniversalTime(),
                _                        => value
            }).Ticks;

            // Technically supports negative deltas for times in the past
            Write(ticks - DateTime.UtcNow.Ticks);
        }
        void Write(IPAddress value)
        {
            Span<byte> stack = stackalloc byte[16];
            value.TryWriteBytes(stack, out var bytesWritten);
            Write((byte)bytesWritten);
            Write(stack[..bytesWritten]);
        }
        void Write(TimeSpan value)
        {
            Write(value.Ticks);
        }

        public void Write(decimal value)
        {
            var bits = decimal.GetBits(value);

            for (var i = 0; i < 4; ++i)
            {
                Write(bits[i]);
            }
        }
        void WriteEncodedInt(int value)
        {
            var v = (uint)value;

            while (v >= 0x80)
            {
                Write((byte)(v | 0x80));
                v >>= 7;
            }

            Write((byte)v);
        }
        void Write(Point3D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
            Write(value.m_Z);
        }
        void Write(Point2D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
        }
        void Write(Rectangle2D value)
        {
            Write(value.Start);
            Write(value.End);
        }
        void Write(Rectangle3D value)
        {
            Write(value.Start);
            Write(value.End);
        }
        void Write(Map value) => Write((byte)(value?.MapIndex ?? 0xFF));
        void Write(Race value) => Write((byte)(value?.RaceIndex ?? 0xFF));
        void Write(ReadOnlySpan<byte> bytes);
        unsafe void WriteEnum<T>(T value) where T : unmanaged, Enum
        {
            switch (sizeof(T))
            {
                default: throw new ArgumentException($"Argument of type {typeof(T)} is not a normal enum");
                case 1:
                    {
                        Write(*(byte*)&value);
                        break;
                    }
                case 2:
                    {
                        Write(*(ushort*)&value);
                        break;
                    }
                case 4:
                    {
                        WriteEncodedInt(*(int*)&value);
                        break;
                    }
                case 8:
                    {
                        Write(*(ulong*)&value);
                        break;
                    }
            }
        }
        void Write(Guid guid)
        {
            Span<byte> stack = stackalloc byte[16];
            guid.TryWriteBytes(stack);
            Write(stack);
        }

        long Seek(long offset, SeekOrigin origin);
    }
}
