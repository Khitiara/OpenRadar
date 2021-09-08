using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FreeRadar.Common;
using JetBrains.Annotations;

namespace OpenRadar.SimClient
{
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

        public string FlightNum {
            get => _flightNum;
            set {
                if (value == _flightNum) return;
                _flightNum = value;
                OnPropertyChanged();
            }
        }

        public double Latitude {
            get => _latitude;
            set {
                if (value.Equals(_latitude)) return;
                _latitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Coordinates));
            }
        }

        public double Longitude {
            get => _longitude;
            set {
                if (value.Equals(_longitude)) return;
                _longitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Coordinates));
            }
        }

        public double Altitude {
            get => _altitude;
            set {
                if (value.Equals(_altitude)) return;
                _altitude = value;
                OnPropertyChanged();
            }
        }

        public int ComFreq {
            get => _comFreq;
            set {
                if (value == _comFreq) return;
                _comFreq = value;
                OnPropertyChanged();
            }
        }

        public int Squawk {
            get => _squawk;
            set {
                if (value == _squawk) return;
                _squawk = value;
                OnPropertyChanged();
            }
        }

        public double GroundTrack {
            get => _groundTrack;
            set {
                if (value.Equals(_groundTrack)) return;
                _groundTrack = value;
                OnPropertyChanged();
            }
        }

        public double GroundSpeed {
            get => _groundSpeed;
            set {
                if (value.Equals(_groundSpeed)) return;
                _groundSpeed = value;
                OnPropertyChanged();
            }
        }

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

        public LatLng Coordinates => new(Latitude, Longitude);

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null!) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}