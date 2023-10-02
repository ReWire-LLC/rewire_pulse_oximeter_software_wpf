using PulseOximeter.Model;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PulseOximeter.ViewModel
{
    public class MainWindowViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ApplicationModel _model;

        #endregion

        #region Constructor

        public MainWindowViewModel(ApplicationModel model)
        {
            _model = model;
            _model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
        }

        #endregion

        #region Properties

        [ReactToModelPropertyChanged(new string[] { "HeartRate" })]
        public string HeartRate
        {
            get
            {
                return _model.HeartRate.ToString();
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SpO2" })]
        public string SpO2
        {
            get
            {
                return _model.SpO2.ToString();
            }
        }

        [ReactToModelPropertyChanged(new string[] { "HeartRate" })]
        public SolidColorBrush HeartRateBackground
        {
            get
            {
                if (_model.HeartRate < _model.HeartRateAlarm_Minimum || _model.HeartRate > _model.HeartRateAlarm_Maximum)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                else
                {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SpO2" })]
        public SolidColorBrush SpO2Background
        {
            get
            {
                if (_model.SpO2 < _model.SpO2Alarm_Minimum || _model.SpO2 > _model.SpO2Alarm_Maximum)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                else
                {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "ConnectionState" })]
        public string ConnectionState
        {
            get
            {
                return DeviceConnectionStateConverter.ConvertToDescription(_model.ConnectionState);
            }
        }

        #endregion
    }
}
