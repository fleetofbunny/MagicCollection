﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace StoreFrontPro.Server
{
   partial class ServerInterface
   {
      public class CardIFace
      {
         public string GetProtoType(string szCardName)
         {
            return SCI.GetCardPrototype(szCardName);
         }

         public void DownloadAndCacheImage(Action<BitmapImage> aCallback, CardModel aoCardModel)
         {
            ImageHandler.Handler.GetImage(aCallback, aoCardModel);
         }
      }
   }
}
