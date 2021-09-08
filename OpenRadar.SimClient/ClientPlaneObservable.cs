using System.ComponentModel;
using System.Runtime.CompilerServices;
using FreeRadar.Common;
using JetBrains.Annotations;

namespace OpenRadar.SimClient
{
    /// <summary>
    /// An observable managed version of <see cref="ClientPlaneData"/>,
    /// for use in data binding. Represents data on a single simulated aircraft.
    /// </summary>
    public sealed class ClientPlaneObservable : INotifyPropertyChanged
    {
        private string _flightNum = string.Empty;
        private double _latitude;
        private double _longitude;
        private double _altitude;
        private int    _comFreq;
        private int    _squawk;
        private double _groundTrack;
        private double _groundSpeed;

        /// <summary>
        /// The ATC flight number of the aircraft
        /// </summary>
        public string FlightNum {
            get => _flightNum;
            set {
                if (value == _flightNum) return;
                _flightNum = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The latitude of the aircraft in decimal degrees. The setter for this properties also invokes <seealso cref="PropertyChanged"/> for the Coordinates computed property.
        /// </summary>
        public double Latitude {
            get => _latitude;
            set {
                if (value.Equals(_latitude)) return;
                _latitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Coordinates));
            }
        }

        /// <summary>
        /// The longitude of the aircraft in decimal degrees. The setter for this properties also invokes <seealso cref="PropertyChanged"/> for the Coordinates computed property.
        /// </summary>
        public double Longitude {
            get => _longitude;
            set {
                if (value.Equals(_longitude)) return;
                _longitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Coordinates));
            }
        }

        /// <summary>
        /// The true altitude of the aircraft, in US customary feet.
        /// </summary>
        public double Altitude {
            get => _altitude;
            set {
                if (value.Equals(_altitude)) return;
                _altitude = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The primary radio communications frequency of the aircraft, in KHz instead of the usual MHz to avoid floating point representation inaccuracy
        /// </summary>
        public int ComFreq {
            get => _comFreq;
            set {
                if (value == _comFreq) return;
                _comFreq = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The transponder squawk code of the aircraft, a four digit binary-coded octal number.
        /// Each octal digit of the squawk is stored in the lower nibble of the corresponding (little-endian) byte,
        /// so for an aircraft squawking 1234, <c>Squawk == 0x1234</c>. Format specifier <c>X4</c> results in correct
        /// display of this encoding with no conversion needed. 
        /// </summary>
        public int Squawk {
            get => _squawk;
            set {
                if (value == _squawk) return;
                _squawk = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The current track of the aircraft over the ground, in true angular degrees. Compensation for magnetic variation is necessary to obtain magnetic ground track.
        /// </summary>
        public double GroundTrack {
            get => _groundTrack;
            set {
                if (value.Equals(_groundTrack)) return;
                _groundTrack = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The current speed of the aircraft over the ground, in knots (nautical miles per hour)
        /// </summary>
        public double GroundSpeed {
            get => _groundSpeed;
            set {
                if (value.Equals(_groundSpeed)) return;
                _groundSpeed = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Allows setting all properties of this object from an unmanaged <seealso cref="ClientPlaneData"/> struct
        /// returned by the SimConnect library
        /// </summary>
        public ClientPlaneData PlaneData {
            set {
                FlightNum = value.FlightNum;
                Latitude = value.Latitude;
                Longitude = value.Longitude;
                Altitude = value.Altitude;
                ComFreq = value.ComFrequency;
                Squawk = value.TransponderSquawk & 0xFFFF;
                GroundTrack = value.GroundTrack;
                GroundSpeed = value.GroundSpeed;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The current coordinates of the aircraft on the earth's surface, as a managed latitude-longitude pair.
        /// see <see cref="Latitude"/>, <see cref="Longitude"/>.
        /// </summary>
        public LatLng Coordinates => new(Latitude, Longitude);

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null!) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}