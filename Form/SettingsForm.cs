using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ForecastingModule.Controller;

namespace ForecastingModule.OtherForm
{
    public partial class SettingsView : Form, IView
    {
        public SettingsView(Tuple<String, List<Tuple<String, String>>> model)
        {
            setModel(model);
            this.table = model.Item1;

            InitializeComponent();
        }
    }
}
