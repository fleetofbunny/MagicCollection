#include "CollectionIO.h"



CollectionIO::CollectionIO()
{
}


CollectionIO::~CollectionIO()
{
}

std::vector<std::string> CollectionIO::GetFileLines(std::string aszFileName)
{
	// Load the collection
	std::ifstream in(aszFileName);
	std::stringstream buffer;
	buffer << in.rdbuf();
	in.close();
	std::string contents(buffer.str());

	return StringHelper::SplitIntoLines(contents);
}

// Returns non-preprocessing lines.
bool CollectionIO::GetPreprocessLines(std::vector<std::string> alstAllLines, std::vector<std::string>& rlstIOLines, std::vector<std::string>& rlstPreprocessingLines)
{
	std::vector<std::string> lstRetVal;
	std::vector<std::string> lstPreprocessLines;
	std::vector<std::string>::iterator iter_Lines = alstAllLines.begin();
	std::string szDefKey(Config::CollectionDefinitionKey);
	for (; iter_Lines != alstAllLines.end(); ++iter_Lines)
	{
		if (iter_Lines->size() > 2 && iter_Lines->substr(0, szDefKey.size()) == szDefKey)
		{
			lstPreprocessLines.push_back(*iter_Lines);
		}
		else
		{
			lstRetVal.push_back(*iter_Lines);
		}
	}

	rlstPreprocessingLines = lstPreprocessLines;
	rlstIOLines = lstRetVal;
	return true;
}

bool 
CollectionIO::CaptureUnlistedItems(std::string aszCollectionName,
	CollectionSource* aptCollectionSource,
	std::map<int, std::list<CopyItem*>>& rlstAdditionalItems,
	std::map<int, std::list<CopyItem*>>& rlstAlreadyCapturedItems)
{
	// Identify already loaded cards in this collection.
	std::vector<int> lstAllPossibleCacheItems = aptCollectionSource->GetCollectionCache(aszCollectionName);
	for (size_t i = 0; i < lstAllPossibleCacheItems.size(); i++)
	{
		CollectionItem* itemPrototype = aptCollectionSource->GetCardPrototype(lstAllPossibleCacheItems[i]);
		std::vector<CopyItem*> lstPossibleLocals = itemPrototype->GetCopiesForCollection(aszCollectionName, CollectionItemType::Local);
		for (size_t t = 0; t < lstPossibleLocals.size(); t++)
		{
			CopyItem* cItem = lstPossibleLocals[t];
			std::list<CopyItem*> lstListItems = rlstAlreadyCapturedItems[lstAllPossibleCacheItems[i]];
			std::vector<CopyItem*> lstSearchItems = std::vector<CopyItem*>(lstListItems.begin(), lstListItems.end());
			if (cItem->IsResidentIn(aszCollectionName) &&
				-1 == ListHelper::List_Find(cItem, lstSearchItems))
			{
				rlstAdditionalItems[lstAllPossibleCacheItems[i]].push_back(cItem);
			}
		}
	}

	return true;
}

bool
CollectionIO::ConsolodateLocalItems(std::string aszCollectionName,
	CollectionSource* aptCollectionSource,
	std::map<int, std::list<CopyItem*>>& rlstPotentialDuplicates,
	std::map<int, std::list<CopyItem*>>& rlstNonDuplicates)
{
	std::function<std::string(CopyItem*)> fnSimpleExtractor;
	std::vector<CopyItem*> lstSearchItems;

	fnSimpleExtractor = [&](CopyItem* item)->std::string { return item->GetHash(); };

	for each (std::pair<int, std::list<CopyItem*>> pairItem in rlstPotentialDuplicates)
	{
		int iItem = pairItem.first;
		CollectionItem* item = aptCollectionSource->GetCardPrototype(iItem);
		std::list<CopyItem*> lstNewItems = pairItem.second;

		if (rlstNonDuplicates.find(iItem) != rlstNonDuplicates.end())
		{
			for each (CopyItem* cItem in lstNewItems)
			{
				lstSearchItems = std::vector<CopyItem*>(rlstNonDuplicates[iItem].begin(), rlstNonDuplicates[iItem].end());
				int iFound = ListHelper::List_Find(cItem->GetHash(), lstSearchItems, fnSimpleExtractor);
				if (-1 != iFound)
				{
					CopyItem* foundItem = lstSearchItems[iFound];
					rlstNonDuplicates.at(iItem).remove(foundItem);
					rlstPotentialDuplicates.at(iItem).remove(cItem);
					item->Erase(cItem);
				}
			}
		}
	}

	return true;
}

bool 
CollectionIO::ConsolodateBorrowedItems(std::string aszCollectionName,
	CollectionSource* aptCollectionSource,
	CollectionFactory* aptCollFactory)
{
	CollectionItem* itemPrototype;
	std::string szItemParent;
	std::string szItemHash;
	std::vector<CopyItem*> lstBorrowedItems;
	std::vector<CopyItem*> lstCItems;
	std::function<std::string(CopyItem*)> fnExtractor;
	std::vector<int> lstCacheIndexes = aptCollectionSource->GetCollectionCache(aszCollectionName);

	// Used to filter out already used existing copies
	fnExtractor = [aszCollectionName](CopyItem* item)->std::string
	{
		if (!item->IsResidentIn(aszCollectionName)) { return item->GetHash(); }
		else { return ""; }
	};

	for (size_t i = 0; i < lstCacheIndexes.size(); i++)
	{
		itemPrototype = aptCollectionSource->GetCardPrototype(lstCacheIndexes[i]);
		lstBorrowedItems = itemPrototype->GetCopiesForCollection(aszCollectionName, CollectionItemType::Borrowed);
		for (size_t t = 0; t < lstBorrowedItems.size(); t++)
		{
			szItemParent = lstBorrowedItems[t]->GetParent();
			szItemHash = lstBorrowedItems[t]->GetHash();
			if (aptCollFactory->CollectionExists(szItemParent)) // The aoFactory is used to check if the collection is loaded.
			{
				itemPrototype->Erase(lstBorrowedItems[t]);// This copy is erased either way. These were added as placeholders.

				lstCItems = itemPrototype->FindAllCopyItems(szItemHash, szItemParent); // This list will be checked for any unused copy that matches this description.
				int iFoundAlreadyUsed = ListHelper::List_Find(szItemHash, lstCItems, fnExtractor);
				if (iFoundAlreadyUsed != -1)
				{
					lstCItems[iFoundAlreadyUsed]->AddResident(aszCollectionName);
				}
			}
			else
			{ // Check if any other collection referenced the unverified copy.
			  // Get a list of all other cards that supposedly belong to this collection
				lstCItems = itemPrototype->GetCopiesForCollection(szItemParent, CollectionItemType::Local);

				int iFoundAlreadyUsed = ListHelper::List_Find(szItemHash, lstCItems, fnExtractor);
				if (iFoundAlreadyUsed != -1)
				{
					itemPrototype->Erase(lstBorrowedItems[t]);
					lstCItems[iFoundAlreadyUsed]->AddResident(aszCollectionName);
				}
			}
		}
	}

	return true;
}

bool 
CollectionIO::ReleaseUnfoundReferences(std::string aszCollectionName,
	CollectionSource* aptCollectionSource)
{
	CollectionItem* itemPrototype;
	std::vector<CopyItem*> lstPossibleLocals;

	auto lstAllPossibleCacheItems = aptCollectionSource->GetCollectionCache(aszCollectionName);
	for (size_t i = 0; i < lstAllPossibleCacheItems.size(); i++)
	{
		itemPrototype = aptCollectionSource->GetCardPrototype(lstAllPossibleCacheItems[i]);
		// This has to iterate over ALL cards because we don't know where dangling references are.
		// Get all copies that claim to be in this collection.
		lstPossibleLocals = itemPrototype->GetCopiesForCollection(aszCollectionName, CollectionItemType::Local);
		for (size_t t = 0; t < lstPossibleLocals.size(); t++)
		{
			if (!lstPossibleLocals[t]->IsResidentIn(aszCollectionName))
			{
				// This copy is not already resident in this collection. That means that this copy was loaded by a non-child collection.
				// We must check if that copy truly exists. If not, delete it.

				std::string szItemHash = lstPossibleLocals[t]->GetHash();
				// Duplicate duplicates because there might be a copy of an existing item.
				// The second param is empty because we want ALL items with a matching hash.
				std::vector<CopyItem*> lstDuplicateDuplicates = itemPrototype->FindAllCopyItems(szItemHash, "");

				// If there is more than one, count the number that were just added to this col, then try to find matching existing ones for each.
				// Make sure that we account for the fact that other collections can borrow up to the amount in this col.
				std::map<std::string, std::vector<CopyItem*>> mapColExistingItems;
				std::vector<CopyItem*> lstNewlyAddedItems;
				for (size_t q = 0; q < lstDuplicateDuplicates.size(); q++)
				{
					if (lstDuplicateDuplicates[q]->IsResidentIn(aszCollectionName))
					{
						lstNewlyAddedItems.push_back(lstDuplicateDuplicates[q]);
					}
					else
					{
						std::string szTargetCol = lstDuplicateDuplicates[q]->GetParent();
						mapColExistingItems[szTargetCol].push_back(lstDuplicateDuplicates[q]);
					}
				}

				// Now go through each collection and account for each one.
				std::map<std::string, std::vector<CopyItem*>>::iterator iter_existingCol = mapColExistingItems.begin();
				for (; iter_existingCol != mapColExistingItems.end(); ++iter_existingCol)
				{
					std::vector<CopyItem*>::iterator iter_existingColItem = iter_existingCol->second.begin();
					int q = 0;
					for (; iter_existingColItem != iter_existingCol->second.end() && q < lstNewlyAddedItems.size(); ++iter_existingColItem, q++)
					{
						lstNewlyAddedItems[q]->AddResident(iter_existingCol->first);
						itemPrototype->Erase(*iter_existingColItem);
					}

					// Delete the items unaccounted for.
					for (; q >= lstNewlyAddedItems.size() && iter_existingColItem != iter_existingCol->second.end(); ++iter_existingColItem, q++)
					{
						itemPrototype->Erase(*iter_existingColItem);
					}
				}
			}
		}
	}

	return true;
}

bool CollectionIO::CollectionFileExists(std::string aszFileName)
{
	std::string szFullFileName = GetCollectionFile(aszFileName);
	std::ifstream f(szFullFileName.c_str());
	return f.good();
}

std::string CollectionIO::GetCollectionFile(std::string aszCollectionName)
{
	return Config::Instance()->GetCollectionsDirectory() + "\\" +
		StringHelper::Str_Replace(aszCollectionName, ' ', '_') + ".txt";
}

std::string CollectionIO::GetMetaFile(std::string aszCollectionName)
{
	return Config::Instance()->GetCollectionsDirectory() + "\\" + Config::Instance()->GetMetaFolderName() + "\\" +
		StringHelper::Str_Replace(aszCollectionName, ' ', '_') + "." + std::string(Config::MetaFileExtension) + ".txt";
}

std::string CollectionIO::GetHistoryFile(std::string aszCollectionName)
{
	return Config::Instance()->GetCollectionsDirectory() + "\\" + Config::Instance()->GetHistoryFolderName() + "\\" +
		StringHelper::Str_Replace(aszCollectionName, ' ', '_') + "." + std::string(Config::HistoryFileExtension) + ".txt";
}