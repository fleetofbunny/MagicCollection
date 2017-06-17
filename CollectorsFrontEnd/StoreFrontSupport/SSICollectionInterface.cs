﻿using CollectorsFrontEnd.InterfaceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorsFrontEnd.StoreFrontSupport
{
    public static partial class ServerInterface
    {

        public static class Collection
        {
            public static void SaveCollection(string aszCollectionName)
            {
                SCI.SaveCollection(aszCollectionName);
            }

            public static void AddItem(string aszCollectionName, string aszCardNameLong, List<Tuple<string, string>> lstMeta)
            {
                SCI.AddItem(aszCollectionName, aszCardNameLong, lstMeta);
            }

            public static void RemoveItem(string aszCollectionName, string aszCardNameLong, List<Tuple<string, string>> lstMeta)
            {
                SCI.RemoveItem(aszCollectionName, aszCardNameLong, lstMeta);
            }

            public static void SetBaselineHistory(string aszCollection)
            {
                SCI.SetBaselineHistory(aszCollection);
            }

            public static void LoadBulkChanges(string aszCollection, List<string> alstChanges)
            {
                SCI.LoadBulkChanges(aszCollection, alstChanges);
            }

            /// <summary>
            /// Calls the server for the most up to date list of copies.
            /// </summary>
            /// <param name="aszCollectionName"></param>
            public static void Refresh(string aszCollectionName)
            {
                if (Server.GetCollectionModel(aszCollectionName) != null)
                {
                    // List of [ { CardNameLong, [Tags, ...] }, ... ]
                    List<Tuple<string, List<Tuple<string, string>>>> lstCards =
                        SCI.GetCollectionListWithMeta(aszCollectionName);
                    CollectionModel CMCurrent = ms_lstCollectionModels.FirstOrDefault(x => x.CollectionName == aszCollectionName);
                    if (CMCurrent != null)
                    {
                        CMCurrent.BuildCopyModelList(lstCards);
                    }
                }
            }
        }

    }
}