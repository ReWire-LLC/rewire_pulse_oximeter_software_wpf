using PulseOximeter.Model;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.ViewModel
{
    public class Window_SetAlarms_ViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ApplicationModel _application_model;

        #endregion

        #region Constructor

        public Window_SetAlarms_ViewModel(ApplicationModel model) 
        {
            _application_model = model;
        }

        #endregion

        #region Properties

        public string HeartRateMinimumAlarm
        {
            get
            {
                return _application_model.HeartRateAlarm_Minimum.ToString();
            }
        }

        public string HeartRateMaximumAlarm
        {
            get
            {
                return _application_model.HeartRateAlarm_Maximum.ToString();
            }
        }

        public string SpO2MinimumAlarm
        {
            get
            {
                return _application_model.SpO2Alarm_Minimum.ToString();
            }
        }

        public string SpO2MaximumAlarm
        {
            get
            {
                return _application_model.SpO2Alarm_Maximum.ToString();
            }
        }

        #endregion

        #region Methods

        public void ApplyAlarmSettings (int hr_min, int hr_max, int spo2_min, int spo2_max)
        {
            _application_model.HeartRateAlarm_Minimum = hr_min;
            _application_model.HeartRateAlarm_Maximum = hr_max;
            _application_model.SpO2Alarm_Minimum = spo2_min;
            _application_model.SpO2Alarm_Maximum = spo2_max;
        }

        #endregion
    }
}
