using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;

namespace SystemMonitor
{
    class SystemMonitor
    {
        private static UpdateVisitor updateVisitor;
        private static Computer computer;
        private static SensorType sensor;
        static void Main(string[] args)
        {
            #if DEBUG
            Console.WriteLine("Debug version");
            #endif

            updateVisitor = new UpdateVisitor();
            computer = new Computer();
            computer.Open();
            switch (args[0].ToLower())
            {
                case "/d":
                    GetDriveInfo();
                    break;
                case "/c":
                    computer.CPUEnabled = true;
                    computer.Accept(updateVisitor);
                    sensor = SensorType.Temperature;
                    GetCPUTemperature();
                    break;
                case "/g":
                    computer.GPUEnabled = true;
                    computer.Accept(updateVisitor);
                    sensor = SensorType.Temperature;
                    GetGPUTemperature();
                    break;
                case "/m":
                    computer.MainboardEnabled = true;
                    computer.Accept(updateVisitor);
                    sensor = SensorType.Temperature;
                    GetMotherBoardTemperature();
                    break;
                case "/gl":
                    computer.GPUEnabled = true;
                    computer.Accept(updateVisitor);
                    sensor = SensorType.Load;
                    GetGPULoad();
                    break;
                case "/a":
                    computer.MainboardEnabled = true;
                    computer.HDDEnabled = true;
                    computer.RAMEnabled = true;
                    computer.GPUEnabled = true;
                    computer.FanControllerEnabled = true;
                    computer.Accept(updateVisitor);
                    GetAll();
                    break;
                default:
                    computer.MainboardEnabled = true;
                    computer.HDDEnabled = true;
                    computer.RAMEnabled = true;
                    computer.GPUEnabled = true;
                    computer.FanControllerEnabled = true;
                    computer.Accept(updateVisitor);
                    GetAll();
                    break;
            }
            computer.Close();
            #if DEBUG
            Console.ReadKey();
            #endif
        }

        static void GetGPUTemperature()
        {
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.GpuAti || computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                            Console.WriteLine(computer.Hardware[i].Sensors[j].Value);
                    }
                }
            }
        }
        
        static void GetData()
        {
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                {
                    if (computer.Hardware[i].Sensors[j].SensorType == sensor)
                        Console.WriteLine(computer.Hardware[i].Sensors[j].Value);
                }

                foreach (IHardware hardware in computer.Hardware[i].SubHardware)
                {
                    foreach (ISensor hardwareSensor in hardware.Sensors)
                    {
                        Console.WriteLine(hardwareSensor.Value);
                    }
                }
            }
        }
        
        static void GetAll()
        {
            foreach (IHardware hardware in computer.Hardware)
            {
                foreach (ISensor sensor in hardware.Sensors)
                {
                    Console.WriteLine(hardware.Name + "," + sensor.Name + ":" + sensor.Value);
                }
                foreach (IHardware subHardware in hardware.SubHardware)
                {
                    foreach (ISensor hardwareSensor in subHardware.Sensors)
                    {
                        Console.WriteLine(hardware.Name + "," + hardwareSensor.Name + ":" + hardwareSensor.Value);
                    }
                }
            }
        }

        static void GetMotherBoardTemperature()
        {
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                {
                    if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        Console.WriteLine(computer.Hardware[i].Sensors[j].Name + ":" + computer.Hardware[i].Sensors[j].Value);
                }

                foreach (IHardware hardware in computer.Hardware[i].SubHardware)
                {
                    foreach (ISensor hardwareSensor in hardware.Sensors)
                    {
                        Console.WriteLine(hardware.Name + "," + hardwareSensor.Name + ":" + hardwareSensor.Value);
                    }
                }
            }
        }

        static void GetCPUTemperature()
        {
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature && computer.Hardware[i].Sensors[j].Name.ToLower().Contains("package"))
                            Console.WriteLine(computer.Hardware[i].Sensors[j].Value);
                    }
                }
            }
            computer.Close();
        }

        
        static void GetGPULoad()
        {
            foreach (IHardware hardware in computer.Hardware)
            {
                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load && sensor.Name.ToLower().Contains("core"))
                        Console.WriteLine(sensor.Value);
                }
                foreach (IHardware subHardware in hardware.SubHardware)
                {
                    foreach (ISensor hardwareSensor in subHardware.Sensors)
                    {
                        if (hardwareSensor.SensorType == SensorType.Load && hardwareSensor.Name.ToLower().Contains("core"))
                            Console.WriteLine(hardwareSensor.Value);
                    }
                }
            }
            computer.Close();
        }
        
        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        static void GetDriveInfo()
        {
            var dicDrives = SmCore.GetSmartDATA();
            foreach (var drive in dicDrives)
            {
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine(" DRIVE ({0}): " + drive.Value.Serial + " - " + drive.Value.Model + " - " + drive.Value.Type, ((drive.Value.IsOK) ? "OK" : "BAD"));
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine("");
 
                Console.WriteLine("ID                   Current  Worst  Threshold  Data  Status");
                foreach (var attr in drive.Value.Attributes)
                {
                    if (attr.Value.HasData)
                        Console.WriteLine("{0}\t {1}\t {2}\t {3}\t " + attr.Value.Data + " " + ((attr.Value.IsOK) ? "OK" : "BAD"), attr.Value.Attribute, attr.Value.Current, attr.Value.Worst, attr.Value.Threshold);
                }
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
    public static class SmCore
    {
        public static Dictionary<int, HDD> GetSmartDATA()
        {
            // retrieve list of drives on computer (this will return both HDD's and CDROM's and Virtual CDROM's)                    
            var dicDrives = new Dictionary<int, HDD>();
            var wdSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            // extract model and interface information
            int iDriveIndex = 0;
            foreach (ManagementObject drive in wdSearcher.Get())
            {
                var hdd = new HDD();
                hdd.Model = drive["Model"].ToString().Trim();
                hdd.Type = drive["InterfaceType"].ToString().Trim();
                dicDrives.Add(iDriveIndex, hdd);
                iDriveIndex++;
            }
 
            var pmsearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
 
            // retrieve hdd serial number
            iDriveIndex = 0;
            foreach (ManagementObject drive in pmsearcher.Get())
            {
                // because all physical media will be returned we need to exit
                // after the hard drives serial info is extracted
                if (iDriveIndex >= dicDrives.Count)
                    break;
 
                dicDrives[iDriveIndex].Serial = drive["SerialNumber"] == null ? "None" : drive["SerialNumber"].ToString().Trim();
                iDriveIndex++;
            }
 
            // get wmi access to hdd 
            var searcher = new ManagementObjectSearcher("Select * from Win32_DiskDrive");
            searcher.Scope = new ManagementScope(@"\root\wmi");

            try
            {
                // check if SMART reports the drive is failing
                searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictStatus");
                iDriveIndex = 0;
                foreach (ManagementObject drive in searcher.Get())
                {
                    dicDrives[iDriveIndex].IsOK = (bool)drive.Properties["PredictFailure"].Value == false;
                    iDriveIndex++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
 
            // retrive attribute flags, value worste and vendor data information
            searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictData");
            iDriveIndex = 0;
            foreach (ManagementObject data in searcher.Get())
            {
                Byte[] bytes = (Byte[])data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 30; ++i)
                {
                    try
                    {
                        int id = bytes[i * 12 + 2];
 
                        int flags = bytes[i * 12 + 4]; // least significant status byte, +3 most significant byte, but not used so ignored.
                        //bool advisory = (flags & 0x1) == 0x0;
                        bool failureImminent = (flags & 0x1) == 0x1;
                        //bool onlineDataCollection = (flags & 0x2) == 0x2;
 
                        int value = bytes[i * 12 + 5];
                        int worst = bytes[i * 12 + 6];
                        int vendordata = BitConverter.ToInt32(bytes, i * 12 + 7);
                        if (id == 0) continue;
 
                        var attr = dicDrives[iDriveIndex].Attributes[id];
                        attr.Current = value;
                        attr.Worst = worst;
                        attr.Data = vendordata;
                        attr.IsOK = failureImminent == false;
                    }
                    catch
                    {
                        // given key does not exist in attribute collection (attribute not in the dictionary of attributes)
                    }
                }
                iDriveIndex++;
            }
 
            // retreive threshold values foreach attribute
            searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictThresholds");
            iDriveIndex = 0;
            foreach (ManagementObject data in searcher.Get())
            {
                Byte[] bytes = (Byte[])data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 30; ++i)
                {
                    try
                    {
 
                        int id = bytes[i * 12 + 2];
                        int thresh = bytes[i * 12 + 3];
                        if (id == 0) continue;
 
                        var attr = dicDrives[iDriveIndex].Attributes[id];
                        attr.Threshold = thresh;
                    }
                    catch
                    {
                        // given key does not exist in attribute collection (attribute not in the dictionary of attributes)
                    }
                }
 
                iDriveIndex++;
            }

            return dicDrives;
        }

        public static void GetMonitors()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_DesktopMonitor");

            foreach (ManagementObject obj in searcher.Get())
            {
                Console.WriteLine(obj["ScreenWidth"].ToString());
                Console.WriteLine(obj["ScreenHeight"].ToString());
                Console.WriteLine(obj["Manufacturer"].ToString());
                Console.WriteLine(obj["DeviceID"].ToString());
            }
        }

        public static List<string> GetComDevices()
        {
            List<string> output = new List<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSSerial_PortName");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                Console.Write("InstanceName: {0}", queryObj["InstanceName"]);
                Console.WriteLine("PortName: {0}", queryObj["PortName"]);
                output.Add(queryObj["PortName"].ToString());
                //If the serial port's instance name contains USB 
                //it must be a USB to serial device
                if (queryObj["InstanceName"].ToString().Contains("USB"))
                {
                    Console.WriteLine(queryObj["PortName"] + "is a USB to SERIAL adapter/converter");
                }
                // do what you like with the Win32_PnpEntity
            }
            return output;
        }
        
        public static List<string> GetVideoCard()
        {
            List<string> output = new List<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            string graphicsCard = string.Empty;
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentBitsPerPixel"] != null && obj["CurrentHorizontalResolution"] != null)
                {
                    output.Add(graphicsCard = obj["Name"].ToString());
                }
            }
            return output;
        }

        
        public static object GetCPUCounter()
        {

            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            // will always start at 0
            dynamic firstValue = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            // now matches task manager reading
            dynamic secondValue = cpuCounter.NextValue();

            return secondValue;
        }

        public static List<PerformanceCounter> GetGPUCounter()
        {
            var list = new List<PerformanceCounter> ();
            var category = new PerformanceCounterCategory ("GPU Engine");
            var names = category.GetInstanceNames ();
            foreach (var name in names)
                list.AddRange (category.GetCounters (name));
            return list;
        }
    }

    public class HDD
    {
     
        public int Index { get; set; }
        public bool IsOK { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Serial { get; set; }
        public Dictionary<int, Smart> Attributes = new Dictionary<int, Smart>
        {
            {0x00, new Smart("Invalid")},
            {0x01, new Smart("Raw read error rate")},
            {0x02, new Smart("Throughput performance")},
            {0x03, new Smart("Spinup time")},
            {0x04, new Smart("Start/Stop count")},
            {0x05, new Smart("Reallocated sector count")},
            {0x06, new Smart("Read channel margin")},
            {0x07, new Smart("Seek error rate")},
            {0x08, new Smart("Seek timer performance")},
            {0x09, new Smart("Power-on hours count")},
            {0x0A, new Smart("Spinup retry count")},
            {0x0B, new Smart("Calibration retry count")},
            {0x0C, new Smart("Power cycle count")},
            {0x0D, new Smart("Soft read error rate")},
            {0xB8, new Smart("End-to-End error")},
            {0xBE, new Smart("Airflow Temperature")},
            {0xBF, new Smart("G-sense error rate")},
            {0xC0, new Smart("Power-off retract count")},
            {0xC1, new Smart("Load/Unload cycle count")},
            {0xC2, new Smart("HDD temperature")},
            {0xC3, new Smart("Hardware ECC recovered")},
            {0xC4, new Smart("Reallocation count")},
            {0xC5, new Smart("Current pending sector count")},
            {0xC6, new Smart("Offline scan uncorrectable count")},
            {0xC7, new Smart("UDMA CRC error rate")},
            {0xC8, new Smart("Write error rate")},
            {0xC9, new Smart("Soft read error rate")},
            {0xCA, new Smart("Data Address Mark errors")},
            {0xCB, new Smart("Run out cancel")},
            {0xCC, new Smart("Soft ECC correction")},
            {0xCD, new Smart("Thermal asperity rate (TAR)")},
            {0xCE, new Smart("Flying height")},
            {0xCF, new Smart("Spin high current")},
            {0xD0, new Smart("Spin buzz")},
            {0xD1, new Smart("Offline seek performance")},
            {0xDC, new Smart("Disk shift")},
            {0xDD, new Smart("G-sense error rate")},
            {0xDE, new Smart("Loaded hours")},
            {0xDF, new Smart("Load/unload retry count")},
            {0xE0, new Smart("Load friction")},
            {0xE1, new Smart("Load/Unload cycle count")},
            {0xE2, new Smart("Load-in time")},
            {0xE3, new Smart("Torque amplification count")},
            {0xE4, new Smart("Power-off retract count")},
            {0xE6, new Smart("GMR head amplitude")},
            {0xE7, new Smart("Temperature")},
            {0xF0, new Smart("Head flying hours")},
            {0xFA, new Smart("Read error retry rate")},
            /* slot in any new codes you find in here */
        };
     
    }
 
    public class Smart
    {
        public bool HasData
        {
            get
            {
                if (Current == 0 && Worst == 0 && Threshold == 0 && Data == 0)
                    return false;
                return true;
            }
        }
        public string Attribute { get; set; }
        public int Current { get; set; }
        public int Worst { get; set; }
        public int Threshold { get; set; }
        public int Data { get; set; }
        public bool IsOK { get; set; }
     
        public Smart()
        {
     
        }
     
        public Smart(string attributeName)
        {
            this.Attribute = attributeName;
        }
    }
}
