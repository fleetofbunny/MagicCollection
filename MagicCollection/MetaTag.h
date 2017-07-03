#pragma once
#include <string>
#include "Config.h"

enum MetaTagType : unsigned short
{
	Public = 0x1, // 001
	Ignored = 0x2, // 010
	Visible = 0x3, // 011
	Hidden = 0x4, // 100
	Any = 0xF // 1111
};

class MetaTag
{
public:
	MetaTag(Tag aTag, MetaTagType atagType);
	MetaTag(std::string aszKey, std::string aszVal, MetaTagType atagType);
	~MetaTag();

	std::string GetKey();
	std::string GetVal(MetaTagType atagType = Any);

	bool CanView(MetaTagType atagType);

private:
	MetaTagType m_Type;

	Tag m_Tag;
};
