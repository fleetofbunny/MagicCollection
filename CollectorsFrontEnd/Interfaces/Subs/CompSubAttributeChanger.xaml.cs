﻿using CollectorsFrontEnd.InterfaceModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CollectorsFrontEnd.Interfaces.Subs
{
    /// <summary>
    /// Interaction logic for CompSubAttributeChanger.xaml
    /// </summary>
    public partial class CompSubAttributeChanger : UserControl, IComponent, INotifyPropertyChanged
    {
        #region Data Binding
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Sub Types
        public class CompSubAttributeChangerModel: IDataModel
        {
            public CompSubAttributeChangerModel(CardModel aDataModel, List<Tuple<string, string>> aLstCurrentTags)
            {
                CardModelObject = aDataModel;
                LstCurrentMetaTags = new List<Tuple<string, string>> (aLstCurrentTags);
            }
            public CardModel CardModelObject;
            
            public List<Tuple<string, string>> LstCurrentMetaTags { get; set; }
        }
        #endregion

        #region Events
        public event ComponentEvent UnhandledEvent;
        #endregion Events

        #region Public Fields
        public CompSubAttributeChangerModel DataModel;
        #endregion Public Fields

        #region Private Fields
        private UserControl m_OverlayControl;
        #endregion

        #region Public Methods
        public CompSubAttributeChanger(CardModel aDataModel)
        {
            InitializeComponent();
            
            DataModel = new CompSubAttributeChangerModel(aDataModel, aDataModel.LstMetaTags);
            DataModel.CardModelObject.PropertyChanged += eImageLoaded;
            DataModel.CardModelObject.GetImage();
            DataContext = DataModel;
        }

        public IDataModel GetDataModel()
        {
            return DataModel;
        }

        #endregion Public Methods

        #region Private Methods
        private void showKeyValCreaterWindow()
        {
            showMainDisplay();

            CompSubKeyValCreater ITI = new CompSubKeyValCreater();
            m_OverlayControl = ITI;
            ITI.UnhandledEvent += RouteReceivedUnhandledEvent;
            Panel.SetZIndex(GrdOverlay, 2);
            GrdOverlay.Children.Add(ITI);
            GrdMain.IsEnabled = false;
        }


        private void showMainDisplay()
        {
            GrdOverlay.Children.Remove(m_OverlayControl);
            m_OverlayControl = null;
            GrdMain.IsEnabled = true;
        }
        #endregion

        #region Public Event Handlers
        public void RouteReceivedUnhandledEvent(IDataModel aDataObject, string aszAction)
        {
            if (aDataObject.GetType() == typeof(CompSubKeyValCreater.CompSubKeyValCreaterModel))
            {
                CompSubKeyValCreater.CompSubKeyValCreaterModel oDataModel =
                    (CompSubKeyValCreater.CompSubKeyValCreaterModel)aDataObject;
                if (aszAction == "OK")
                {
                    ecAddMetaTag(oDataModel);
                }
                else if (aszAction == "Cancel")
                {
                    showMainDisplay();
                }
            }
        }
        #endregion Public EH

        #region Private Event Handlers
        private void ecAddMetaTag(CompSubKeyValCreater.CompSubKeyValCreaterModel aDataObject)
        {
            if (aDataObject.Key != "")
            {
                DataModel.LstCurrentMetaTags.Add(new Tuple<string, string>(aDataObject.Key, aDataObject.Value));
                DGMetas.Items.Refresh();
            }
            showMainDisplay();
        }

        private void eImageLoaded(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CardImage")
            {
                CardModel dataModel = (CardModel)sender;
                CardImage = (BitmapImage)dataModel.CardImage;
            }
        }
        #endregion Private EH

        #region GUI Event Handlers
        private void eOK_Click(object sender, RoutedEventArgs e)
        {
            UnhandledEvent(DataModel, "OK");
        }

        private void eCancel_Click(object sender, RoutedEventArgs e)
        {
            UnhandledEvent(DataModel, "Cancel");
        }

        private void eAddTag_Click(object sender, RoutedEventArgs e)
        {
            showKeyValCreaterWindow();
        }

        private void eRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            showMainDisplay();
        }

        #endregion GUI EH
    }
}