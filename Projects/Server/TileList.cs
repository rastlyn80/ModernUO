using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server;

public class TileList
{
    private static readonly StaticTile[] m_EmptyTiles = Array.Empty<StaticTile>();
    private StaticTile[] _tiles;

    public TileList()
    {
        _tiles = new StaticTile[8];
        Count = 0;
    }

    public int Count { get; private set; }

    public void AddRange(StaticTile[] tiles)
    {
        if (Count + tiles.Length > _tiles.Length)
        {
            var old = _tiles;
            _tiles = new StaticTile[(Count + tiles.Length) * 2];

            for (var i = 0; i < old.Length; ++i)
            {
                _tiles[i] = old[i];
            }
        }

        for (var i = 0; i < tiles.Length; ++i)
        {
            _tiles[Count++] = tiles[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Add(StaticTile* ptr)
    {
        Add(Marshal.PtrToStructure<StaticTile>((nint)ptr));
    }

    public void Add(StaticTile tile)
    {
        TryResize();
        _tiles[Count] = tile;
        ++Count;
    }

    public void Add(ushort id, byte x, byte y, sbyte z, short hue = 0)
    {
        TryResize();
        ref var tile = ref _tiles[Count];
        tile.m_ID = id;
        tile.m_X = x;
        tile.m_Y = y;
        tile.m_Z = z;
        tile.m_Hue = hue;
        ++Count;
    }

    public void Add(ushort id, sbyte z)
    {
        TryResize();
        _tiles[Count].m_ID = id;
        _tiles[Count].m_Z = z;
        ++Count;
    }

    private void TryResize()
    {
        if (Count + 1 > _tiles.Length)
        {
            var old = _tiles;
            _tiles = new StaticTile[old.Length * 2];
            Array.Copy(old, _tiles, old.Length);
        }
    }

    public StaticTile[] ToArray()
    {
        if (Count == 0)
        {
            return m_EmptyTiles;
        }

        var tiles = new StaticTile[Count];

        for (var i = 0; i < Count; ++i)
        {
            tiles[i] = _tiles[i];
        }

        Count = 0;

        return tiles;
    }
}
