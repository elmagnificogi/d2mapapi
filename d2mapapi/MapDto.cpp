#include "MapDto.h"

MapDto map::MapToDto(CCollisionMap& map)
{
	auto mapDto = MapDto{};
	mapDto.crop = map.crop;
	mapDto.exits = map.exits;
	mapDto.mapData = map.mapData;
	mapDto.levelOrigin = map.m_levelOrigin;
	mapDto.npcs = map.m_npcs;
	mapDto.objects = map.m_objects;

	return mapDto;
}
