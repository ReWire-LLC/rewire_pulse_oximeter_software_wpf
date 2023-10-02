using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model
{
    public class DeviceConnectionStateConverter
    {
        /// <summary>
        /// Converts a device connection state to a string description.
        /// </summary>
        /// <param name="connection_state">Device connection state</param>
        /// <returns>The string description of the device connection state</returns>
        public static string ConvertToDescription(DeviceConnectionState connection_state)
        {
            FieldInfo? fi = connection_state.GetType().GetField(connection_state.ToString());
            if (fi != null)
            {
                DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes != null && attributes.Length > 0)
                {
                    return attributes[0].Description;
                }
            }

            return connection_state.ToString();
        }
    }
}
