﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CollectorsFrontEnd.InterfaceModels
{
    public class CardModel : IDataModel, INotifyPropertyChanged
    {
        public string CardName { get; set; }
        public int Amount { get; set; }
        public string CardNameLong;

        public List<Tuple<string, string>> LstMetaTags;
        public List<Tuple<string, string>> LstSpecifiedAttrs; // Attrs that can change between copy such as 'Set'
        public List<Tuple<string, string>> LstIdentifiedAttrs; // Attrs that define a copy into a card class.

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private BitmapImage _CardImage;
        public BitmapImage CardImage
        {
            get
            {
                return _CardImage;
            }
            set
            {
                _CardImage = value;
                OnPropertyChanged("CardImage");
            }
        }

        public CardModel(string aszCardName,
            List<Tuple<string, string>> aLstSpecifiedAttrs,
            List<Tuple<string, string>> aLstIdentifiedAttrs,
            List<Tuple<string, string>> aLstMetaTags)
        {
            CardName = aszCardName;
            LstMetaTags = aLstMetaTags;
            LstSpecifiedAttrs = aLstSpecifiedAttrs;
            LstIdentifiedAttrs = aLstIdentifiedAttrs;
            /*
            if (LstMetaTags.FirstOrDefault(x => x.Item1 == "Generalization") == null)
            {
                LstMetaTags.Add(new Tuple<string, string>("Generalization", "Main"));
            }
            */
        }

        public void SetAuxData(int aiAmount, string aszCardNameLong)
        {
            Amount = aiAmount;
            CardNameLong = aszCardNameLong;
        }

        public string GetMetaTag(string aszKey)
        {
            string szRetVal = "";
            foreach (Tuple<string, string> KeyVal in LstMetaTags)
            {
                if (KeyVal.Item1 == aszKey)
                {
                    szRetVal = KeyVal.Item2;
                    break;
                }
            }
            return szRetVal;
        }

        public bool IsSameAs(CardModel aoCardModel)
        {
            return ServerInterfaceModel.CardClassInterfaceModel.AreCardsSame(this, aoCardModel);
        }

        public void GetImage()
        {
            if (CardImage == null)
            {
                ServerInterfaceModel.CardClassInterfaceModel.DownloadAndCacheImage(ImageLoaded, this);
            }
            else
            {
                OnPropertyChanged("CardImage");
            }
        }

        private void ImageLoaded(object sender, EventArgs e)
        {
            CardImage = (BitmapImage)sender;
        }
    }
}
