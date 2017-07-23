﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreFrontPro.Server
{
    public partial class ServerInterface
    {

        public class CollectionIFace
        {
            public void SaveCollection(string aszCollectionName)
            {
                singleton.enqueueService(() =>
                {
                    SCI.SaveCollection(aszCollectionName);
                });
            }

            public void SetBaselineHistory(string aszCollection)
            {
                //SCI.SetBaselineHistory(aszCollection);
            }

            public void LoadBulkChanges(string aszCollection, List<string> alstChanges, Action aTask, bool UICallback = false)
            {
                singleton.enqueueService(() =>
                {
                    if (alstChanges != null)
                    {
                        SCI.SubmitBulkChanges(aszCollection, alstChanges);
                        aTask();
                    }
                }, UICallback);

            }

            public void GetCollectionMetaData(string aszCollectionName, Action<List<string>> aCallback, bool UICallback)
            {
                singleton.enqueueService(()=> {
                    aCallback(SCI.GetCollectionMetaData(aszCollectionName));
                }, UICallback);
            }

            public List<string> GetCollectionMetaDataSync(string aszColID)
            {
                    return SCI.GetCollectionMetaData(aszColID);
            }

            public void GetCollectionList(string aszColID, bool abCollapsed, Action<List<string>> aCallback, bool UICallback = false)
            {
                singleton.enqueueService(() =>
                {
                    aCallback(SCI.GetCollectionList(aszColID, abCollapsed));
                }, UICallback);
            }
        }

    }
}
