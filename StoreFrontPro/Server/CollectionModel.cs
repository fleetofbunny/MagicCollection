﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using StoreFrontPro.Tools;
using System.Reflection;
using System.Collections.Specialized;
using StoreFrontPro.Views;

namespace StoreFrontPro.Server
{
   class CollectionDelta
   {
      public string Command { get; private set; }
      public string DisplayString { get; private set; }
      public int MaxRemoveCount { get; private set; }
      public List<string> SelectionOptions { get; private set; }

      private CollectionDelta(string Command, string DisplayString, int MaxRemove, List<string> Options)
      {
         this.Command = Command;
         this.DisplayString = DisplayString;
         this.MaxRemoveCount = MaxRemove;
         this.SelectionOptions = Options;
      }

      /// <summary>
      /// Constructs the Collection Delta with the given parameters.
      /// TODO: Ask to specify which UID to remove.
      /// </summary>
      /// <param name="AddCard"></param>
      /// <param name="RemoveCardIdealID"></param>
      /// <param name="Collection"></param>
      /// <returns></returns>
      public static CollectionDelta GetDelta(string AddCard, string RemoveCardIdealID, CollectionModel Collection)
      {
         string szDisplay, szCmdString, szAddCardProto;
         int iMaxDeltaCount = int.MaxValue;
         List<string> lstAdditionOptions = null;
         CardModel oRemoveCard = null;
         List<CardModel> lstModels = Collection.CollectionItems.Item;

         AddCard = AddCard ?? "";
         RemoveCardIdealID = RemoveCardIdealID ?? "";

         if( RemoveCardIdealID != "" )
         {
            oRemoveCard = Collection.GetCardModel(RemoveCardIdealID);
            if( oRemoveCard == null )
            {
               RemoveCardIdealID = "";
            }
            else
            {
               iMaxDeltaCount = 0;
               lstModels.ForEach(x => {
                  if (oRemoveCard.GetMetaTag("__hash") == x.GetMetaTag("__hash"))
                  {
                     iMaxDeltaCount += x.Count;
                  }
               });
            }

         }

         if( AddCard != "" )
         {
            szAddCardProto = ServerInterface.Card.GetProtoType(AddCard);
            if( szAddCardProto == "" )
            {
               AddCard = "";
            }
            else
            {
               CardModel.GetPrototype(AddCard).AttributeOptions.TryGetValue("set", out lstAdditionOptions);
            }
         }

         if( AddCard != "" && RemoveCardIdealID != "" )
         {
            szDisplay = "% " + oRemoveCard.GetIdealIdentifier();
            szDisplay += " -> " + AddCard;

            szCmdString = "% " + oRemoveCard.GetFullIdentifier();
            szCmdString += " -> " + AddCard;
         }
         else if( AddCard != "" )
         {
            szDisplay = "+ " + AddCard;
            szCmdString = "+ " + AddCard;
         }
         else if( RemoveCardIdealID != "" )
         {
            szDisplay = "- " + oRemoveCard.GetIdealIdentifier();
            szCmdString = "- " + oRemoveCard.GetFullIdentifier();
         }
         else
         {
            return null;
         }

         lstAdditionOptions = lstAdditionOptions ?? new List<string>() { "" };
         return new CollectionDelta(szCmdString, szDisplay, iMaxDeltaCount, lstAdditionOptions);
      }
   }

   partial class CollectionModel : IModel
   {
      public List<CardModel> Collection
      {
         get { return CollectionItems.Item; }
      }

      public string ID;
      public string CollectionName;
      public BasicModel<List<CardModel>> CollectionItems;

      public CollectionModel(string aszID)
      {
         CollectionItems = new BasicModel<List<CardModel>>(new List<CardModel>(), Sync);
         ID = aszID;

         // Synchonously build this collection.
         CollectionItems.Sync(ASync: false);
      }

      public void CreateChildCollection(string aszNewName)
      {
         ServerInterface.Server.CreateCollection(aszNewName, ID);
      }

      public void SetBaselineHistory()
      {

      }

      public void SaveCollection()
      {
         ServerInterface.Collection.SaveCollectionAS(ID);
      }

      public void SubmitBulkEdits(List<string> alstEdits)
      {
         ServerInterface.Collection.LoadBulkChangesAS(
             this.ID, alstEdits, () => { Sync(false); }, false);
      }

      public List<string> SearchCollection(string aszSearch)
      {
          List<string> lstRetVal = new List<string>();
          List<string> lstHoldVals = new List<string>();
          foreach (string item in Collection.Select(x => x.GetIdealIdentifier()))
          {
             int iMatchIndex = item.ToLower().IndexOf(aszSearch);
             if (iMatchIndex == 0)
             {
                lstRetVal.Add(item);
             }
             else if (iMatchIndex > 0)
             {
                lstHoldVals.Add(item);
             }
          }
          
          lstRetVal = lstRetVal.Concat(lstHoldVals).ToList();
          return lstRetVal;
      }

      public CollectionDelta GetDeltaCommand(string AddCard = "", string RemoveIdealIdentifier = "")
      {
         return CollectionDelta.GetDelta(AddCard, RemoveIdealIdentifier, this);
      }

      public CardModel GetCardModel(string IdealIdentifier)
      {
         return Collection.Where(x => x.GetIdealIdentifier() == IdealIdentifier).FirstOrDefault();
      }

      private void setCollectionModels(List<string> aLstCards)
      {
         var lstNewCards = new List<string>();
         var lstRemoveCards = new List<CardModel>();
         var lstUsedCards = new List<CardModel>();
         foreach( string szInspectCard in aLstCards )
         {
            var pairNameUIDs = fastExtractUIDs(szInspectCard);
            var szName = pairNameUIDs.Item1;
            var lstUIDs = pairNameUIDs.Item2;

            if( szName == "" ) { continue; }

            // Try to find a matching card model.
            var models = Collection.Where(x => x.PrototypeName == szName);

            // Record which uids no long exist. Forget about
            // Uids that exist.
            
            bool bFoundModel = false;
            foreach(var copy in models)
            {
               // If any of the UIDs in this model match
               // the any UID in the list, then this model
               // represents this line.
               bool bIsModel = false;
               foreach(var uid in copy.UIDs)
               {
                  if( lstUIDs.Contains(uid) )
                  {
                     bIsModel = true;
                     break;
                  }
               }

               if( bIsModel )
               {
                  var lstRemoveUIDs = new List<string>();
                  foreach(var uid in copy.UIDs)
                  {
                     if( !lstUIDs.Contains(uid) )
                     {
                        lstRemoveUIDs.Add(uid);
                     }
                     else
                     {
                        lstUIDs.Remove(uid);
                     }
                  }

                  // Remove the no long existent UIDs
                  foreach(var UID in lstRemoveUIDs)
                  {
                     copy.UIDs.Remove(UID);
                  }

                  // Add the new UIDs
                  foreach(var newUID in lstUIDs)
                  {
                     copy.UIDs.Add(newUID);
                  }

                  lstUsedCards.Add(copy);

                  bFoundModel = true;
                  break;
               }
            }

            if( !bFoundModel )
            {
               lstNewCards.Add(szInspectCard);
            }
         }
         // Anything not in lstUsedCards need to be removed.
         lstRemoveCards = Collection.Where(x => !lstUsedCards.Contains(x)).ToList();

         // Now we have new cards and used cards. Send those to the view.
                  // Remove the removed cards.
         ServerInterface.Server.SyncServerTask(() =>
         {
            lstRemoveCards.ForEach(x => CollectionItems.Item.Remove(x));
         });

         // Add new cards.
         ServerInterface.Server.GenerateCopyModelsAS(
            Identifiers: lstNewCards,
            CollectionName: CollectionName,
            Callback: (lst) => { lst.ForEach(x => { CollectionItems.Item.Add(x); }); },
            UICallback: true);

         ServerInterface.Server.SyncServerTask(CollectionItems.NotifyViewModel);
      }
      /*
      /// <summary>
      /// The list of cards come in like,
      /// Long Name : { uid1, uid2 }
      /// </summary>
      /// <param name="aLstCards"></param>
      private void setCollectionModels(List<string> aLstCards)
      {
         // Calculate differences.
         List<string> lstHashesAndCounts = aLstCards
             .Select(x => fastExtractHash(x, true)).ToList();

         List<string> lstNewHashes = lstHashesAndCounts
             .Select(x => x.Split(',')[1]).ToList();

         List<string> lstNewCounts = lstHashesAndCounts
             .Select(x => x.Split(',')[0] == "" ? (1).ToString() : x.Split(',')[0]).ToList();

         List<CardModel> lstRemoves = new List<CardModel>();
         if (!m_bHardRebuild)
         {
            foreach (CardModel cm in CollectionItems.Item)
            {
               // Since count is not picked up in the hash, it must be checked for.
               int iCount;
               string szTargetHash = cm.GetMetaTag("__hash");
               int iFound = lstNewHashes.IndexOf(szTargetHash);

               if( ( iFound != -1 ) &&
                   ( int.TryParse(lstNewCounts[iFound], out iCount) ) &&
                   ( iCount == cm.Count ) )
               {
                  // These CMs stay in the list
                  lstNewCounts.RemoveAt(iFound);
                  lstNewHashes.RemoveAt(iFound);
                  aLstCards.RemoveAt(iFound);
               }
               else
               {
                  // These CMs are removed
                  lstRemoves.Add(cm);
               }
            }
         }
         else
         {
            CollectionItems.Item.Clear();
         }

         // Remove the removed cards.
         ServerInterface.Server.SyncServerTask(() =>
         {
            lstRemoves.ForEach(x => CollectionItems.Item.Remove(x));
         });

         // Add new cards.
         ServerInterface.Server.GenerateCopyModelsAS(
            Identifiers: aLstCards,
            CollectionName: CollectionName,
            Callback: (lst) => { lst.ForEach(x => { CollectionItems.Item.Add(x); }); },
            UICallback: true);

         ServerInterface.Server.SyncServerTask(CollectionItems.NotifyViewModel);
         m_bHardRebuild = false;
      }
      */
      private void analyzeMetaData(List<string> alstMeta)
      {
         var itemTags = new List<string>();
         var collectionTags = new List<string>();
         foreach (string szLine in alstMeta)
         {
            if (szLine.Contains(":"))
            {
               itemTags.Add(szLine);
            }
            else
            {
               collectionTags.Add(szLine);
            }
         }

         foreach(var itemTag in itemTags)
         {
            // TODO: Handle These.
         }

         foreach(var collectionTag in collectionTags)
         {
            List<string> lstSplitLine = collectionTag.Split('=').ToList();
            if (lstSplitLine.Count > 1)
            {
               string szKey = lstSplitLine[0];
               string szVal = lstSplitLine[1];
               if (szKey == "Name")
               {
                  CollectionName = szVal.Trim('"');
               }
            }
         }
      }

      /// <summary>
      /// WithCount is included be a count change will not be detected by the hash.
      /// If withcount is true, there is guaranteed to be a comma in the return.
      /// </summary>
      /// <param name="aszIdentifier"></param>
      /// <param name="WithCount"></param>
      /// <returns></returns>
      private string fastExtractHash(string aszIdentifier, bool WithCount = false)
      {
         string szWithCount = "";
         int iFirstSpace = aszIdentifier.IndexOf(' ');
         if (WithCount)
         {
            szWithCount = aszIdentifier.Substring(0, iFirstSpace);
            if (szWithCount[0] == 'x')
            {
               szWithCount = szWithCount.Substring(1, iFirstSpace - 1);
            }
            szWithCount += ",";
         }

         int iHashStart = aszIdentifier.IndexOf("__hash");
         if (!(iHashStart >= 0 && iHashStart < aszIdentifier.Length)) { return ""; }
         string remainingString = aszIdentifier.Substring(iHashStart);

         int iOpeningQuote = remainingString.IndexOf('\"');
         if (!(iOpeningQuote >= 0 && iOpeningQuote < aszIdentifier.Length)) { return ""; }

         int iClosingQuote = remainingString.IndexOf("\"", iOpeningQuote + 1);
         if (!(iClosingQuote >= 0 && iClosingQuote < aszIdentifier.Length)) { return ""; }

         return szWithCount + remainingString.Substring(iOpeningQuote + 1, iClosingQuote - iOpeningQuote - 1).Trim();
      }

      /// <summary>
      /// Returns a list of UIDs from the passed in card.
      /// </summary>
      /// <param name="aszCard"></param>
      /// <returns></returns>
      private Tuple<string, List<string>> fastExtractUIDs(string aszCard)
      {
         List<string> lstCard = aszCard.Split(':').ToList();
         if( lstCard.Count > 1 )
         {
            string szUIDs = lstCard[1];
            List<Tuple<string,string>> lstParseUIDs = CardModel.ParseTagList(szUIDs);
            var lstUIDs = lstParseUIDs.Select(x => x.Item2).ToList();
            
            string szName;
            int iNameEnd = lstCard[0].IndexOf('{');
            if( iNameEnd > 0 )
            {
               szName = lstCard[0].Substring(0, iNameEnd).Trim();
            }
            else
            {
               szName = lstCard[0];
            }

            //If there is a number leading the name, remove it.
            int iCount;
            int iIndexX = szName.IndexOf('x');
            int iIndexS = szName.IndexOf(' ');
            if( iIndexX < iIndexS )
            {
               string szTryName = szName.Substring(iIndexX+1, iIndexS-iIndexX-1);
               if( int.TryParse(szTryName, out iCount) )
               {
                  szName = szName.Substring(iIndexS+1);
               }
            }

            return new Tuple<string, List<string>>(szName, lstUIDs);
         }

         return new Tuple<string, List<string>>("", new List<string>());
      }

      #region IModel
      private bool notify = true;
      private List<WeakReference<IViewModel>> viewModels = new List<WeakReference<IViewModel>>();
      public void NotifyViewModel()
      {
         if( !notify ) { return; }
         viewModels.ForEach(x => 
         {
            IViewModel model;
            if(x.TryGetTarget(out model))
            {
               model.ModelUpdated();
            }
         });
      }

      public void Register(IViewModel item)
      {
         viewModels.Add(new WeakReference<IViewModel>(item));
      }

      public void UnRegister(IViewModel item)
      {
         // Find the model that corresponds.
         var lstRemoves = new List<WeakReference<IViewModel>>();
         foreach(var model in viewModels)
         {
            IViewModel test;
            if(model.TryGetTarget(out test))
            {
               if( test == item )
               {
                  lstRemoves.Add(model);
               }
            }
         }

         foreach( var model in lstRemoves )
         {
            viewModels.Remove(model);
         }
      }

      public void EnableNotification(bool abNotify = false)
      {
         notify = true;
         if (abNotify) { NotifyViewModel(); }
      }

      public void DisableNotification()
      {
         notify = false;
      }

      public void Sync(bool ASync = true)
      {
         if (ASync)
         {
            ServerInterface.Server.SyncServerTask(()=> { Sync(false); });
         }
         else
         {
            List<string> lstMDs = ServerInterface.Collection.GetCollectionMetaData(ID);
            analyzeMetaData(lstMDs);

            List<string> lstItems = ServerInterface.Collection.GetCollectionList(ID);
            setCollectionModels(lstItems);

            NotifyViewModel();
         }
      }
      #endregion
   }
}
