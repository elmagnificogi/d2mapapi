#include "CollisionMap.h"
#include <iostream>
#include <vector>
#include <map>
#include "D2Map.h"
#include "D2Ptrs.h"
#include <algorithm>

constexpr int unit_type_npc = 1;
constexpr int unit_type_object = 2;
constexpr int unit_type_tile = 5;

CCollisionMap::CCollisionMap(Act* pAct, unsigned int areaid)
{
	this->pAct = pAct;
	this->areaid = areaid;
}

void CCollisionMap::build()
{

	Level* pLevel = GetLevel(pAct->pMisc, areaid);
    unsigned int currLevelNo = pLevel->dwLevelNo;
    Act* act = pAct;

	if (pLevel)
	{
		if (!pLevel->pRoom2First)
		{
			D2COMMON_InitLevel(pLevel);
		}

		if (pLevel->pRoom2First)
		{
			m_levelOrigin.x = pLevel->dwPosX * 5;
			m_levelOrigin.y = pLevel->dwPosY * 5;

			const int width = pLevel->dwSizeX * 5;
			const int height = pLevel->dwSizeY * 5;
            auto map = std::vector<int16_t>(width * height, -1);

            std::vector<std::tuple<uint32_t, int, int, int>> sides;
			for (Room2* pRoom2 = pLevel->pRoom2First; pRoom2; pRoom2 = pRoom2->pRoom2Next)
			{
				BOOL bAdded = FALSE;

				if (!pRoom2->pRoom1)
				{
					bAdded = TRUE;
					D2COMMON_AddRoomData( pAct, pLevel->dwLevelNo, pRoom2->dwPosX, pRoom2->dwPosY, NULL );
				}

                auto roomPosX = pRoom2->dwPosX;
                auto roomPosY = pRoom2->dwPosY;
				// levels near
				for (DWORD i = 0; i < pRoom2->dwRoomsNear; i++)
				{
                    int side = -1;
                    auto* pRoom2Near = pRoom2->pRoom2Near[i];
                    auto nearLevelNo = pRoom2Near->pLevel->dwLevelNo;
                    if (currLevelNo == nearLevelNo) { continue; }
                    auto nearPosX = pRoom2Near->dwPosX;
                    auto nearPosY = pRoom2Near->dwPosY;
                    auto nearSizeX = pRoom2Near->dwSizeX;
                    auto nearSizeY = pRoom2Near->dwSizeY;
                    auto roomSizeX = pRoom2->dwSizeX;
                    auto roomSizeY = pRoom2->dwSizeY;
                    if (nearPosX + nearSizeX == roomPosX && nearPosY == roomPosY) {
                        side = 0;
                    }
                    else if (nearPosX == roomPosX + roomSizeX && nearPosY == roomPosY) {
                        side = 1;
                    }
                    else if (nearPosY + nearSizeY == roomPosY && nearPosX == roomPosX) {
                        side = 2;
                    }
                    else if (nearPosY == roomPosY + roomSizeY && nearPosX == roomPosX) {
                        side = 3;
                    }
                    if (side < 0) { continue; }
                    bool bAddedNear = false;
                    if (!pRoom2Near->pRoom1) {
                        D2COMMON_AddRoomData(act, nearLevelNo, nearPosX, nearPosY, nullptr);
                        bAddedNear = true;
                    }
                    int sideStart = -1;
                    Room1* room1;
                    CollMap* coll;
                    if ((room1 = pRoom2Near->pRoom1) && (coll = room1->Coll)) {
                        uint16_t* p = coll->pMapStart;
                        auto w = coll->dwSizeGameX, h = coll->dwSizeGameY;
                        switch (side) {
                        case 0:
                            p += w - 1;
                            for (int z = 0; z < h; z++) {
                                if ((*p & 1) || (*(p - 1) & 1)) {
                                    if (sideStart >= 0) {
                                        sides.emplace_back(nearLevelNo, side, coll->dwPosGameY + sideStart, coll->dwPosGameY + z);
                                        sideStart = -1;
                                    }
                                }
                                else {
                                    if (sideStart < 0) {
                                        sideStart = z;
                                    }
                                }
                                p += w;
                            }
                            break;
                        case 1:
                            for (int z = 0; z < h; z++) {
                                if ((*p & 1) || (*(p + 1) & 1)) {
                                    if (sideStart >= 0) {
                                        sides.emplace_back(nearLevelNo, side, coll->dwPosGameY + sideStart, coll->dwPosGameY + z);
                                        sideStart = -1;
                                    }
                                }
                                else {
                                    if (sideStart < 0) {
                                        sideStart = z;
                                    }
                                }
                                p += w;
                            }
                            break;
                        case 2:
                            p += w * (h - 1);
                            for (int z = 0; z < w; z++) {
                                if ((*p & 1) || (*(p - w) & 1)) {
                                    if (sideStart >= 0) {
                                        sides.emplace_back(nearLevelNo, side, coll->dwPosGameX + sideStart, coll->dwPosGameX + z);
                                        sideStart = -1;
                                    }
                                }
                                else {
                                    if (sideStart < 0) {
                                        sideStart = z;
                                    }
                                }
                                p++;
                            }
                            break;
                        case 3:
                            for (int z = 0; z < w; z++) {
                                if ((*p & 1) || (*(p + w) & 1)) {
                                    if (sideStart >= 0) {
                                        sides.emplace_back(nearLevelNo, side, coll->dwPosGameX + sideStart, coll->dwPosGameX + z);
                                        sideStart = -1;
                                    }
                                }
                                else {
                                    if (sideStart < 0) {
                                        sideStart = z;
                                    }
                                }
                                p++;
                            }
                            break;
                        default: break;
                        }
                        if (sideStart >= 0) {
                            if (side == 2 || side == 3) {
                                sides.emplace_back(nearLevelNo, side, coll->dwPosGameX + sideStart, coll->dwPosGameX + w);
                            }
                            else {
                                sides.emplace_back(nearLevelNo, side, coll->dwPosGameY + sideStart, coll->dwPosGameY + h);
                            }
                        }
                    }
                    if (bAddedNear) {
                        D2COMMON_RemoveRoomData(act, nearLevelNo, nearPosX, nearPosY, nullptr);
                    }
				}

                
                CollMap* coll;
				// add collision data
				if (pRoom2->pRoom1 && pRoom2->pRoom1->Coll)
				{
					const int x = pRoom2->pRoom1->Coll->dwPosGameX - m_levelOrigin.x;
					const int y = pRoom2->pRoom1->Coll->dwPosGameY - m_levelOrigin.y;
					const int cx = pRoom2->pRoom1->Coll->dwSizeGameX;
					const int cy = pRoom2->pRoom1->Coll->dwSizeGameY;
					const int nLimitX = x + cx;
					const int nLimitY = y + cy;

                    //uint16_t* p = coll->pMapStart;
                    if (crop.x0 < 0 || x < crop.x0) crop.x0 = x;
                    if (crop.y0 < 0 || y < crop.y0) crop.y0 = y;
                    if (crop.x1 < 0 || nLimitX > crop.x1) crop.x1 = nLimitX;
                    if (crop.y1 < 0 || nLimitY > crop.y1) crop.y1 = nLimitY;
					WORD* p = pRoom2->pRoom1->Coll->pMapStart;
					for (int j = y; j < nLimitY; j++)
					{
                        int index = j * width + x;
						for (int i = x; i < nLimitX; i++)
						{
                            map[index++] = *p++;
						}
					}
				}

				// add unit data
				for (PresetUnit* pPresetUnit = pRoom2->pPreset; pPresetUnit; pPresetUnit = pPresetUnit->pPresetNext)
				{
					// npcs
					if (pPresetUnit->dwType == unit_type_npc)
					{
						const auto npcX = static_cast<int>(pRoom2->dwPosX * 5 + pPresetUnit->dwPosX);
						const auto npcY = static_cast<int>(pRoom2->dwPosY * 5 + pPresetUnit->dwPosY);
						auto fileNumber = std::to_string( pPresetUnit->dwTxtFileNo );
						m_npcs[fileNumber].push_back( Point{npcX, npcY});
					}

					// objects
					if (pPresetUnit->dwType == unit_type_object)
					{
						const auto objectX = static_cast<int>(pRoom2->dwPosX * 5 + pPresetUnit->dwPosX);
						const auto objectY = static_cast<int>(pRoom2->dwPosY * 5 + pPresetUnit->dwPosY);
						auto fileNumber = std::to_string( pPresetUnit->dwTxtFileNo );
						m_objects[fileNumber].push_back( Point{ objectX, objectY } );
					}

					// level exits
					if (pPresetUnit->dwType == unit_type_tile)
					{
						for (RoomTile* pRoomTile = pRoom2->pRoomTiles; pRoomTile; pRoomTile = pRoomTile->pNext)
						{
							if (*pRoomTile->nNum == pPresetUnit->dwTxtFileNo)
							{
								const auto exitX = static_cast<int>(pRoom2->dwPosX * 5 + pPresetUnit->dwPosX);
								const auto exitY = static_cast<int>(pRoom2->dwPosY * 5 + pPresetUnit->dwPosY);

								auto levelNumber = std::to_string( pRoomTile->pRoom2->pLevel->dwLevelNo );
                                exits[levelNumber].offsets.push_back(Point{ exitX , exitY });
                                exits[levelNumber].isPortal = true;
							}
						}
					}
				}

				if (bAdded)
				{
					D2COMMON_RemoveRoomData( pAct, pLevel->dwLevelNo, pRoom2->dwPosX, pRoom2->dwPosY, NULL );
				}
			}

            Point offset;
            offset.x = pLevel->dwPosX * 5;
            offset.y = pLevel->dwPosY * 5;

            

            std::sort(sides.begin(), sides.end());
            std::vector<std::tuple<uint32_t, int, int, int>> realSides;
            uint32_t lastNearLevelNo = 0;
            int lastSide = -1, start = -1, end = -1;
            for (auto [nearLevelNo, side, sideStart, sideEnd] : sides) {
                if (lastNearLevelNo != nearLevelNo || lastSide != side) {
                    if (start >= 0) {
                        realSides.emplace_back(lastNearLevelNo, lastSide, start, end);
                    }
                    lastSide = side;
                    lastNearLevelNo = nearLevelNo;
                    start = -1;
                    end = -1;
                }
                if (start == -1) {
                    start = sideStart;
                    end = sideEnd;
                }
                else if (sideStart == end) {
                    end = sideEnd;
                }
                else {
                    realSides.emplace_back(lastNearLevelNo, lastSide, start, end);
                    start = sideStart;
                    end = sideEnd;
                }
            }
            if (start >= 0) {
                realSides.emplace_back(lastNearLevelNo, lastSide, start, end);
            }
            sides.clear();
            for (auto [nearLevelNo, side, sideStart, sideEnd] : realSides) {
                int sStart = -1;
                switch (side) {
                case 0: {
                    sideStart -= offset.y;
                    if (sideStart < crop.y0) sideStart = crop.y0;
                    sideEnd -= offset.y;
                    if (sideEnd > crop.y1) sideEnd = crop.y1;
                    if (sideStart == sideEnd) break;
                    auto w = crop.x1 - crop.x0;
                    auto* p = map.data() + (sideStart - crop.y0) * w;
                    for (auto z = sideStart; z < sideEnd; ++z) {
                        if ((*p & 1) || (*(p + 1) & 1)) {
                            if (sStart >= 0) {
                                sides.emplace_back(nearLevelNo, side, sStart, z);
                                sStart = -1;
                            }
                        }
                        else {
                            if (sStart < 0) {
                                sStart = z;
                            }
                        }
                        p += w;
                    }
                    break;
                }
                case 1: {
                    sideStart -= offset.y;
                    if (sideStart < crop.y0) sideStart = crop.y0;
                    sideEnd -= offset.y;
                    if (sideEnd > crop.y1) sideEnd = crop.y1;
                    if (sideStart == sideEnd) break;
                    auto w = crop.x1 - crop.x0;
                    auto* p = map.data() + (sideStart - crop.y0) * w + (w - 1);
                    for (auto z = sideStart; z < sideEnd; ++z) {
                        if ((*p & 1) || (*(p - 1) & 1)) {
                            if (sStart >= 0) {
                                sides.emplace_back(nearLevelNo, side, sStart, z);
                                sStart = -1;
                            }
                        }
                        else {
                            if (sStart < 0) {
                                sStart = z;
                            }
                        }
                        p += w;
                    }
                    break;
                }
                case 2: {
                    sideStart -= offset.x;
                    if (sideStart < crop.x0) sideStart = crop.x0;
                    sideEnd -= offset.x;
                    if (sideEnd > crop.x1) sideEnd = crop.x1;
                    if (sideStart == sideEnd) break;
                    auto w = crop.x1 - crop.x0;
                    auto* p = map.data() + (sideStart - crop.x0);
                    for (auto z = sideStart; z < sideEnd; ++z) {
                        if ((*p & 1) || (*(p + w) & 1)) {
                            if (sStart >= 0) {
                                sides.emplace_back(nearLevelNo, side, sStart, z);
                                sStart = -1;
                            }
                        }
                        else {
                            if (sStart < 0) {
                                sStart = z;
                            }
                        }
                        p++;
                    }
                    break;
                }
                case 3: {
                    sideStart -= offset.x;
                    if (sideStart < crop.x0) sideStart = crop.x0;
                    sideEnd -= offset.x;
                    if (sideEnd > crop.x1) sideEnd = crop.x1;
                    if (sideStart == sideEnd) break;
                    auto w = crop.x1 - crop.x0;
                    auto* p = map.data() + w * (crop.y1 - crop.y0 - 1) + (sideStart - crop.x0);
                    for (auto z = sideStart; z < sideEnd; ++z) {
                        if ((*p & 1) || (*(p - w) & 1)) {
                            if (sStart >= 0) {
                                sides.emplace_back(nearLevelNo, side, sStart, z);
                                sStart = -1;
                            }
                        }
                        else {
                            if (sStart < 0) {
                                sStart = z;
                            }
                        }
                        p++;
                    }
                    break;
                }
                default:
                    break;
                }
                if (sStart >= 0) {
                    sides.emplace_back(nearLevelNo, side, sStart, sideEnd);
                }
            }

            for (auto [nearLevelNo, side, sideStart, sideEnd] : sides) {
                if (sideStart + 2 >= sideEnd) { continue; }
                auto levelNumber = std::to_string(nearLevelNo);
                switch (side) {
                case 0:
                    exits[levelNumber].offsets.push_back(Point{ offset.x + crop.x0, offset.y + (sideStart + sideEnd) / 2 });
                    break;
                case 1:
                    exits[levelNumber].offsets.push_back(Point{ offset.x + crop.x1 - 1, offset.y + (sideStart + sideEnd) / 2 });
                    break;
                case 2:
                    exits[levelNumber].offsets.push_back(Point{ offset.x + (sideStart + sideEnd) / 2, offset.y + crop.y0 });
                    break;
                case 3:
                    exits[levelNumber].offsets.push_back(Point{ offset.x + (sideStart + sideEnd) / 2, offset.y + crop.y1 - 1 });
                    break;
                default:
                    break;
                }
            }

            ///std::vector<int16_t> mapData;
            /* run length encoding map data */
            mapData.clear();
            for (int j = crop.y0; j < crop.y1; ++j) {
                int index = j * width + crop.x0;
                bool lastIsWalkable = false;
                int count = 0;
                for (int i = crop.x0; i < crop.x1; ++i) {
                    bool walkable = !(map[index++] & 1);
                    if (walkable == lastIsWalkable) {
                        ++count;
                        continue;
                    }
                    mapData.emplace_back(count);
                    count = 1;
                    lastIsWalkable = walkable;
                }
                mapData.emplace_back(count);
                mapData.emplace_back(-1);
            }
		}
	}
}