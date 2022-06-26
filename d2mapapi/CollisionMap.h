#pragma once

#include <map>
#include <string>
#include <json_dto/pub.hpp>
#include "D2Structs.h"

struct Point
{
	int x{};
	int y{};

	template < typename JSON_IO >
	void
		json_io( JSON_IO& io )
	{
		io
			& json_dto::mandatory( "x", x )
			& json_dto::mandatory( "y", y );
	}
};

struct Exit {
	std::vector<Point> offsets;
	bool isPortal = false;

	template < typename JSON_IO >
	void
		json_io(JSON_IO& io)
	{
		io
			& json_dto::mandatory("offsets", offsets)
			& json_dto::mandatory("isPortal", isPortal);
	}
};

struct AdjacentLevel
{
	std::vector<Point> exits{};
	Point levelOrigin{};
	int width{};
	int height{};

	template < typename JSON_IO >
	void
		json_io( JSON_IO& io )
	{
		io
			& json_dto::mandatory( "exits", exits )
			& json_dto::mandatory( "origin", levelOrigin )
			& json_dto::mandatory( "width", width )
			& json_dto::mandatory( "height", height );
	}
};

struct Rect {
	int x0{};
	int y0{};
	int x1{};
	int y1{};
	template < typename JSON_IO >
	void
		json_io(JSON_IO& io)
	{
		io
			& json_dto::mandatory("x0", x0)
			& json_dto::mandatory("y0", y0)
			& json_dto::mandatory("x1", x1)
			& json_dto::mandatory("y1", y1);
	}
};

class CCollisionMap
{
private:	
	unsigned int areaid;
	Act* pAct;
public:
	CCollisionMap(Act* pAct, unsigned int areaid);
	void build();

	Point m_levelOrigin; // level top-left
	std::vector<int16_t> mapData;
	Rect crop = { -1, -1, -1, -1 };
	std::map<std::string, Exit> exits;
	std::map<std::string, std::vector<Point>> m_npcs;
	std::map<std::string, std::vector<Point>> m_objects;

};
