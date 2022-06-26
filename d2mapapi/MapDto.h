#pragma once
#include "CollisionMap.h"

struct MapDto
{
	Rect crop;
	Point levelOrigin; // level top-left
	std::vector<int16_t> mapData;
	std::map<std::string, Exit> exits;
	std::map<std::string, std::vector<Point>> npcs;
	std::map<std::string, std::vector<Point>> objects;

	template<typename Json_Io>
	void json_io( Json_Io& io ) {
		io	& json_dto::mandatory( "offset", levelOrigin )
			& json_dto::mandatory("crop", crop)
			& json_dto::mandatory( "exits", exits)
			& json_dto::mandatory( "mapData", mapData)
			& json_dto::mandatory( "npcs", npcs )
			& json_dto::mandatory( "objects", objects );
	}
};

namespace map
{
	MapDto MapToDto( CCollisionMap& map );
}

