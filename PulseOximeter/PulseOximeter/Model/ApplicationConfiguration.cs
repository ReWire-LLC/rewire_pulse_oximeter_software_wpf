﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model
{
    public class ApplicationConfiguration
    {
        #region Private constants

        private const string _application_settings_file_name = "rewire_pulseox.json";
        private const string _company_name = "ReWire";
        private const string _application_name = "PulseOximeter";

        private int _hr_min = 50;
        private int _hr_max = 150;
        private int _spo2_min = 70;
        private int _spo2_max = 100;

        #endregion

        #region Constructor

        public ApplicationConfiguration() 
        {
            LoadSettings();
        }

        #endregion

        #region Private Methods

        public string GetLocalApplicationDataFolder()
        {
            var path_name = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path_name = path_name + @"\" + _company_name + @"\" + _application_name + @"\";

            //Create the path if necessary
            DirectoryInfo dir_info = new DirectoryInfo(path_name);
            if (!dir_info.Exists)
            {
                dir_info.Create();
            }

            //Return the path to the caller
            return path_name;
        }

        private void CreateSettings ()
        {
            //Get the settings file
            var file_name = GetLocalApplicationDataFolder() + _application_settings_file_name;

            //Check if the file doesn't yet exist
            if (!File.Exists(file_name))
            {
                //If the file doesn't exist...

                //Serialize the defaults to JSON
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);

                //Write the defaults to the file
                File.WriteAllText(file_name, json);
            }
        }

        private void LoadSettings ()
        {
            //Create the default file if it does not yet exist
            CreateSettings();

            //Get the settings file
            var file_name = GetLocalApplicationDataFolder() + _application_settings_file_name;

            //Read the file
            if (File.Exists(file_name))
            {
                JObject json_object = JObject.Parse(File.ReadAllText(file_name));
                
                if (json_object.ContainsKey(nameof(HeartRateAlarmMaximum)))
                {
                    this.HeartRateAlarmMaximum = json_object[nameof(HeartRateAlarmMaximum)].ToObject<int>();
                }

                if (json_object.ContainsKey(nameof(HeartRateAlarmMinimum)))
                {
                    this.HeartRateAlarmMinimum = json_object[nameof(HeartRateAlarmMinimum)].ToObject<int>();
                }
                
                if (json_object.ContainsKey(nameof(SpO2AlarmMaximum)))
                {
                    this.SpO2AlarmMaximum = json_object[nameof(SpO2AlarmMaximum)].ToObject<int>();
                }
                
                if (json_object.ContainsKey(nameof(SpO2AlarmMinimum)))
                {
                    this.SpO2AlarmMinimum = json_object[nameof(SpO2AlarmMinimum)].ToObject<int>();
                }
            }
        }

        private void SaveSettings ()
        {
            //Get the settings file
            var file_name = GetLocalApplicationDataFolder() + _application_settings_file_name;

            //Check if the file doesn't yet exist
            if (File.Exists(file_name))
            {
                //Serialize the values to JSON
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);

                //Write the values to the file
                File.WriteAllText(file_name, json);
            }
        }

        #endregion

        #region Properties

        public int HeartRateAlarmMaximum
        {
            get
            {
                return _hr_max;
            }
            set
            {
                _hr_max = value;
                SaveSettings();
            }
        }

        public int HeartRateAlarmMinimum
        {
            get
            {
                return _hr_min;
            }
            set
            {
                _hr_min = value;
                SaveSettings();
            }
        }

        public int SpO2AlarmMaximum
        {
            get
            {
                return _spo2_max;
            }
            set
            {
                _spo2_max = value;
                SaveSettings();
            }
        }

        public int SpO2AlarmMinimum
        {
            get
            {
                return _spo2_min;
            }
            set
            {
                _spo2_min = value;
                SaveSettings();
            }
        }

        #endregion
    }
}
