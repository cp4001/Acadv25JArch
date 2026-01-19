//// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        //NIC 정보 출력 
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            Console.WriteLine($"Name: {nic.Name}");
            Console.WriteLine($"Description: {nic.Description}");
            Console.WriteLine($"Type: {nic.NetworkInterfaceType}");
            Console.WriteLine($"Status: {nic.OperationalStatus}");
            Console.WriteLine($"MAC: {nic.GetPhysicalAddress()}");
            Console.WriteLine("---");
        }



        string uniqueId = GetSystemID();
        Console.WriteLine($"고유 ID: {uniqueId}");
    }

    public static string GetSystemID()
    {
        string macAddress = GetPhysicalMacAddress();
        string cpuId = GetCpuId();
        string combined = macAddress + cpuId;
        return ComputeSha256Hash(combined).Substring(0, 16);
    }

    static string GetPhysicalMacAddress()
    {
        var nic = NetworkInterface.GetAllNetworkInterfaces()
         .FirstOrDefault(n =>
             (n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
              n.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
             n.GetPhysicalAddress().ToString().Length > 0 &&
             !n.Description.ToLower().Contains("virtual") &&
             !n.Description.ToLower().Contains("hyper-v") &&
             !n.Description.ToLower().Contains("bluetooth"));

        return nic?.GetPhysicalAddress().ToString() ?? "UNKNOWN";
    }

    static string GetCpuId()
    {
        using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
        {
            foreach (var obj in searcher.Get())
            {
                return obj["ProcessorId"]?.ToString() ?? "UNKNOWN";
            }
        }
        return "UNKNOWN";
    }

    static string ComputeSha256Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}