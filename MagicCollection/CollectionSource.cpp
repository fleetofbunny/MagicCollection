#include "CollectionSource.h"

using namespace rapidxml;

CollectionSource::CollectionSource()
{
   m_bIsLoaded = false;
   resetBuffer();
}

CollectionSource::~CollectionSource()
{
   m_lstoCardCache.clear();
   m_lstCardBuffer.clear();

   m_iAllCharBuffSize = 0;
   delete[] m_AllCharBuff;
}

void CollectionSource::LoadLib(std::string aszFileName)
{
   Config* config = Config::Instance();

   m_lstCardBuffer.clear();
   m_lstCardBuffer.reserve(25000);

   xml_document<> doc;
   std::ifstream file(aszFileName);
   if (!file.good())
   {
      // TODO Close collection.
      return;
   }

   std::stringstream buffer;
   buffer << file.rdbuf();
   file.close();

   // std::string content(buffer.str());
   std::string* textContent = new std::string( buffer.str() );
   doc.parse<0>(&textContent->front());

   xml_node<> *xmlNode_CardDatabase = doc.first_node();

   // With the xml example above this is the <document/> node
   xml_node<>* xmlNode_Cards = xmlNode_CardDatabase->first_node("cards");
   xml_node<>* xmlNode_Card = xmlNode_Cards->first_node();
   while (xmlNode_Card != 0)
   {
      xml_node<> *xmlNode_CardAttribute = xmlNode_Card->first_node();

      m_lstCardBuffer.push_back(SourceObject(m_iAllCharBuffSize));
      SourceObject* sO = &m_lstCardBuffer.back();

      while (xmlNode_CardAttribute != 0)
      {
         std::string szCardKey = xmlNode_CardAttribute->name();
         char keyCode = config->GetKeyCode(szCardKey);
         if( keyCode != -1 )
         {
            m_iAllCharBuffSize += sO->AddAttribute( szCardKey, xmlNode_CardAttribute->value(),
                                                    m_AllCharBuff, ms_iMaxBufferSize );
         }

         xmlNode_CardAttribute = xmlNode_CardAttribute->next_sibling();
      }

      xmlNode_Card = xmlNode_Card->next_sibling();
   }
   delete textContent;

   finalizeBuffer();
   m_bIsLoaded = true;
}

void CollectionSource::HotSwapLib(std::string aszFileName)
{
   std::vector<std::string> vecHoldCards;
   for (auto card : m_lstoCardCache)
   {
      vecHoldCards.push_back(card.GetName());
   }

   resetBuffer();
   LoadLib(aszFileName);

   for (auto card : vecHoldCards)
   {
      LoadCard(card);
   }
}

int CollectionSource::LoadCard(std::string aszCardName)
{
   Config* config = Config::Instance();
   std::string szCardName = StringHelper::Str_Trim(aszCardName, ' ');

   // Since it is faster to look in the cache, check that first.
   int iCacheLocation = findInCache(szCardName, false);
   if (iCacheLocation == -1)
   {
      // Look in the Source Object Buffer for a matching item.
      // If the card is found in the buffer, create a CollectionItem and cache it.
      int iFound = findInBuffer(szCardName, false);
      if (iFound != -1)
      {
         SourceObject* oSource = &m_lstCardBuffer.at(iFound);
         
         // Get the static Attrs
         std::vector<Tag> lstStaticAttrs = oSource->GetAttributes(m_AllCharBuff);

         // Build the trait items from each of the ID attrs and their
         // allowed values.
         auto lstAttrRestrictions = oSource->GetIDAttrRestrictions(m_AllCharBuff);

         std::vector<TraitItem> lstIdentifyingTraits;
         auto iter_Traits = lstAttrRestrictions.begin();
         for (; iter_Traits != lstAttrRestrictions.end(); ++iter_Traits)
         {
            TraitItem newTrait( iter_Traits->first,
                                iter_Traits->second,
                                config->GetPairedKeysList() );
            lstIdentifyingTraits.push_back(newTrait);
         }

         CollectionItem oCard( aszCardName, lstStaticAttrs,
                               lstIdentifyingTraits );

         // Store the location of the CollectionItem in the cache
         iCacheLocation = m_lstoCardCache.size();
         oSource->SetCacheIndex(iCacheLocation);

         // Cache the CollectionItem
         m_lstoCardCache.push_back(oCard);
      }
   }

   return iCacheLocation;
}

TryGet<CollectionItem>
CollectionSource::GetCardPrototype(int aiCacheIndex)
{
   TryGet<CollectionItem> ColRetVal;
   if (aiCacheIndex < m_lstoCardCache.size())
   {
      ColRetVal.Set(&m_lstoCardCache.at(aiCacheIndex));
   }
   
   return ColRetVal;
}

TryGet<CollectionItem> 
CollectionSource::GetCardPrototype(std::string aszCardName)
{
   int iCacheIndex = LoadCard(aszCardName);
   return GetCardPrototype(iCacheIndex);
}

// Notifies all collections other than the input collection that they
// need to sync.
void 
CollectionSource::NotifyNeedToSync(const Location& aAddrForcedSync)
{
   std::vector<std::pair<Location, bool>>::iterator iter_SyncCols;
   for ( iter_SyncCols = m_lstSync.begin();
         iter_SyncCols != m_lstSync.end(); 
         ++iter_SyncCols )
   {
      if( iter_SyncCols->first != aAddrForcedSync )
      {
         iter_SyncCols->second = true;
      }
      else
      {
         iter_SyncCols->second = false;
      }
   }
}

bool CollectionSource::IsSyncNeeded(const Location& aAddrNeedSync)
{
   std::function<Location(const std::pair<Location, bool>&)> fnExtractor =
      [](const std::pair<Location, bool>& pAddrEx)->Location { return pAddrEx.first; };

   int iFound = ListHelper::List_Find(aAddrNeedSync, m_lstSync, fnExtractor);
   if (-1 != iFound)
   {
      return m_lstSync[iFound].second;
   }
   else
   {
      m_lstSync.push_back(std::make_pair(aAddrNeedSync, false));
      return false;
   }
}

std::vector<int>
CollectionSource::GetCollectionCache( Location aAddrColID, 
                                      CollectionItemType aColItemType )
{
   std::vector<int> lstRetVal;

   for (size_t i = 0; i < m_lstoCardCache.size(); i++)
   {
      if (m_lstoCardCache[i].FindCopies(aAddrColID, aColItemType).size() > 0)
      {
         lstRetVal.push_back(i);
      }
   }

   return lstRetVal;
}

std::vector<std::shared_ptr<CopyItem>>
CollectionSource::GetCollection( Location aAddrColID,
                                 CollectionItemType aColItemType )
{
   std::vector<std::shared_ptr<CopyItem>> lstRetVal;

   for( auto& item : m_lstoCardCache )
   {
      auto lstCopies = item.FindCopies(aAddrColID, aColItemType);

      auto iter_Copy = lstCopies.begin();
      for (; iter_Copy != lstCopies.end(); ++iter_Copy)
      {
         lstRetVal.push_back(*iter_Copy);
      }
   }

   return lstRetVal;
}

std::vector<std::string>
CollectionSource::GetAllCardsStartingWith(std::string aszText)
{
   std::vector<std::string> lstCards;

   bool bActiveCache = aszText.size() > 2;

   if (!bActiveCache)
   {
      m_lstSearchCache.clear();
   }

   // Check if the search is cached already
   if (bActiveCache)
   {
      int iEnd = m_lstSearchCache.size();
      for (int i = iEnd - 1; i >= 0; i--)
      {
         std::pair<std::string, std::vector<SourceObject>>
            pairICache = m_lstSearchCache[i];
         if (pairICache.first == aszText)
         {
            auto iter_Result = pairICache.second.begin();
            for (; iter_Result != pairICache.second.end(); ++iter_Result)
            {
               lstCards.push_back((iter_Result)->GetName(m_AllCharBuff));
            }

            return lstCards;
         }
         else if (aszText.substr(0, pairICache.first.size()) == pairICache.first)
         {
            break;
         }
         else
         {
            m_lstSearchCache.pop_back();
         }
      }
   }

   // If there are still entries in the searchCache, and we are here, then we only need to search
   //  the sublist.
   std::vector<SourceObject>* lstSearchList;
   std::vector<SourceObject> lstCache;
   if (bActiveCache && m_lstSearchCache.size() > 0)
   {
      lstSearchList = &m_lstSearchCache[m_lstSearchCache.size() - 1].second;
   }
   else
   {
      lstSearchList = &m_lstCardBuffer;
   }

   std::vector<std::string> lstStartCards;
   std::vector<std::string> lstOthers;

   std::vector<SourceObject>::iterator iter_Cards = lstSearchList->begin();
   for (; iter_Cards != lstSearchList->end(); ++iter_Cards)
   {
      std::string szCard = iter_Cards->GetName(m_AllCharBuff);
      std::transform(szCard.begin(), szCard.end(), szCard.begin(), ::tolower);
      size_t iFindIndex = 0;
      iFindIndex = szCard.find(aszText);
      if (iFindIndex != std::string::npos)
      {
         if (iFindIndex == 0)
         {
            lstStartCards.push_back(iter_Cards->GetName(m_AllCharBuff));
         }
         else
         {
            lstOthers.push_back(iter_Cards->GetName(m_AllCharBuff));
         }

         if (bActiveCache)
         {
            lstCache.push_back(*iter_Cards);
         }
      }
   }

   for (std::string sz : lstStartCards)
   {
      lstCards.push_back(sz);
   }
   for (std::string sz : lstOthers)
   {
      lstCards.push_back(sz);
   }

   if (bActiveCache)
   {
      m_lstSearchCache.push_back(std::make_pair(aszText, lstCache));
   }

   return lstCards;
}

void 
CollectionSource::ClearCache()
{
   m_lstoCardCache.clear();
}

bool
CollectionSource::IsLoaded()
{
   return m_bIsLoaded;
}

int
CollectionSource::findInBuffer(std::string aszCardName, bool abCaseSensitive)
{
   // Binary search chokes on all sorts of characters so Im just 
   // using linear search.
   std::string szCardNameFixed = StringHelper::convertToSearchString(aszCardName);
	if (!abCaseSensitive)
	{
		std::transform( szCardNameFixed.begin(),
                      szCardNameFixed.end(),
                      szCardNameFixed.begin(), ::tolower );
	}

	int iSize = m_lstCardBuffer.size();
	int iLeft = 0;
	int iRight = iSize;
	if (iRight < 1)
	{
		return -1;
	}

	std::string szName;
	while (iLeft <= iRight)
	{
		int middle = (iLeft + iRight) / 2;

		if (middle < 0 || middle >= iSize)
		{
			return -1;
		}

		szName = m_lstCardBuffer.at(middle).GetName(m_AllCharBuff);
      szName = StringHelper::convertToSearchString(szName);
		if (!abCaseSensitive)
		{
			std::transform( szName.begin(), szName.end(), 
                         szName.begin(), ::tolower );
		}

		if (szName == szCardNameFixed)
			return middle;
		else if (szName < szCardNameFixed)
			iLeft = middle + 1;
		else
			iRight = middle - 1;
	}
   return -1;
}

int
CollectionSource::findInCache( std::string aszName,
                               bool abCaseSensitive )
{
   auto iter_ColObj = m_lstoCardCache.begin();
   int index = 0;
   for (; iter_ColObj != m_lstoCardCache.end(); ++iter_ColObj)
   {
      if (iter_ColObj->GetName() == aszName)
      {
         return index;
      }
      index++;
   }
   return -1;
}

void CollectionSource::resetBuffer()
{
   m_lstoCardCache.clear();
   m_lstCardBuffer.clear();

   m_iAllCharBuffSize = 0;
   delete[] m_AllCharBuff;
   m_AllCharBuff = new char[ms_iMaxBufferSize];
}

void CollectionSource::finalizeBuffer()
{
   char* newBufferSize = new char[m_iAllCharBuffSize];
   memcpy(newBufferSize, m_AllCharBuff, m_iAllCharBuffSize);
   delete[] m_AllCharBuff;
   m_AllCharBuff = newBufferSize;
}
