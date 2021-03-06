#pragma once
#include <vector>
#include <string>
#include <list>
#include <fstream>
#include <sstream>

#include "Config.h"
#include "StringHelper.h"
#include "CopyItem.h"
#include "CollectionItem.h"
#include "Collection.h"
#include "CollectionFactory.h"
#include "CollectionSource.h"
#include "ListHelper.h"

class CollectionFactory;
class Collection;

class CollectionIO
{
public:
   CollectionIO();
   ~CollectionIO();

   bool CollectionIO::GetFileLines( std::string aszFileName,
                                    std::vector<std::string>& rlstFileLines );
   bool GetNameAndCollectionLines(std::vector<std::string> alstAllLines,
      std::string& rszName, std::vector<std::string>& rlstCardLines);

   bool CaptureUnlistedItems(Location aAddrColID,
      CollectionSource* aptCollectionSource,
      std::map<int, std::list<CopyItem*>>& rlstAdditionalItems,
      std::map<int, std::list<CopyItem*>>& rlstAlreadyCapturedItems);

   bool ConsolodateLocalItems(Location aAddrColID,
      CollectionSource* aptCollectionSource,
      std::map<int, std::list<CopyItem*>>& rlstPotentialDuplicates,
      std::map<int, std::list<CopyItem*>>& rlstNonDuplicates);

   bool RejoinAsyncedLocalItems(Location aAddrColID,
      CollectionSource* aptCollectionSource,
      unsigned long aulNewItemTS,
      std::map<int, std::list<CopyItem*>>& rlstPotentialDuplicates,
      std::map<int, std::list<CopyItem*>>& rlstNonDuplicates);

   bool ConsolodateBorrowedItems(Location aAddrColID,
      CollectionSource* aptCollectionSource,
      CollectionFactory* aptCollFactory);

   bool RegisterRemainingInList( std::vector<int>& alstRegistry, 
                                 std::map<int, std::list<CopyItem*>>& amapNewItems );

   bool ReleaseUnfoundReferences(Location aAddrColID,
      CollectionSource* aptCollectionSource);

   bool CollectionFileExists(std::string aszFileName);
   std::string GetCollectionFile(std::string aszCollectionName);
   std::string GetMetaFile(std::string aszCollectionName);
   std::string GetHistoryFile(std::string aszCollectionName);
   std::string GetOverheadFile(std::string aszCollectionName);
};

