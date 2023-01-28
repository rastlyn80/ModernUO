/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TileMatrix.cs                                                   *
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
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server;

public class TileMatrix
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TileMatrix));
    private static readonly List<TileMatrix> _instances = new();
    private static readonly StaticTile[][][] _emptyStaticBlock;
    private static readonly LandTile[] _invalidLandBlock = new LandTile[196];

    private readonly StaticTile[][][][][] _staticTiles;
    private readonly LandTile[][][] _landTiles;
    private readonly int _fileIndex;
    private readonly Map _map;
    private readonly int[][] _staticPatches;
    private readonly int[][] _landPatches;
    private readonly List<TileMatrix> _fileShare = new();

    public TileMatrixPatch Patch { get; }
    public int BlockWidth { get; }
    public int BlockHeight { get; }

    public static bool Pre6000ClientSupport { get; private set; }

    public static void Configure()
    {
        // Set to true to support < 6.0.0 clients where map0.mul is both Felucca & Trammel
        var isPre6000Trammel = UOClient.ServerClientVersion != null && UOClient.ServerClientVersion < ClientVersion.Version6000;
        Pre6000ClientSupport = ServerConfiguration.GetSetting("maps.enablePre6000Trammel", isPre6000Trammel);
    }

    public static bool IsInvalidLandBlock(LandTile[] landblock) => landblock == _invalidLandBlock;

    static TileMatrix()
    {
        var emptyTiles = Array.Empty<StaticTile>();

        // This works since we assume each block is 8 tiles.
        _emptyStaticBlock = new StaticTile[8][][];

        for (var i = 0; i < 8; ++i)
        {
            _emptyStaticBlock[i] = new StaticTile[8][];

            for (var j = 0; j < 8; ++j)
            {
                _emptyStaticBlock[i][j] = emptyTiles;
            }
        }
    }

    public TileMatrix(Map owner, int fileIndex, int mapID, int width, int height)
    {
        for (var i = 0; i < _instances.Count; ++i)
        {
            var tm = _instances[i];

            if (tm._fileIndex == fileIndex)
            {
                tm._fileShare.Add(this);
                _fileShare.Add(tm);
                break;
            }
        }

        _instances.Add(this);

        _fileIndex = fileIndex;
        BlockWidth = width >> 3;
        BlockHeight = height >> 3;

        _map = owner;

        _staticPatches = new int[BlockWidth][];
        _landPatches = new int[BlockWidth][];

        if (fileIndex == 0x7F)
        {
            return;
        }

        FileStream mapStream = null;
        UOPIndex uopMapIndex = null;

        var mapFileIndex = Pre6000ClientSupport && mapID == 1 ? 0 : fileIndex;

        var mapPath = Core.FindDataFile($"map{mapFileIndex}.mul", false);

        if (mapPath != null)
        {
            mapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        else
        {
            mapPath = Core.FindDataFile($"map{mapFileIndex}LegacyMUL.uop", false);

            if (mapPath != null)
            {
                mapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                uopMapIndex = new UOPIndex(mapStream);
            }
            else
            {
                logger.Warning("{File} was not found.", $"map{mapFileIndex}.mul");
            }
        }

        _landTiles = new LandTile[BlockWidth][][];

        ReadAllLandBlocks(mapStream, uopMapIndex);
        mapStream?.Dispose();

        BinaryReader staticIndexReader = null;

        var indexPath = Core.FindDataFile($"staidx{mapFileIndex}.mul", false);

        if (indexPath != null)
        {
            var stream = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            staticIndexReader = new BinaryReader(stream);
        }
        else
        {
            logger.Warning("{File} was not found.", $"staidx{mapFileIndex}.mul");
        }

        var staticsPath = Core.FindDataFile($"statics{mapFileIndex}.mul", false);

        if (staticsPath != null)
        {
            var staticStream = new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            _staticTiles = new StaticTile[BlockWidth][][][][];
            ReadAllStaticBlocks(staticIndexReader, staticStream);

            staticStream.Dispose();
        }
        else
        {
            logger.Warning("{File} was not found.", $"statics{fileIndex}.mul");
        }

        staticIndexReader?.Dispose();

        Patch = new TileMatrixPatch(this, fileIndex);
    }

    public StaticTile[][][] EmptyStaticBlock => _emptyStaticBlock;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void SetStaticBlock(int x, int y, StaticTile[][][] value)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return;
        }

        _staticTiles[x] ??= new StaticTile[BlockHeight][][][];
        _staticTiles[x][y] = value;

        _staticPatches[x] ??= new int[(BlockHeight + 31) >> 5];
        _staticPatches[x][y >> 5] |= 1 << (y & 0x1F);
    }

    public StaticTile[][][] GetStaticBlock(int blockX, int blockY)
    {
        if (blockX < 0 || blockY < 0 || blockX >= BlockWidth || blockY >= BlockHeight)
        {
            return _emptyStaticBlock;
        }

        _staticTiles[blockX] ??= new StaticTile[BlockHeight][][][];

        var tiles = _staticTiles[blockX][blockY];

        if (tiles == null)
        {
            for (var i = 0; tiles == null && i < _fileShare.Count; ++i)
            {
                var shared = _fileShare[i];

                // Out of bounds
                if (blockX >= shared.BlockWidth || blockY >= shared.BlockHeight)
                {
                    continue;
                }

                var theirTiles = shared._staticTiles[blockX];

                // No shared tile matrix
                if (theirTiles == null)
                {
                    continue;
                }

                tiles = theirTiles[blockY];

                var theirBits = shared._staticPatches[blockX];

                if (theirBits != null && (theirBits[blockY >> 5] & (1 << (blockY & 0x1F))) != 0)
                {
                    tiles = null;
                }
            }

            _staticTiles[blockX][blockY] = tiles;
        }

        return tiles;
    }

    public StaticTile[] GetStaticTiles(int x, int y)
    {
        var tiles = GetStaticBlock(x >> 3, y >> 3);

        return tiles[x & 0x7][y & 0x7];
    }

    private readonly TileList m_TilesList = new();

    [MethodImpl(MethodImplOptions.Synchronized)]
    public StaticTile[] GetStaticTiles(int x, int y, bool multis)
    {
        var tiles = GetStaticBlock(x >> 3, y >> 3);

        if (multis)
        {
            var eable = _map.GetMultiTilesAt(x, y);

            if (eable == Map.NullEnumerable<StaticTile[]>.Instance)
            {
                return tiles[x & 0x7][y & 0x7];
            }

            var any = false;

            foreach (var multiTiles in eable)
            {
                if (!any)
                {
                    any = true;
                }

                m_TilesList.AddRange(multiTiles);
            }

            eable.Free();

            if (!any)
            {
                return tiles[x & 0x7][y & 0x7];
            }

            m_TilesList.AddRange(tiles[x & 0x7][y & 0x7]);

            return m_TilesList.ToArray();
        }

        return tiles[x & 0x7][y & 0x7];
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void SetLandBlock(int x, int y, LandTile[] value)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return;
        }

        _landTiles[x] ??= new LandTile[BlockHeight][];
        _landTiles[x][y] = value;

        _landPatches[x] ??= new int[(BlockHeight + 31) >> 5];
        _landPatches[x][y >> 5] |= 1 << (y & 0x1F);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public LandTile[] GetLandBlock(int x, int y)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return _invalidLandBlock;
        }

        _landTiles[x] ??= new LandTile[BlockHeight][];

        var tiles = _landTiles[x][y];

        if (tiles != null)
        {
            return tiles;
        }

        for (var i = 0; tiles == null && i < _fileShare.Count; ++i)
        {
            var shared = _fileShare[i];

            lock (shared)
            {
                if (x < shared.BlockWidth && y < shared.BlockHeight)
                {
                    var theirTiles = shared._landTiles[x];

                    if (theirTiles != null)
                    {
                        tiles = theirTiles[y];
                    }

                    if (tiles != null)
                    {
                        var theirBits = shared._landPatches[x];

                        if (theirBits != null && (theirBits[y >> 5] & (1 << (y & 0x1F))) != 0)
                        {
                            tiles = null;
                        }
                    }
                }
            }
        }

        _landTiles[x][y] = tiles;

        return tiles;
    }

    public LandTile GetLandTile(int x, int y)
    {
        var tiles = GetLandBlock(x >> 3, y >> 3);

        return tiles[((y & 0x7) << 3) + (x & 0x7)];
    }

    private unsafe void ReadAllStaticBlocks(BinaryReader staticIndexReader, FileStream staticStream)
    {
        if (staticIndexReader == null || staticStream == null)
        {
            return;
        }

        var tileBuffer = new StaticTile[128];
        var tileLists = new TileList[8][];
        for (var i = 0; i < 8; ++i)
        {
            tileLists[i] = new TileList[8];

            for (var j = 0; j < 8; ++j)
            {
                tileLists[i][j] = new TileList();
            }
        }

        var x = 0;
        var y = 0;

        try
        {
            do
            {
                if (y == 0)
                {
                    _staticTiles[x] = new StaticTile[BlockHeight][][][];
                }

                var lookup = staticIndexReader.ReadInt32();
                var length = staticIndexReader.ReadInt32();
                staticIndexReader.ReadUInt32();

                if (lookup != -1 && length > 0)
                {
                    var count = length / 7;

                    if (staticStream.Position != lookup)
                    {
                        staticStream.Seek(lookup, SeekOrigin.Begin);
                    }

                    if (tileBuffer.Length < count)
                    {
                        tileBuffer = new StaticTile[count];
                    }

                    var staTiles = tileBuffer;

                    fixed (StaticTile* pTiles = staTiles)
                    {
                        int bytesRead = staticStream.Read(new Span<byte>(pTiles, length));
                        if (bytesRead < length)
                        {
                            logger.Warning("Not enough bytes read from {File}.", staticStream.Name);
                        }

                        StaticTile* pCur = pTiles, pEnd = pTiles + count;

                        // We have to loop them and stage them because they aren't in a particular order.
                        while (pCur < pEnd)
                        {
                            // X/Y must be between 0 and 7
                            tileLists[pCur->m_X][pCur->m_Y].Add(pCur++);
                        }

                        var tiles = new StaticTile[8][][];

                        for (var i = 0; i < 8; ++i)
                        {
                            tiles[i] = new StaticTile[8][];

                            for (var j = 0; j < 8; ++j)
                            {
                                if (tileLists[i][j].Count == 0)
                                {
                                    tiles[i][j] = _emptyStaticBlock[i][j];
                                }
                                else
                                {
                                    tiles[i][j] = tileLists[i][j].ToArray();
                                }
                            }
                        }

                        _staticTiles[x][y] = tiles;
                    }
                }
                else
                {
                    _staticTiles[x][y] = _emptyStaticBlock;
                }

                if (++y >= BlockHeight)
                {
                    y = 0;
                    x++;
                }
            } while (x < BlockWidth);
        }
        catch (EndOfStreamException ex)
        {
            if (Core.Now >= m_NextStaticWarning)
            {
                logger.Warning(ex, "Warning: End of stream for map file for {Map} ({X}, {Y})", _map, x, y);
                m_NextStaticWarning = Core.Now + TimeSpan.FromSeconds(10);
            }

            _staticTiles[x][y] = _emptyStaticBlock;
        }
    }

    private DateTime m_NextStaticWarning;
    private DateTime m_NextLandWarning;

    public void Force()
    {
        if (AssemblyHandler.Assemblies?.Length > 0)
        {
            return;
        }

        throw new Exception("No assemblies were loaded, therefore we cannot load TileMatrix.");
    }

    private unsafe void ReadAllLandBlocks(FileStream mapStream, UOPIndex uopIndex)
    {
        if (mapStream == null)
        {
            return;
        }

        var x = 0;
        var y = 0;

        try
        {
            do
            {
                if (y == 0)
                {
                    _landTiles[x] = new LandTile[BlockHeight][];
                }

                // Offset for entry is 4 (header) * (4 + (3 * 64))
                var offset = (x * BlockHeight + y) * 196 + 4;

                if (uopIndex != null)
                {
                    offset = uopIndex.Lookup(offset);
                }

                mapStream.Seek(offset, SeekOrigin.Begin);

                var tiles = new LandTile[64];

                fixed (LandTile* pTiles = tiles)
                {
                    var bytesRead = mapStream.Read(new Span<byte>(pTiles, 192));
                    if (bytesRead < 192)
                    {
                        logger.Warning("Not enough bytes read from {File}.", mapStream.Name);
                    }
                }

                _landTiles[x][y] = tiles;

                if (++y >= BlockHeight)
                {
                    y = 0;
                    x++;
                }
            } while (x < BlockWidth);
        }
        catch (Exception ex)
        {
            if (Core.Now >= m_NextLandWarning)
            {
                logger.Warning(ex, "Warning: End of stream for map file for {Map} ({X}, {Y})", _map, x, y);
                m_NextLandWarning = Core.Now + TimeSpan.FromSeconds(10);
            }

            _landTiles[x][y] = _invalidLandBlock;
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LandTile
{
    internal short m_ID;
    internal sbyte m_Z;

    public int ID => m_ID;

    public int Z => m_Z;

    public bool Ignored => m_ID is 2 or 0x1DB or >= 0x1AE and <= 0x1B5;

    public LandTile(short id, sbyte z)
    {
        m_ID = id;
        m_Z = z;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticTile
{
    internal ushort m_ID;
    internal byte m_X;
    internal byte m_Y;
    internal sbyte m_Z;
    internal short m_Hue;

    public int ID => m_ID;

    public int X { get => m_X; set => m_X = (byte)value; }

    public int Y { get => m_Y; set => m_Y = (byte)value; }

    public int Z { get => m_Z; set => m_Z = (sbyte)value; }

    public int Hue { get => m_Hue; set => m_Hue = (short)value; }

    public int Height => TileData.ItemTable[m_ID & TileData.MaxItemValue].Height;

    public StaticTile(ushort id, sbyte z)
    {
        m_ID = id;
        m_Z = z;

        m_X = 0;
        m_Y = 0;
        m_Hue = 0;
    }

    public StaticTile(ushort id, byte x, byte y, sbyte z, short hue)
    {
        m_ID = id;
        m_X = x;
        m_Y = y;
        m_Z = z;
        m_Hue = hue;
    }

    public void Set(ushort id, sbyte z)
    {
        m_ID = id;
        m_Z = z;
    }

    public void Set(ushort id, byte x, byte y, sbyte z, short hue)
    {
        m_ID = id;
        m_X = x;
        m_Y = y;
        m_Z = z;
        m_Hue = hue;
    }
}

public class UOPIndex
{
    private class UOPEntry : IComparable<UOPEntry>
    {
        public int m_Offset;
        public readonly int m_Length;
        public int m_Order;

        public UOPEntry(int offset, int length)
        {
            m_Offset = offset;
            m_Length = length;
            m_Order = 0;
        }

        public int CompareTo(UOPEntry other) => m_Order.CompareTo(other.m_Order);
    }

    private class OffsetComparer : IComparer<UOPEntry>
    {
        public static readonly IComparer<UOPEntry> Instance = new OffsetComparer();

        public int Compare(UOPEntry x, UOPEntry y) => x!.m_Offset.CompareTo(y!.m_Offset);
    }

    private readonly BinaryReader m_Reader;
    private readonly int m_Length;
    private readonly UOPEntry[] m_Entries;

    public int Version { get; }

    public UOPIndex(FileStream stream)
    {
        m_Reader = new BinaryReader(stream);
        m_Length = (int)stream.Length;

        if (m_Reader.ReadInt32() != 0x50594D)
        {
            throw new ArgumentException("Invalid UOP file.");
        }

        Version = m_Reader.ReadInt32();
        m_Reader.ReadInt32();
        var nextTable = m_Reader.ReadInt32();

        var entries = new List<UOPEntry>();

        do
        {
            stream.Seek(nextTable, SeekOrigin.Begin);
            var count = m_Reader.ReadInt32();
            nextTable = m_Reader.ReadInt32();
            m_Reader.ReadInt32();

            for (var i = 0; i < count; ++i)
            {
                var offset = m_Reader.ReadInt32();

                if (offset == 0)
                {
                    stream.Seek(30, SeekOrigin.Current);
                    continue;
                }

                m_Reader.ReadInt64();
                var length = m_Reader.ReadInt32();

                entries.Add(new UOPEntry(offset, length));

                stream.Seek(18, SeekOrigin.Current);
            }
        }
        while (nextTable != 0 && nextTable < m_Length);

        entries.Sort(OffsetComparer.Instance);

        for (var i = 0; i < entries.Count; ++i)
        {
            stream.Seek(entries[i].m_Offset + 2, SeekOrigin.Begin);

            int dataOffset = m_Reader.ReadInt16();
            entries[i].m_Offset += 4 + dataOffset;

            stream.Seek(dataOffset, SeekOrigin.Current);
            entries[i].m_Order = m_Reader.ReadInt32();
        }

        entries.Sort();
        m_Entries = entries.ToArray();
    }

    public int Lookup(int offset)
    {
        var total = 0;

        for (var i = 0; i < m_Entries.Length; ++i)
        {
            var newTotal = total + m_Entries[i].m_Length;

            if (offset < newTotal)
            {
                return m_Entries[i].m_Offset + (offset - total);
            }

            total = newTotal;
        }

        return m_Length;
    }

    public void Close()
    {
        m_Reader.Close();
    }
}
