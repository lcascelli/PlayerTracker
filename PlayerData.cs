using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayerTracker.Models
{
    public class Player : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Core properties
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _position = string.Empty;
        public string Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _nationality = string.Empty;
        public string Nationality
        {
            get => _nationality;
            set
            {
                if (_nationality != value)
                {
                    _nationality = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _promoted;
        public bool Promoted
        {
            get => _promoted;
            set
            {
                if (_promoted != value)
                {
                    _promoted = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _released;
        public bool Released
        {
            get => _released;
            set
            {
                if (_released != value)
                {
                    _released = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<PlayerStatVersion> History { get; set; } = new List<PlayerStatVersion>();
        private PlayerStatVersion? LatestStats => History.Count > 0 ? History[0] : null;

        // Stat properties
        private int _age;
        public int Age
        {
            get => LatestStats?.Age ?? _age;
            set => UpdateStat(nameof(Age), value);
        }

        private int _overall;
        public int Overall
        {
            get => LatestStats?.Overall ?? _overall;
            set => UpdateStat(nameof(Overall), value);
        }

        private int _minPotential;
        public int MinPotential
        {
            get => LatestStats?.MinPotential ?? _minPotential;
            set => UpdateStat(nameof(MinPotential), value);
        }

        private int _maxPotential;
        public int MaxPotential
        {
            get => LatestStats?.MaxPotential ?? _maxPotential;
            set => UpdateStat(nameof(MaxPotential), value);
        }

        private void UpdateStat(string propertyName, int newValue)
        {
            var latest = LatestStats;

            if (latest == null)
            {
                // First version
                var newVersion = new PlayerStatVersion
                {
                    Timestamp = DateTime.Now,
                    Age = propertyName == nameof(Age) ? newValue : 0,
                    Overall = propertyName == nameof(Overall) ? newValue : 0,
                    MinPotential = propertyName == nameof(MinPotential) ? newValue : 0,
                    MaxPotential = propertyName == nameof(MaxPotential) ? newValue : 0
                };
                History.Insert(0, newVersion);
                OnPropertyChanged(propertyName);
                return;
            }

            // Check if value changed
            bool changed = false;
            switch (propertyName)
            {
                case nameof(Age):
                    changed = latest.Age != newValue;
                    break;
                case nameof(Overall):
                    changed = latest.Overall != newValue;
                    break;
                case nameof(MinPotential):
                    changed = latest.MinPotential != newValue;
                    break;
                case nameof(MaxPotential):
                    changed = latest.MaxPotential != newValue;
                    break;
            }

            if (!changed)
                return;

            var newVersionCopy = new PlayerStatVersion
            {
                Timestamp = DateTime.Now,
                Age = propertyName == nameof(Age) ? newValue : latest.Age ?? 0,
                Overall = propertyName == nameof(Overall) ? newValue : latest.Overall,
                MinPotential = propertyName == nameof(MinPotential) ? newValue : latest.MinPotential,
                MaxPotential = propertyName == nameof(MaxPotential) ? newValue : latest.MaxPotential
            };

            History.Insert(0, newVersionCopy);
            OnPropertyChanged(propertyName);
        }
    }

    public class PlayerStatVersion
    {
        public DateTime Timestamp { get; set; }
        public int? Age { get; set; }
        public int MinPotential { get; set; }
        public int MaxPotential { get; set; }
        public int Overall { get; set; }
    }
}
