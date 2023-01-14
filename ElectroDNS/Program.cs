using System.Management;
using System.Net;
using System.Net.NetworkInformation;

namespace ElectroDNS
{
    internal static class Program
    {
        const string electroPreferedDns = "78.157.42.100";
        const string electroAlternateDns = "78.157.42.101";

        static string Response { get; set; } = string.Empty;

        static void Main(string[] args)
        {
            Console.Title = "Auto ElectroDNS Changer";
            Console.WriteLine("Auto ElectroDNS Changer by Pedram Elmi (pedram.elmi@gmail.com)");
            Console.WriteLine("Electro Website: https://electrotm.org");
            Console.WriteLine("Github Repository: ")

            var network = TryGetActiveEthernetOrWifiNetworkInterface();
            if (network == null)
            {
                return;
            }

            PrintNetworkInfo(network);
            Console.WriteLine("--------------------");

            Switch(network);

            Console.WriteLine("Press Any Key to Exit...");
            Console.ReadKey();
        }

        static NetworkInterface? Switch(NetworkInterface? network)
        {
            if (network == null)
            {
                return null;
            }

            var dns = new List<string>();
            foreach(var item in network.GetIPProperties().DnsAddresses)
            {
                dns.Add(item.ToString());
            }

            if(dns.Contains(electroPreferedDns) && dns.Contains(electroAlternateDns))
            {
                Console.WriteLine("Switched to Automatic DNS");
                UnsetDNS(network);
                network = TryGetActiveEthernetOrWifiNetworkInterface();
                if(network == null)
                {
                    return null;
                }
                PrintNetworkInfo(network);
            }
            else
            {
                Console.WriteLine("Switched to Electro DNS");
                SetDNS(network, electroPreferedDns, electroAlternateDns);
                network = TryGetActiveEthernetOrWifiNetworkInterface();
                if(network == null)
                {
                    return null;
                }
                PrintNetworkInfo(network);
            }

            return network;
        }

        static NetworkInterface? TryGetActiveEthernetOrWifiNetworkInterface()
        {
            var network = GetActiveEthernetOrWifiNetworkInterface();
            if(network is null)
            {
                Console.WriteLine("Could not find any Active Network. Try Again? (Y/N)");
                Response = Console.ReadKey().Key.ToString();
                if(Response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    return TryGetActiveEthernetOrWifiNetworkInterface();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return network;
            }
        }

        static void PrintNetworkInfo(NetworkInterface network)
        {
            var currentDns = network.GetIPProperties().DnsAddresses;
            Console.WriteLine("Active Network:");
            Console.WriteLine($"Id: {network.Id}");
            Console.WriteLine($"Name: {network.Name}");
            Console.WriteLine($"Name: {network.Description}");
            Console.WriteLine("DNS Addresses:");
            foreach(var dns in currentDns)
            {
                Console.WriteLine($"\t{dns}");
            }
        }

        static NetworkInterface? GetActiveEthernetOrWifiNetworkInterface()
        {
            var network = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                a => a.OperationalStatus == OperationalStatus.Up &&
                (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork"));

            return network;
        }

        static void SetDNS(NetworkInterface networkInterface, string preferedDns, string? alternateDns = null)
        {
            List<string> Dns = new List<string>()
            {
                preferedDns,
            };

            if(alternateDns != null)
            {
                Dns.Add(alternateDns);
            }

            if(networkInterface == null)
                return;

            var objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach(ManagementObject objMO in objMOC)
            {
                if((bool)objMO["IPEnabled"])
                {
                    if(objMO["Description"].ToString().Equals(networkInterface.Description))
                    {
                        ManagementBaseObject objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if(objdns != null)
                        {
                            objdns["DNSServerSearchOrder"] = Dns.ToArray();
                            objMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                        }
                    }
                }
            }
        }

        static void UnsetDNS(NetworkInterface networkInterface)
        {
            if(networkInterface == null)
            {
                return;
            }

            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach(ManagementObject objMO in objMOC)
            {
                if((bool)objMO["IPEnabled"])
                {
                    if(objMO["Description"].ToString().Equals(networkInterface.Description))
                    {
                        ManagementBaseObject objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if(objdns != null)
                        {
                            objdns["DNSServerSearchOrder"] = null;
                            objMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                        }
                    }
                }
            }
        }
    }
}