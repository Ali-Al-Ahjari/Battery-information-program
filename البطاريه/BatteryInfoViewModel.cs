using System;
using System.ComponentModel;
using System.Management;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace البطاريه
{
    public class BatteryInfoViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;

        public BatteryInfoViewModel()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, e) => RefreshData();
            _timer.Start();

            RefreshData();
        }

        private void RefreshData()
        {
            var powerStatus = SystemInformation.PowerStatus;

            int newPercent = (int)(powerStatus.BatteryLifePercent * 100);
            BatteryPercentage = newPercent;
            
            // Arabic status text (BatteryChargeStatus is a flags enum)
            if (powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery))
            {
                BatteryStatus = "لا توجد بطارية";
            }
            else if (powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging))
            {
                BatteryStatus = "قيد الشحن";
            }
            else if (powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Critical))
            {
                BatteryStatus = "حرج";
            }
            else if (powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Low))
            {
                BatteryStatus = "منخفض";
            }
            else if (powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.High))
            {
                BatteryStatus = "مرتفع";
            }
            else
            {
                BatteryStatus = powerStatus.PowerLineStatus == PowerLineStatus.Online ? "متصل بالكهرباء" : "يعمل على البطارية";
            }

            // Arabic time remaining
            if (powerStatus.BatteryLifeRemaining > 0)
            {
                TimeSpan time = TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining);
                EstimatedTimeRemaining = $"{time.Hours} ساعة و {time.Minutes} دقيقة متبقية";
            }
            else
            {
                EstimatedTimeRemaining = powerStatus.PowerLineStatus == PowerLineStatus.Online ? "جاري الشحن..." : "جاري الحساب...";
            }

            // Power source
            PowerSource = powerStatus.PowerLineStatus == PowerLineStatus.Online ? "التيار الكهربائي" : "البطارية";

            // Charging status
            IsCharging = powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging);
            ChargingStatus = IsCharging ? "نعم" : "لا";

            if (string.IsNullOrEmpty(DesignCapacity))
            {
                GetWmiData();
            }
        }

        private void GetWmiData()
        {
            try
            {
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Battery");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection collection = searcher.Get();

                foreach (ManagementObject mo in collection)
                {
                    if (mo["Name"] != null) BatteryName = mo["Name"].ToString();
                    if (mo["DesignCapacity"] != null) DesignCapacity = mo["DesignCapacity"].ToString() + " ميلي واط/ساعة";
                    if (mo["FullChargeCapacity"] != null) FullChargeCapacity = mo["FullChargeCapacity"].ToString() + " ميلي واط/ساعة";
                    if (mo["EstimatedChargeRemaining"] != null) EstimatedChargeRemaining = mo["EstimatedChargeRemaining"].ToString() + "%";
                    if (mo["DeviceID"] != null) DeviceID = mo["DeviceID"].ToString();
                    if (mo["Chemistry"] != null)
                    {
                        int chemistry = Convert.ToInt32(mo["Chemistry"]);
                        switch (chemistry)
                        {
                            case 1: BatteryChemistry = "أخرى"; break;
                            case 2: BatteryChemistry = "غير معروف"; break;
                            case 3: BatteryChemistry = "رصاص حمضي"; break;
                            case 4: BatteryChemistry = "نيكل كادميوم"; break;
                            case 5: BatteryChemistry = "نيكل هيدريد معدني"; break;
                            case 6: BatteryChemistry = "ليثيوم أيون"; break;
                            case 7: BatteryChemistry = "زنك هواء"; break;
                            case 8: BatteryChemistry = "ليثيوم بوليمر"; break;
                            default: BatteryChemistry = "غير معروف"; break;
                        }
                    }
                    if (mo["BatteryStatus"] != null)
                    {
                        int status = Convert.ToInt32(mo["BatteryStatus"]);
                        switch (status)
                        {
                            case 1: BatteryHealth = "يفرغ"; break;
                            case 2: BatteryHealth = "متصل بالتيار"; break;
                            case 3: BatteryHealth = "مشحون بالكامل"; break;
                            case 4: BatteryHealth = "منخفض"; break;
                            case 5: BatteryHealth = "حرج"; break;
                            case 6: BatteryHealth = "قيد الشحن"; break;
                            case 7: BatteryHealth = "شحن مرتفع"; break;
                            case 8: BatteryHealth = "شحن منخفض"; break;
                            case 9: BatteryHealth = "شحن حرج"; break;
                            default: BatteryHealth = "غير معروف"; break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(DesignCapacity)) DesignCapacity = "غير متوفر";
                if (string.IsNullOrEmpty(FullChargeCapacity)) FullChargeCapacity = "غير متوفر";
                if (string.IsNullOrEmpty(BatteryName)) BatteryName = "بطارية غير معروفة";
                if (string.IsNullOrEmpty(BatteryChemistry)) BatteryChemistry = "غير معروف";
                if (string.IsNullOrEmpty(BatteryHealth)) BatteryHealth = "غير معروف";
            }
            catch (Exception)
            {
                BatteryName = "غير معروف";
                DesignCapacity = "غير متوفر";
                FullChargeCapacity = "غير متوفر";
                BatteryChemistry = "غير معروف";
                BatteryHealth = "غير معروف";
            }
        }

        private int _batteryPercentage;
        public int BatteryPercentage
        {
            get { return _batteryPercentage; }
            set { if (_batteryPercentage != value) { _batteryPercentage = value; OnPropertyChanged(); } }
        }

        private string _batteryStatus = "جاري التحميل...";
        public string BatteryStatus
        {
            get { return _batteryStatus; }
            set { if (_batteryStatus != value) { _batteryStatus = value; OnPropertyChanged(); } }
        }

        private string _estimatedTimeRemaining = "جاري الحساب...";
        public string EstimatedTimeRemaining
        {
            get { return _estimatedTimeRemaining; }
            set { if (_estimatedTimeRemaining != value) { _estimatedTimeRemaining = value; OnPropertyChanged(); } }
        }

        private string _batteryName = "جاري التحميل...";
        public string BatteryName
        {
            get { return _batteryName; }
            set { if (_batteryName != value) { _batteryName = value; OnPropertyChanged(); } }
        }

        private string _designCapacity = "";
        public string DesignCapacity
        {
            get { return _designCapacity; }
            set { if (_designCapacity != value) { _designCapacity = value; OnPropertyChanged(); } }
        }

        private string _fullChargeCapacity = "";
        public string FullChargeCapacity
        {
            get { return _fullChargeCapacity; }
            set { if (_fullChargeCapacity != value) { _fullChargeCapacity = value; OnPropertyChanged(); } }
        }

        private string _batteryHealth = "";
        public string BatteryHealth
        {
            get { return _batteryHealth; }
            set { if (_batteryHealth != value) { _batteryHealth = value; OnPropertyChanged(); } }
        }

        private string _batteryChemistry = "";
        public string BatteryChemistry
        {
            get { return _batteryChemistry; }
            set { if (_batteryChemistry != value) { _batteryChemistry = value; OnPropertyChanged(); } }
        }

        private string _powerSource = "";
        public string PowerSource
        {
            get { return _powerSource; }
            set { if (_powerSource != value) { _powerSource = value; OnPropertyChanged(); } }
        }

        private bool _isCharging;
        public bool IsCharging
        {
            get { return _isCharging; }
            set { if (_isCharging != value) { _isCharging = value; OnPropertyChanged(); } }
        }

        private string _chargingStatus = "";
        public string ChargingStatus
        {
            get { return _chargingStatus; }
            set { if (_chargingStatus != value) { _chargingStatus = value; OnPropertyChanged(); } }
        }

        private string _estimatedChargeRemaining = "";
        public string EstimatedChargeRemaining
        {
            get { return _estimatedChargeRemaining; }
            set { if (_estimatedChargeRemaining != value) { _estimatedChargeRemaining = value; OnPropertyChanged(); } }
        }

        private string _deviceID = "";
        public string DeviceID
        {
            get { return _deviceID; }
            set { if (_deviceID != value) { _deviceID = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
