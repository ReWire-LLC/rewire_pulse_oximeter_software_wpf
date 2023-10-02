using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PulseOximeter.Model;
using PulseOximeter.Utilities;
using PulseOximeter.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace PulseOximeter.ViewModel
{
    public class MainWindowViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ApplicationModel _model;
        private bool _detailed_view = false;

        private TimeSpan _ppg_lookback_duration = TimeSpan.FromSeconds(30);
        private PlotModel _ppg_plotmodel = new PlotModel();
        private List<DateTime> _ppg_plot_xvals = new List<DateTime>();
        private List<int> _ppg_plot_yvals = new List<int>();

        private TimeSpan _hr_lookback_duration = TimeSpan.FromMinutes(30);
        private PlotModel _hr_plotmodel = new PlotModel();
        private List<DateTime> _hr_plot_xvals = new List<DateTime>();
        private List<int> _hr_plot_yvals = new List<int>();
        private DateTime _hr_plotmodel_last_update_time = DateTime.MinValue;

        private TimeSpan _spo2_lookback_duration = TimeSpan.FromMinutes(30);
        private PlotModel _spo2_plotmodel = new PlotModel();
        private List<DateTime> _spo2_plot_xvals = new List<DateTime>();
        private List<int> _spo2_plot_yvals = new List<int>();
        private DateTime _spo2_plotmodel_last_update_time = DateTime.MinValue;

        #endregion

        #region Constructor

        public MainWindowViewModel(ApplicationModel model)
        {
            _model = model;
            _model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
            _model.PropertyChanged += HandleSpecificModelPropertyChanges;

            Initialize_PPG_PlotModel();
            Initialize_HR_PlotModel();
        }

        #endregion

        #region OxyPlot Setup Code

        private void Initialize_PPG_PlotModel ()
        {
            LinearAxis x_axis = new LinearAxis()
            {
                Minimum = 0,
                Maximum = _ppg_lookback_duration.TotalSeconds,
                Position = AxisPosition.Bottom,
                LabelFormatter = x => null,
                IsPanEnabled = false,
                IsZoomEnabled = false,
            };

            LinearAxis y_axis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                LabelFormatter = x => null,
                IsPanEnabled = false,
                IsZoomEnabled = false,
            };

            LineSeries line_series = new LineSeries()
            {
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1,
                Color = OxyColors.Blue
            };

            _ppg_plotmodel.PlotAreaBorderThickness = new OxyPlot.OxyThickness(1, 0, 0, 1);
            _ppg_plotmodel.PlotMargins = new OxyPlot.OxyThickness(0);
            _ppg_plotmodel.Axes.Add(x_axis);
            _ppg_plotmodel.Axes.Add(y_axis);
            _ppg_plotmodel.Series.Add(line_series);
            _ppg_plotmodel.InvalidatePlot(true);
        }

        private void Initialize_HR_PlotModel ()
        {
            LinearAxis x_axis = new LinearAxis()
            {
                Minimum = 0,
                Maximum = _hr_lookback_duration.TotalSeconds,
                Position = AxisPosition.Bottom,
                LabelFormatter = x => null,
                IsPanEnabled = false,
                IsZoomEnabled = false,
            };

            LinearAxis y_axis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                Minimum = 0,
                MinimumRange = 50
            };

            LineSeries line_series = new LineSeries()
            {
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1,
                Color = OxyColors.Blue
            };

            _hr_plotmodel.PlotAreaBorderThickness = new OxyPlot.OxyThickness(1, 0, 0, 1);
            _hr_plotmodel.PlotMargins = new OxyPlot.OxyThickness(0);
            _hr_plotmodel.Axes.Add(x_axis);
            _hr_plotmodel.Axes.Add(y_axis);
            _hr_plotmodel.Series.Add(line_series);
            _hr_plotmodel.InvalidatePlot(true);
        }

        #endregion

        #region OxyPlot Update Code

        private void HandleSpecificModelPropertyChanges(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null)
            {
                if (e.PropertyName.Equals("IR"))
                {
                    Update_PPG_PlotModel();
                }
                else if (e.PropertyName.Equals("HeartRate"))
                {
                    Update_HR_PlotModel();
                }
            }
        }

        private void Update_PPG_PlotModel ()
        {
            var now_datetime = DateTime.Now;
            var ir_value = _model.IR;
            _ppg_plot_xvals.Add(now_datetime);
            _ppg_plot_yvals.Add(ir_value);

            int last_index_to_remove = _ppg_plot_xvals.FindLastIndex(0, x => x < (now_datetime - _ppg_lookback_duration));
            if (last_index_to_remove > -1)
            {
                _ppg_plot_xvals.RemoveRange(0, last_index_to_remove + 1);
                _ppg_plot_yvals.RemoveRange(0, last_index_to_remove + 1);
            }

            var line_series = _ppg_plotmodel.Series.FirstOrDefault() as LineSeries;
            if (line_series != null)
            {
                line_series.Points.Clear();
                var oldest_datetime = _ppg_plot_xvals[0];
                var datapoints = _ppg_plot_xvals.Zip(_ppg_plot_yvals, (first, second) => new DataPoint((first - oldest_datetime).TotalSeconds, second)).ToList();
                line_series.Points.AddRange(datapoints);
            }

            _ppg_plotmodel.InvalidatePlot(true);
        }

        private void Update_HR_PlotModel ()
        {
            var now_datetime = DateTime.Now;
            var ir_value = _model.HeartRate;
            _hr_plot_xvals.Add(now_datetime);
            _hr_plot_yvals.Add(ir_value);

            int last_index_to_remove = _hr_plot_xvals.FindLastIndex(0, x => x < (now_datetime - _hr_lookback_duration));
            if (last_index_to_remove > -1)
            {
                _hr_plot_xvals.RemoveRange(0, last_index_to_remove + 1);
                _hr_plot_yvals.RemoveRange(0, last_index_to_remove + 1);
            }

            var line_series = _hr_plotmodel.Series.FirstOrDefault() as LineSeries;
            if (line_series != null)
            {
                line_series.Points.Clear();
                var oldest_datetime = _hr_plot_xvals[0];
                var datapoints = _hr_plot_xvals.Zip(_hr_plot_yvals, (first, second) => new DataPoint((first - oldest_datetime).TotalSeconds, second)).ToList();
                line_series.Points.AddRange(datapoints);
            }

            _hr_plotmodel.InvalidatePlot(true);
        }

        #endregion

        #region Properties

        public PlotModel PPG_PlotModel
        {
            get
            {
                return _ppg_plotmodel;
            }
        }

        public PlotModel HR_PlotModel
        {
            get
            {
                return _hr_plotmodel;
            }
        }

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

        public bool DetailedView
        {
            get
            {
                return _detailed_view;
            }
        }

        public string DetailedViewButtonText
        {
            get
            {
                if (_detailed_view)
                {
                    return "Standard View";
                }
                else
                {
                    return "Detailed View";
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "MuteAudio" })]
        public string MuteButtonText
        {
            get
            {
                if (_model.MuteAudio)
                {
                    return "Unmute";
                }
                else
                {
                    return "Mute";
                }
            }
        }

        #endregion

        #region Methods

        public void ToggleDetailedView ()
        {
            _detailed_view = !_detailed_view;
            NotifyPropertyChanged(nameof(DetailedView));
            NotifyPropertyChanged(nameof(DetailedViewButtonText));
        }

        public void ToggleMute ()
        {
            _model.MuteAudio = !_model.MuteAudio;
        }

        #endregion
    }
}
