﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CollectorsFrontEnd.StoreFrontSupport
{
    public class ViewModel: INotifyPropertyChanged
    {
        protected UserControl _Model;
        public UserControl Model
        {
            get { return _Model; }
            set { _Model = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ViewModel(UserControl Model)
        {
            this.Model = Model;
        }
    }
}
