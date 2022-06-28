using D2Map.Core.Models;
using D2Map.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace D2Map.Core.Helpers
{
    public static class MapHelpers
    {
        static public uint[] ActLevels = { 1, 40, 75, 103, 109, 137 };

        static private uint unit_type_npc = 1;
        static private uint unit_type_object = 2;
        static private uint unit_type_tile = 5;

        public static Models.Act GetAct(Area area)
        {
            for (uint i = 1; i < 5; ++i)
            {
                if ((int)area < ActLevels[i])
                {
                    return (Models.Act)(i - 1);
                }
            }
            return Models.Act.Act5;
        }

        public unsafe static CollisionMap BuildCollissionMap(Wrapper.Act* act, Area area)
        {
            var collisionMap = new CollisionMap();
            if (act->pActMisc->RealTombArea != 0)
            {
                collisionMap.TombArea = (Area)act->pActMisc->RealTombArea;
            }

            Level* pLevel = MapDll.GetLevel(act->pActMisc, (uint)area);

            if (pLevel == null) { return null; }

            uint currLevelNo;
            uint currPosX;
            uint currPosY;
            uint width, height;
            if (pLevel->pRoom2First == null)
            {
                MapDll.InitLevel(pLevel);
            }
            collisionMap.built = true;
            if (pLevel->pRoom2First == null) { return null; }
            currLevelNo = pLevel->dwLevelNo;
            currPosX = pLevel->dwPosX;
            currPosY = pLevel->dwPosY;
            width = pLevel->dwSizeX * 5;
            height = pLevel->dwSizeY * 5;
            collisionMap.offset.X = currPosX * 5;
            collisionMap.offset.Y = currPosY * 5;

            var map = new int[width * height];
            collisionMap.size.width = width;
            collisionMap.size.height = height;

            foreach (int i in map)
            {
                map[i] = -1;
            }

            Room1* room1;
            CollMap* coll;
            List<(uint, int, int, int)> sides = new List<(uint, int, int, int)>();
            for (Room2* pRoom2 = pLevel->pRoom2First; pRoom2 != null; pRoom2 = pRoom2->pRoom2Next)
            {
                bool bAdded = false;
                var roomPosX = pRoom2->dwPosX;
                var roomPosY = pRoom2->dwPosY;

                if (pRoom2->pRoom1 == null)
                {
                    bAdded = true;
                    MapDll.AddRoomData(act, pLevel->dwLevelNo, roomPosX, roomPosY, null);
                }

                /* Check near levels' walkable rect (we check 2 pixels from edge)
                 * side: 0-left 1-right 2-top 3-bottom
                 */
                for (uint i = 0; i < pRoom2->dwRoomsNear; i++)
                {
                    int side = -1;
                    var pRoom2Near = pRoom2->pRoom2Near[i];
                    var nearLevelNo = pRoom2Near->pLevel->dwLevelNo;
                    if (currLevelNo == nearLevelNo) { continue; }
                    var nearPosX = pRoom2Near->dwPosX;
                    var nearPosY = pRoom2Near->dwPosY;
                    var nearSizeX = pRoom2Near->dwSizeX;
                    var nearSizeY = pRoom2Near->dwSizeY;
                    var roomSizeX = pRoom2->dwSizeX;
                    var roomSizeY = pRoom2->dwSizeY;
                    if (nearPosX + nearSizeX == roomPosX && nearPosY == roomPosY)
                    {
                        side = 0;
                    }
                    else if (nearPosX == roomPosX + roomSizeX && nearPosY == roomPosY)
                    {
                        side = 1;
                    }
                    else if (nearPosY + nearSizeY == roomPosY && nearPosX == roomPosX)
                    {
                        side = 2;
                    }
                    else if (nearPosY == roomPosY + roomSizeY && nearPosX == roomPosX)
                    {
                        side = 3;
                    }
                    if (side < 0) { continue; }
                    bool bAddedNear = false;
                    if (pRoom2Near->pRoom1 == null)
                    {
                        MapDll.AddRoomData(act, nearLevelNo, nearPosX, nearPosY, null);
                        bAddedNear = true;
                    }
                    int sideStart = -1;
                    room1 = pRoom2Near->pRoom1;
                    coll = room1->Coll;
                    if ((room1 != null) && (coll != null))
                    {
                        ushort* p = coll->pMapStart;
                        var w = coll->dwSizeGameX;
                        var h = coll->dwSizeGameY;
                        switch (side)
                        {
                            case 0:
                                p += w - 1;
                                for (int z = 0; z < h; z++)
                                {
                                    if ((*p & 1) == 1 || (*(p - 1) & 1) == 1)
                                    {
                                        if (sideStart >= 0)
                                        {
                                            sides.Append((nearLevelNo, side, (int)coll->dwPosGameY + sideStart, (int)coll->dwPosGameY + z));
                                            sideStart = -1;
                                        }
                                    }
                                    else
                                    {
                                        if (sideStart < 0)
                                        {
                                            sideStart = z;
                                        }
                                    }
                                    p += w;
                                }
                                break;
                            case 1:
                                for (int z = 0; z < h; z++)
                                {
                                    if ((*p & 1) == 1 || (*(p + 1) & 1) == 1)
                                    {
                                        if (sideStart >= 0)
                                        {
                                            sides.Append((nearLevelNo, side, (int)coll->dwPosGameY + sideStart, (int)coll->dwPosGameY + z));
                                            sideStart = -1;
                                        }
                                    }
                                    else
                                    {
                                        if (sideStart < 0)
                                        {
                                            sideStart = z;
                                        }
                                    }
                                    p += w;
                                }
                                break;
                            case 2:
                                p += w * (h - 1);
                                for (int z = 0; z < w; z++)
                                {
                                    if ((*p & 1) == 1 || (*(p - w) & 1) == 1)
                                    {
                                        if (sideStart >= 0)
                                        {
                                            sides.Append((nearLevelNo, side, (int)coll->dwPosGameX + sideStart, (int)coll->dwPosGameX + z));
                                            sideStart = -1;
                                        }
                                    }
                                    else
                                    {
                                        if (sideStart < 0)
                                        {
                                            sideStart = z;
                                        }
                                    }
                                    p++;
                                }
                                break;
                            case 3:
                                for (int z = 0; z < w; z++)
                                {
                                    if ((*p & 1) == 1 || (*(p + w) & 1) == 1)
                                    {
                                        if (sideStart >= 0)
                                        {
                                            sides.Append((nearLevelNo, side, (int)coll->dwPosGameX + sideStart, (int)coll->dwPosGameX + z));
                                            sideStart = -1;
                                        }
                                    }
                                    else
                                    {
                                        if (sideStart < 0)
                                        {
                                            sideStart = z;
                                        }
                                    }
                                    p++;
                                }
                                break;
                            default: break;
                        }
                        if (sideStart >= 0)
                        {
                            if (side == 2 || side == 3)
                            {
                                sides.Append((nearLevelNo, side, (int)coll->dwPosGameX + sideStart, (int)coll->dwPosGameX + (int)w));
                            }
                            else
                            {
                                sides.Append((nearLevelNo, side, (int)coll->dwPosGameY + sideStart, (int)coll->dwPosGameY + (int)h));
                            }
                        }
                    }
                    if (bAddedNear)
                    {
                        MapDll.RemoveRoomData(act, nearLevelNo, nearPosX, nearPosY, null);
                    }
                }

                // add collision data
                room1 = pRoom2->pRoom1;
                coll = room1->Coll;
                if ((room1 != null) && (coll != null))
                {
                    int x = (int)(coll->dwPosGameX - collisionMap.offset.X);
                    int y = (int)(coll->dwPosGameY - collisionMap.offset.Y);
                    int cx = (int)coll->dwSizeGameX;
                    int cy = (int)coll->dwSizeGameY;
                    int nLimitX = x + cx;
                    int nLimitY = y + cy;

                    ushort* p = coll->pMapStart;
                    if (collisionMap.crop.x0 < 0 || x < collisionMap.crop.x0) collisionMap.crop.x0 = x;
                    if (collisionMap.crop.y0 < 0 || y < collisionMap.crop.y0) collisionMap.crop.y0 = y;
                    if (collisionMap.crop.x1 < 0 || nLimitX > collisionMap.crop.x1) collisionMap.crop.x1 = nLimitX;
                    if (collisionMap.crop.y1 < 0 || nLimitY > collisionMap.crop.y1) collisionMap.crop.y1 = nLimitY;
                    for (int j = y; j < nLimitY; j++)
                    {
                        int index = (int)(j * width + x);
                        for (int i = x; i < nLimitX; i++)
                        {
                            map[index++] = *p++;
                        }
                    }
                }

                // add unit data
                for (PresetUnit* pPresetUnit = pRoom2->pPreset; pPresetUnit != null; pPresetUnit = pPresetUnit->pPresetNext)
                {
                    // npcs
                    var type = pPresetUnit->dwType;
                    if (type == unit_type_npc)
                    {
                        var npcX = (int)(roomPosX * 5 + pPresetUnit->dwPosX);
                        var npcY = (int)(roomPosY * 5 + pPresetUnit->dwPosY);
                        collisionMap.Npcs[pPresetUnit->dwTxtFileNo.ToString()].Append(new Point((uint)npcX, (uint)npcY));
                    }

                    // objects
                    if (type == unit_type_object) {
                        var objectX = (int)(roomPosX * 5 + pPresetUnit->dwPosX);
                        var objectY = (int)(roomPosY * 5 + pPresetUnit->dwPosY);
                        collisionMap.Objects[pPresetUnit->dwTxtFileNo.ToString()].Append(new Point((uint)objectX, (uint)objectY));
                    }

                    // level exits
                    if (type == unit_type_tile) {
                        var txtFileNo = pPresetUnit->dwTxtFileNo;
                        var presetPosX = pPresetUnit->dwPosX;
                        var presetPosY = pPresetUnit->dwPosY;
                        for (RoomTile* pRoomTile = pRoom2->pRoomTiles; pRoomTile != null; pRoomTile = pRoomTile->pNext) {
                            if (*pRoomTile->nNum == txtFileNo) {
                                var exitX = (int)(roomPosX * 5 + presetPosX);
                                var exitY = (int)(roomPosY * 5 + presetPosY);

                                collisionMap.exits[pRoomTile->pRoom2->pLevel->dwLevelNo.ToString()].isPortal = true;
                                collisionMap.exits[pRoomTile->pRoom2->pLevel->dwLevelNo.ToString()].offsets.Append(new Point((uint)exitX, (uint)exitY));
                            }
                        }
                    }
                }

                if (bAdded)
                {
                    MapDll.RemoveRoomData(act, currLevelNo, roomPosX, roomPosY, null);
                }
            }

            // lastNearLevelNo small to big 
            sides.Sort();
            //std::sort(sides.begin(), sides.end());
            List<(uint, int, int, int)> realSides = new List<(uint, int, int, int)>();
            uint lastNearLevelNo = 0;
            int lastSide = -1, start = -1, end = -1;
            foreach (var (nearLevelNo, side, sideStart, sideEnd) in sides)
            {
                if (lastNearLevelNo != nearLevelNo || lastSide != side)
                {
                    if (start >= 0)
                    {
                        realSides.Append((lastNearLevelNo, lastSide, start, end));
                    }
                    lastSide = side;
                    lastNearLevelNo = nearLevelNo;
                    start = -1;
                    end = -1;
                }
                if (start == -1)
                {
                    start = sideStart;
                    end = sideEnd;
                }
                else if (sideStart == end)
                {
                    end = sideEnd;
                }
                else
                {
                    realSides.Append((lastNearLevelNo, lastSide, start, end));
                    start = sideStart;
                    end = sideEnd;
                }
            }
            if (start >= 0)
            {
                realSides.Append((lastNearLevelNo, lastSide, start, end));
            }
            sides.Clear();
            foreach (var s in realSides)
            {
                var nearLevelNo = s.Item1;
                var side = s.Item2;
                var sideStart = s.Item3;
                var sideEnd = s.Item4;
                int sStart = -1;
                switch (side)
                {
                    case 0:
                        {
                            sideStart -= (int)(collisionMap.offset.Y);
                            if (sideStart < collisionMap.crop.y0) sideStart = collisionMap.crop.y0;
                            sideEnd -= (int)(collisionMap.offset.Y);
                            if (sideEnd > collisionMap.crop.y1) sideEnd = collisionMap.crop.y1;
                            if (sideStart == sideEnd) break;
                            var w = collisionMap.crop.x1 - collisionMap.crop.x0;
                            var index = (sideStart - collisionMap.crop.y0) * w;
                            for (var z = sideStart; z < sideEnd; ++z)
                            {
                                if (((map[index] & 1) == 1) || ((map[index + 1] & 1) == 1))
                                {
                                    if (sStart >= 0)
                                    {
                                        sides.Append((nearLevelNo, side, sStart, z));
                                        sStart = -1;
                                    }
                                }
                                else
                                {
                                    if (sStart < 0)
                                    {
                                        sStart = z;
                                    }
                                }
                                index += w;
                            }
                            break;
                        }
                    case 1:
                        {
                            sideStart -= (int)(collisionMap.offset.Y);
                            if (sideStart < collisionMap.crop.y0) sideStart = collisionMap.crop.y0;
                            sideEnd -= (int)(collisionMap.offset.Y);
                            if (sideEnd > collisionMap.crop.y1) sideEnd = collisionMap.crop.y1;
                            if (sideStart == sideEnd) break;
                            var w = collisionMap.crop.x1 - collisionMap.crop.x0;
                            var index = (sideStart - collisionMap.crop.y0) * w + w - 1;
                            for (var z = sideStart; z < sideEnd; ++z)
                            {
                                if (((map[index] & 1) == 1) || ((map[index - 1] & 1) == 1))
                                {
                                    if (sStart >= 0)
                                    {
                                        sides.Append((nearLevelNo, side, sStart, z));
                                        sStart = -1;
                                    }
                                }
                                else
                                {
                                    if (sStart < 0)
                                    {
                                        sStart = z;
                                    }
                                }
                                index += w;
                            }
                            break;
                        }
                    case 2:
                        {
                            sideStart -= (int)(collisionMap.offset.X);
                            if (sideStart < collisionMap.crop.x0) sideStart = collisionMap.crop.x0;
                            sideEnd -= (int)(collisionMap.offset.X);
                            if (sideEnd > collisionMap.crop.x1) sideEnd = collisionMap.crop.x1;
                            if (sideStart == sideEnd) break;
                            var w = collisionMap.crop.x1 - collisionMap.crop.x0;
                            var index = (sideStart - collisionMap.crop.x0);
                            for (var z = sideStart; z < sideEnd; ++z)
                            {
                                if (((map[index] & 1) == 1) || ((map[index + w] & 1) == 1))
                                {
                                    if (sStart >= 0)
                                    {
                                        sides.Append((nearLevelNo, side, sStart, z));
                                        sStart = -1;
                                    }
                                }
                                else
                                {
                                    if (sStart < 0)
                                    {
                                        sStart = z;
                                    }
                                }
                                index++;
                            }
                            break;
                        }
                    case 3:
                        {
                            sideStart -= (int)(collisionMap.offset.X);
                            if (sideStart < collisionMap.crop.x0) sideStart = collisionMap.crop.x0;
                            sideEnd -= (int)(collisionMap.offset.X);
                            if (sideEnd > collisionMap.crop.x1) sideEnd = collisionMap.crop.x1;
                            if (sideStart == sideEnd) break;
                            var w = collisionMap.crop.x1 - collisionMap.crop.x0;
                            var index = w * (collisionMap.crop.y1 - collisionMap.crop.y0 - 1) + (sideStart - collisionMap.crop.x0);
                            for (var z = sideStart; z < sideEnd; ++z)
                            {
                                if (((map[index] & 1) == 1) || ((map[index - w] & 1) == 1))
                                {
                                    if (sStart >= 0)
                                    {
                                        sides.Append((nearLevelNo, side, sStart, z));
                                        sStart = -1;
                                    }
                                }
                                else
                                {
                                    if (sStart < 0)
                                    {
                                        sStart = z;
                                    }
                                }
                                index++;
                            }
                            break;
                        }
                    default:
                        break;
                }
                if (sStart >= 0)
                {
                    sides.Append((nearLevelNo, side, sStart, sideEnd));
                }
            }

            foreach (var (nearLevelNo, side, sideStart, sideEnd) in sides)
            {
                if (sideStart + 2 >= sideEnd) { continue; }
                switch (side)
                {
                    case 0:
                        collisionMap.exits[nearLevelNo.ToString()].offsets.Append(new Point(collisionMap.offset.X + (uint)collisionMap.crop.x0, (uint)collisionMap.offset.Y + (uint)(sideStart + sideEnd) / 2));
                        break;
                    case 1:
                        collisionMap.exits[nearLevelNo.ToString()].offsets.Append(new Point(collisionMap.offset.X + (uint)collisionMap.crop.x1 - 1, (uint)collisionMap.offset.Y + (uint)(sideStart + sideEnd) / 2));
                        break;
                    case 2:
                        collisionMap.exits[nearLevelNo.ToString()].offsets.Append(new Point(collisionMap.offset.X + (uint)(sideStart + sideEnd) / 2, (uint)collisionMap.offset.Y + (uint)collisionMap.crop.y0));
                        break;
                    case 3:
                        collisionMap.exits[nearLevelNo.ToString()].offsets.Append(new Point(collisionMap.offset.X + (uint)(sideStart + sideEnd) / 2, (uint)collisionMap.offset.Y + (uint)collisionMap.crop.y1 - 1));
                        break;
                    default:
                        break;
                }
            }
        
            /* run length encoding map data */
            collisionMap.mapData = new List<int>();
            for (int j = collisionMap.crop.y0; j < collisionMap.crop.y1; ++j)
            {
                int index = (int)(j * width + collisionMap.crop.x0);
                bool lastIsWalkable = false;
                int count = 0;
                for (int i = collisionMap.crop.x0; i < collisionMap.crop.x1; ++i)
                {
                    bool walkable = !((map[index++] & 1)==1);
                    if (walkable == lastIsWalkable)
                    {
                        ++count;
                        continue;
                    }
                    collisionMap.mapData.Append(count);
                    count = 1;
                    lastIsWalkable = walkable;
                }
                collisionMap.mapData.Append(count);
                collisionMap.mapData.Append(-1);
            }

            return collisionMap;
        }
    }
}
