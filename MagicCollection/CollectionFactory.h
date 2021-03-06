#pragma once
#include <vector>
#include <string>
#include <memory>
#include <unordered_set>

#include "Collection.h"
#include "StringHelper.h"
#include "CollectionSource.h"
#include "TryGet.h"

class CollectionFactory
{
public:
   CollectionFactory(CollectionSource* aoColSource);
   ~CollectionFactory();

   bool CollectionExists(std::string aszCollectionName);
   bool CollectionExists(const Location& aAddrColID);
   TryGet<Collection> GetCollection(std::string aszCollectionName) const;
   TryGet<Collection> GetCollection(const Location& aAddrColID) const;

   void SaveCollection(std::string aszCollectionName) const;
   std::string LoadCollectionFromFile(std::string aszColFile);
   std::string CreateNewCollection(std::string aszColName, std::string aszParent = "");

   std::vector<std::string> GetLoadedCollections();

private:
   std::unordered_set<std::string> m_setInLoading;
   std::vector<std::shared_ptr<Collection>> m_lstCollections;
   CollectionSource* m_ColSource;

   bool processAction(const std::string& aszAction, bool abPreload);
   bool performAction(const std::string& aszActionCmd, const std::string& aszParms);
   std::string getNextChildName(std::string aszParentID) const;
};

