#pragma once
#include <string>
#include "Addresser.h"

class CollectionDetails
{
public:
   CollectionDetails();
   ~CollectionDetails();

   void SetName(std::string aszName);
   std::string GetName();

   void SetFileName( std::string aszFileName, 
                     bool abDefaultLocation = false );
   std::string GetFileName();

   void SetFile(std::string aszFile);
   std::string GetFile();

   void SetTimeStamp(unsigned long aulTimeStamp = 0);
   unsigned long GetTimeStamp();

   void SetChildrenCount(unsigned int auiChildCount);
   unsigned int GetChildrenCount();
   void IncrementChildCount();

   void SetAddress(const Location& aAddress);
   void AssignAddress(std::string aszStringAddress);
   Location* GetAddress();

private:
   std::string m_szName;
   std::string m_szFile;
   std::string m_szFileName;
   unsigned long m_ulTimeStamp;
   unsigned int m_iChildrenCount;
   // The address of a collection is always a location. ie. only has 1 subaddr.
   Location* m_ptrAddress;
};

