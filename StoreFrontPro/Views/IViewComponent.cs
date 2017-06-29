﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreFrontPro.Views
{
    #region Data Types
    public class DisplayEventArgs
    {
        public string Source;
        public string Property;
        public string Event;

        private Dictionary<string, object> m_lstParms = new Dictionary<string, object>();

        public DisplayEventArgs(string Source, string Property, string Event)
        {
            this.Source = Source;
            this.Property = Property;
            this.Event = Event;
        }

        public void Add(string Key, object Value)
        {
            m_lstParms.Add(Key, Value);
        }

        public object Get(string aszKey)
        {
            return m_lstParms.ContainsKey(aszKey) ? m_lstParms[aszKey] : null;
        }
    }

    public class StoreFrontMenuItem
    {
        public RelayCommand Operation;
        public string MenuName;

        public StoreFrontMenuItem(string Name, Action<object> Operation)
        {
            this.Operation = new RelayCommand(Operation);
            this.MenuName = Name;
        }
    }
    #endregion

    #region Events & Delegates
    public delegate void DisplayEventHandler(object Source, DisplayEventArgs Event);
    #endregion

    interface IViewComponent
    {
        #region Interface
        event DisplayEventHandler DisplayEvent;

        List<StoreFrontMenuItem> GetMenuItems();
        #endregion
    }
}
