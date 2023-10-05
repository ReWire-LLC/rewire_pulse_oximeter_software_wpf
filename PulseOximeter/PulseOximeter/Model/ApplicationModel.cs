using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PulseOximeter.Model
{
    public class ApplicationModel : NotifyPropertyChangedObject
    {
        #region Private constants

        private const int rewire_pulse_oximeter_vid = 0x04D8;
        private const int rewire_pulse_oximeter_pid = 0xE636;
        private const string rewire_pulse_oximeter_vid_string = "04D8";
        private const string rewire_pulse_oximeter_pid_string = "E636";

        #endregion

        #region Private data members

        private bool _recording = false;
        private string _recording_file = string.Empty;
        private DateTime _recording_start_time = DateTime.MinValue;
        private Stopwatch _recording_stopwatch = new Stopwatch();
        private RecordingState _recording_state = RecordingState.NotRecording;
        private StreamWriter? _recording_writer = null;
        private DateTime _recording_last_ui_update_time = DateTime.MinValue;

        private List<DateTime> _recent_ir_value_datetimes = new List<DateTime>();
        private List<int> _recent_ir_values = new List<int>();
        private DateTime _last_perfusion_index_update_time = DateTime.MinValue;
        private TimeSpan _perfusion_index_update_period = TimeSpan.FromSeconds(1);

        private ApplicationConfiguration _application_configuration = new ApplicationConfiguration();

        private DeviceConnectionState _device_connection_state = DeviceConnectionState.NoDevice;

        private BackgroundWorker _background_thread = new BackgroundWorker();
        private int _heart_rate = 0;
        private int _spo2 = 0;
        private int _ir = 0;
        private double _perfusion_index = 0;

        private SerialPort? _serial_port = null;
        private List<byte> _buffer = new List<byte>();
        private WaveOut _audio_player = new WaveOut();
        private DateTime _last_alarm_time = DateTime.MinValue;
        private TimeSpan _alarm_period = TimeSpan.FromSeconds(10);
        private DateTime _last_no_pulse_time = DateTime.MinValue;
        private TimeSpan _no_pulse_period = TimeSpan.FromSeconds(15);

        private Stopwatch _stopwatch = new Stopwatch();

        private int _alarm_hr_min = 50;
        private int _alarm_hr_max = 100;
        private int _alarm_spo2_min = 70;
        private int _alarm_spo2_max = 100;

        private bool _mute_audio = false;

        /// <summary>
        /// This pitch mapping was taken from the following published paper: 
        /// https://array.aami.org/doi/10.2345/0899-8205-56.2.46
        /// "Signaling Patient Oxygen Desaturation with Enhanced Pulse Oximetry Tones"
        /// Biomedical Information & Technology, 2022
        /// 
        /// Other useful links/papers:
        /// 1. https://journals.lww.com/anesthesia-analgesia/fulltext/2016/05000/the_sounds_of_desaturation__a_survey_of_commercial.26.aspx
        /// </summary>
        private Dictionary<int, int> _spo2_pitch_mapping = new Dictionary<int, int>()
        {
            { 100, 881 },
            { 99, 858 },
            { 98, 836 },
            { 97, 815 },
            { 96, 794 },
            { 95, 774 },
            { 94, 754 },
            { 93, 735 },
            { 92, 716 },
            { 91, 698 },
            { 90, 680 },
            { 89, 663 },
            { 88, 646 },
            { 87, 629 },
            { 86, 613 },
            { 85, 597 },
            { 84, 582 },
            { 83, 567 },
            { 82, 553 },
            { 81, 539 },
            { 80, 525 }
        };

        #endregion

        #region Constructor

        public ApplicationModel()
        {
            //Load the alarm values from the configuration class
            _alarm_hr_max = _application_configuration.HeartRateAlarmMaximum;
            _alarm_hr_min = _application_configuration.HeartRateAlarmMinimum;
            _alarm_spo2_max = _application_configuration.SpO2AlarmMaximum;
            _alarm_spo2_min = _application_configuration.SpO2AlarmMinimum;

            //Set up the background thread
            _background_thread.DoWork += _background_thread_DoWork;
            _background_thread.RunWorkerCompleted += _background_thread_RunWorkerCompleted;
            _background_thread.ProgressChanged += _background_thread_ProgressChanged;
            _background_thread.WorkerReportsProgress = true;
            _background_thread.WorkerSupportsCancellation = true;
        }

        #endregion

        #region Background thread methods

        private void _background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _device_connection_state = DeviceConnectionState.Error;
                NotifyPropertyChanged(nameof(ConnectionState));
            }
        }

        private void _background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                string? property_name = e.UserState as string;
                if (!string.IsNullOrWhiteSpace(property_name))
                {
                    NotifyPropertyChanged(property_name);
                }
            }
        }

        private void _background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Do some setup work
            string selected_com_port = string.Empty;
            _stopwatch.Start();

            //Loop forever
            while (!_background_thread.CancellationPending)
            {
                switch (_device_connection_state)
                {
                    case DeviceConnectionState.NoDevice:

                        //Just proceed to the next state
                        _background_thread.ReportProgress(0, new List<string>() { "0", "0" });
                        ConnectionState = DeviceConnectionState.SearchingForDevice;

                        break;
                    case DeviceConnectionState.SearchingForDevice:

                        //Search for the device
                        selected_com_port = BackgroundThread_FindConnectedDevice();
                        if (!string.IsNullOrWhiteSpace(selected_com_port))
                        {
                            ConnectionState = DeviceConnectionState.ConnectingToDevice;
                        }
                        else
                        {
                            ConnectionState = DeviceConnectionState.NoDevice;
                        }

                        break;
                    case DeviceConnectionState.ConnectingToDevice:

                        //Connect to the selected device
                        _serial_port = new SerialPort(selected_com_port);
                        _serial_port.Open();
                        if (_serial_port.IsOpen)
                        {
                            _serial_port.WriteLine("stream on");
                            ConnectionState = DeviceConnectionState.Connected;
                        }

                        break;

                    case DeviceConnectionState.Connected:

                        if (_serial_port != null && _serial_port.IsOpen)
                        {
                            try
                            {
                                BackgroundThread_HandleConnectedDevice();
                            }
                            catch (Exception ex)
                            {
                                //empty
                            }
                        }
                        else
                        {
                            ConnectionState = DeviceConnectionState.NoDevice;
                        }

                        break;
                }
            }
        }

        private void BackgroundThread_HandleConnectedDevice ()
        {
            //Receive the pulse oximetry data
            BackgroundThread_ReceivePulseOximeterData();

            //Handle audio output
            if (!_mute_audio)
            {
                BackgroundThread_HandleAudio();
            }
        }

        private void BackgroundThread_HandleRecording (string data_row)
        {
            switch (_recording_state)
            {
                case RecordingState.NotRecording:

                    //If the user has requested to start a recording, move to the "launching recording" state
                    if (_recording && !string.IsNullOrWhiteSpace(_recording_file))
                    {
                        _recording_state = RecordingState.LaunchingRecording;
                    }

                    break;
                case RecordingState.LaunchingRecording:

                    //In this state, we attempt to open a file for writing
                    try
                    {
                        _recording_writer = new StreamWriter(_recording_file);
                        _recording_state = RecordingState.Recording;

                        string first_line = "Milliseconds, IR, Red, HR, HR Confidence, SpO2, Algorithm State, Algorithm Status, Interbeat Interval";
                        _recording_writer.WriteLine(first_line);

                        _recording_start_time = DateTime.Now;
                        _recording_stopwatch.Restart();
                    }
                    catch (Exception ex)
                    {
                        _recording_writer = null;
                        _recording_state = RecordingState.NotRecording;
                        _recording = false;
                        _recording_stopwatch.Stop();
                        BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));
                    }
                    
                    break;
                case RecordingState.Recording:

                    double ts = _recording_stopwatch.ElapsedMilliseconds;
                    string new_line = ts + ", " + data_row;
                    try
                    {
                        //Record the current line of data
                        _recording_writer?.WriteLine(new_line);

                        //Check to see if the UI needs to be updated with recording information
                        if (DateTime.Now >= (_recording_last_ui_update_time + TimeSpan.FromSeconds(1)))
                        {
                            BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));
                        }

                        //Check to see the recording has been stopped/cancelled
                        if (!_recording)
                        {
                            //If so, go to the "stopping recording" state
                            _recording_state = RecordingState.StoppingRecording;
                        }
                    }
                    catch (Exception ex)
                    {
                        _recording_writer = null;
                        _recording_state = RecordingState.NotRecording;
                        _recording = false;
                        _recording_stopwatch.Stop();
                        BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));
                    }
                    
                    break;
                case RecordingState.StoppingRecording:

                    try
                    {
                        _recording_writer?.Close();
                        _recording_stopwatch.Stop();
                    }
                    catch (Exception ex)
                    {
                        //empty
                    }

                    _recording_state = RecordingState.NotRecording;
                    BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));

                    break;
            }
        }

        private void BackgroundThread_ReceivePulseOximeterData ()
        {
            //Handle input of the data from the pulse oximeter
            if (_serial_port != null && _serial_port.BytesToRead > 0)
            {
                byte[] current_read = new byte[_serial_port.BytesToRead];
                var total_bytes_read = _serial_port.Read(current_read, 0, current_read.Length);
                _buffer.AddRange(current_read.ToList());

                if (_buffer.Contains(0x0A))
                {
                    int index_of_newline = _buffer.IndexOf(0x0A);
                    var current_line_as_bytes = _buffer.Take(index_of_newline + 1);
                    string current_line = Encoding.UTF8.GetString(current_line_as_bytes.ToArray()).Trim();
                    if (_buffer.Count > (index_of_newline + 1))
                    {
                        _buffer = _buffer.Skip(index_of_newline + 1).ToList();
                    }
                    else
                    {
                        _buffer.Clear();
                    }

                    if (!string.IsNullOrWhiteSpace(current_line) && current_line.StartsWith("[DATA]"))
                    {
                        var split_current_line = current_line.Split('\t').ToList();
                        if (split_current_line.Count >= 9)
                        {
                            var ir_string = split_current_line[1];
                            var hr_string = split_current_line[3];
                            var spo2_string = split_current_line[5];

                            var ir_parse_success = Int32.TryParse(ir_string, out int ir);
                            var hr_parse_success = Int32.TryParse(hr_string, out int hr);
                            var spo2_parse_success = Int32.TryParse(spo2_string, out int spo2);

                            if (hr_parse_success && spo2_parse_success && ir_parse_success)
                            {
                                IR = ir;
                                UpdatePerfusionIndex(ir);

                                if (_stopwatch.ElapsedMilliseconds >= 1000)
                                {
                                    HeartRate = hr;
                                    SpO2 = spo2;

                                    _stopwatch.Restart();
                                }

                                //Handle recording of data
                                BackgroundThread_HandleRecording(current_line.Substring(7).Replace("\t", ", "));
                            }
                        }
                    }
                }
            }
        }

        private void UpdatePerfusionIndex (int ir)
        {
            //The following sources were used to determine the calculation for the perfusion index:
            //1. https://www.ncbi.nlm.nih.gov/pmc/articles/PMC3417976/
            //2. https://www.analog.com/en/technical-articles/guidelines-for-spo2-measurement--maxim-integrated.html
            //3. https://dsp.stackexchange.com/questions/46615/calculate-spo%E2%82%82-value-from-raw-fingertip-plethysmography-ppg
            //4. https://www.ti.com/lit/an/slaa655/slaa655.pdf

            DateTime now_datetime = DateTime.Now;
            _recent_ir_values.Add(ir);
            _recent_ir_value_datetimes.Add(now_datetime);

            int last_index_to_remove = _recent_ir_value_datetimes.FindLastIndex(0, x => x < (now_datetime - TimeSpan.FromSeconds(5)));
            if (last_index_to_remove > -1)
            {
                _recent_ir_value_datetimes.RemoveRange(0, last_index_to_remove + 1);
                _recent_ir_values.RemoveRange(0, last_index_to_remove + 1);
            }

            if (now_datetime >= (_last_perfusion_index_update_time + _perfusion_index_update_period))
            {
                _last_perfusion_index_update_time = DateTime.Now;
                var dc_component = _recent_ir_values.Average();
                var ac_component = _recent_ir_values.Max() - _recent_ir_values.Min();
                var pi = (ac_component / dc_component) * 100.0;
                PerfusionIndex = pi;
            }
        }

        private void BackgroundThread_HandleAudio ()
        {
            //If we are not currently playing a sound
            if (_audio_player.PlaybackState != PlaybackState.Playing)
            {
                //Check if heart rate is currently 0
                if (HeartRate == 0)
                {
                    if (DateTime.Now >= (_last_no_pulse_time + _no_pulse_period))
                    {
                        //Play the "no pulse found" sound
                        _audio_player.Init(BackgroundThread_GenerateNoPulseSound());
                        _audio_player.Play();

                        _last_no_pulse_time = DateTime.Now;
                    }
                }
                else
                {
                    //If there is a heart rate...

                    //Determine whether to play the alarm
                    bool should_alarm = (HeartRate < _alarm_hr_min || HeartRate > _alarm_hr_max || SpO2 < _alarm_spo2_min || SpO2 > _alarm_spo2_max);
                    should_alarm &= (DateTime.Now >= (_last_alarm_time + _alarm_period));

                    if (should_alarm)
                    {
                        //If we have determined that we should play an alarm...

                        //Determine whether to play a single or a double
                        Random rnd = new Random();
                        int rnd_num = rnd.Next(0, 2);
                        bool generate_double = rnd_num > 0;

                        //Play the alarm audio
                        _audio_player.Init(BackgroundThread_GenerateAlarmSound(generate_double));
                        _audio_player.Play();

                        //Set the "last alarm time" variable
                        _last_alarm_time = DateTime.Now;
                    }
                    else
                    {
                        //Otherwise, let's play the normal heart rate audio

                        //Determine the pitch of the tone
                        int pitch;
                        if (_spo2_pitch_mapping.ContainsKey(SpO2))
                        {
                            pitch = _spo2_pitch_mapping[SpO2];
                        }
                        else
                        {
                            pitch = _spo2_pitch_mapping[80];
                        }

                        //Determine how fast the tone will play
                        var total_duration = 60_000 / HeartRate;
                        var silence_duration = total_duration - 50;

                        //Generate the tone and the silence that follows it
                        var beep = (new SignalGenerator() { Frequency = pitch, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(50));
                        var silence = new SilenceProvider(beep.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(silence_duration));
                        var generated_sound = beep.FollowedBy(silence);
                        _audio_player.Init(generated_sound);
                        _audio_player.Play();
                    }
                }
            }
        }

        private ISampleProvider BackgroundThread_GenerateNoPulseSound ()
        {
            var beep1 = (new SignalGenerator() { Frequency = 200, Gain = 0.75 }).Take(TimeSpan.FromMilliseconds(400));
            var silence1 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(500));
            var beep2 = (new SignalGenerator() { Frequency = 200, Gain = 0.75 }).Take(TimeSpan.FromMilliseconds(400));
            var silence2 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(500));

            var no_pulse_sound = beep1
                .FollowedBy(silence1)
                .FollowedBy(beep2)
                .FollowedBy(silence2);

            return no_pulse_sound;
        }

        private ISampleProvider BackgroundThread_GenerateAlarmSound (bool generate_double)
        {
            if (!generate_double)
            {
                var beep1 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence1 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep2 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence2 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep3 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence3 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(350));
                var beep4 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence4 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep5 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence5 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));

                var alarm_sound = beep1
                    .FollowedBy(silence1)
                    .FollowedBy(beep2)
                    .FollowedBy(silence2)
                    .FollowedBy(beep3)
                    .FollowedBy(silence3)
                    .FollowedBy(beep4)
                    .FollowedBy(silence4)
                    .FollowedBy(beep5)
                    .FollowedBy(silence5);

                return alarm_sound;
            }
            else
            {
                var beep1 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence1 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep2 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence2 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep3 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence3 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(350));
                var beep4 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence4 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep5 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));

                var silence5 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(350));

                var beep6 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence6 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep7 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence7 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep8 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence8 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(350));
                var beep9 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence9 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));
                var beep10 = (new SignalGenerator() { Frequency = 440, Gain = 0.2 }).Take(TimeSpan.FromMilliseconds(175));
                var silence10 = new SilenceProvider(beep1.WaveFormat).ToSampleProvider().Take(TimeSpan.FromMilliseconds(80));

                var alarm_sound = beep1
                    .FollowedBy(silence1)
                    .FollowedBy(beep2)
                    .FollowedBy(silence2)
                    .FollowedBy(beep3)
                    .FollowedBy(silence3)
                    .FollowedBy(beep4)
                    .FollowedBy(silence4)
                    .FollowedBy(beep5)
                    .FollowedBy(silence5)
                    .FollowedBy(beep6)
                    .FollowedBy(silence6)
                    .FollowedBy(beep7)
                    .FollowedBy(silence7)
                    .FollowedBy(beep8)
                    .FollowedBy(silence8)
                    .FollowedBy(beep9)
                    .FollowedBy(silence9)
                    .FollowedBy(beep10)
                    .FollowedBy(silence10);

                return alarm_sound;
            }
        }

        private string BackgroundThread_FindConnectedDevice ()
        {
            //Query all connected devices
            var searcher = new ManagementObjectSearcher(@"SELECT * FROM WIN32_SerialPort");
            var collection = searcher.Get();

            //Iterate over each connected device
            foreach (var device in collection)
            {
                try
                {
                    //Get the VID/PID and the com port name of the device
                    string pnp_device_id = (string)device.GetPropertyValue("PNPDeviceID");
                    string com_port = (string)device.GetPropertyValue("DeviceID");

                    //Parse out the VID/PID
                    if (!string.IsNullOrWhiteSpace(pnp_device_id) && !string.IsNullOrWhiteSpace(com_port))
                    {
                        int vid_idx = pnp_device_id.IndexOf("VID_");
                        int pid_idx = pnp_device_id.IndexOf("PID_");
                        if (vid_idx >= 0 && pid_idx >= 0)
                        {
                            string vid = pnp_device_id.Substring(vid_idx + 4, 4);
                            string pid = pnp_device_id.Substring(pid_idx + 4, 4);
                            if (vid == rewire_pulse_oximeter_vid_string && pid == rewire_pulse_oximeter_pid_string)
                            {
                                return com_port;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //empty
                }
            }

            //Dispose of the collection of queried devices
            collection.Dispose();

            //Return the list of devices that were found
            //return devices;
            return string.Empty;
        }

        private void BackgroundThread_NotifyPropertyChanged (string property_name)
        {
            _background_thread.ReportProgress(1, property_name);
        }

        #endregion

        #region Properties

        public int IR
        {
            get
            {
                return _ir;
            }
            private set
            {
                _ir = value;
                BackgroundThread_NotifyPropertyChanged(nameof(IR));
            }
        }

        public int HeartRate
        {
            get
            {
                return _heart_rate;
            }
            private set
            {
                _heart_rate = value;
                BackgroundThread_NotifyPropertyChanged(nameof(HeartRate));
            }
        }

        public int SpO2
        {
            get
            {
                return _spo2;
            }
            private set
            {
                _spo2 = value;
                BackgroundThread_NotifyPropertyChanged(nameof(SpO2));
            }
        }

        public double PerfusionIndex
        {
            get
            {
                return _perfusion_index;
            }
            private set
            {
                _perfusion_index = value;
                BackgroundThread_NotifyPropertyChanged(nameof(PerfusionIndex));
            }
        }

        public int HeartRateAlarm_Minimum
        {
            get
            {
                return _alarm_hr_min;
            }
            set
            {
                if (value != _alarm_hr_min)
                {
                    _alarm_hr_min = value;
                    _application_configuration.HeartRateAlarmMinimum = value;
                }
            }
        }

        public int HeartRateAlarm_Maximum
        {
            get
            {
                return _alarm_hr_max;
            }
            set
            {
                if (value != _alarm_hr_max)
                {
                    _alarm_hr_max = value;
                    _application_configuration.HeartRateAlarmMaximum = value;
                }
            }
        }

        public int SpO2Alarm_Minimum
        {
            get
            {
                return _alarm_spo2_min;
            }
            set
            {
                if (value != _alarm_spo2_min)
                {
                    _alarm_spo2_min = value;
                    _application_configuration.SpO2AlarmMinimum = value;
                }
            }
        }

        public int SpO2Alarm_Maximum
        {
            get
            {
                return _alarm_spo2_max;
            }
            set
            {
                if (value != _alarm_spo2_max)
                {
                    _alarm_spo2_max = value;
                    _application_configuration.SpO2AlarmMaximum = value;
                }
            }
        }

        public DeviceConnectionState ConnectionState
        {
            get
            {
                return _device_connection_state;
            }
            private set
            {
                _device_connection_state = value;
                BackgroundThread_NotifyPropertyChanged(nameof(ConnectionState));
            }
        }

        public bool MuteAudio
        {
            get
            {
                return _mute_audio;
            }
            set
            {
                _mute_audio = value;
                NotifyPropertyChanged(nameof(MuteAudio));
            }
        }

        public bool IsRecording
        {
            get
            {
                return _recording;
            }
        }

        public DateTime RecordingStartTime
        {
            get
            {
                return _recording_start_time;
            }
        }

        #endregion

        #region Methods

        public void Start ()
        {
            _background_thread.RunWorkerAsync();
        }

        public void StartRecording (string filename)
        {
            if (!_recording)
            {
                _recording_file = filename;
                _recording = true;
                NotifyPropertyChanged(nameof(IsRecording));
            }
        }

        public void StopRecording ()
        {
            if (_recording)
            {
                _recording = false;
                NotifyPropertyChanged(nameof(IsRecording));
            }
        }

        #endregion
    }
}
