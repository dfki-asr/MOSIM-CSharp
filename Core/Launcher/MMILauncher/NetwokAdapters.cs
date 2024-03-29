﻿// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Adam Klodowski

using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Communication
{

    public class TInterface
    {
        public string Name;
        public string IP;

        public TInterface()
        {

        }

        public TInterface(string name, string ip)
        {
            this.Name = name;
            this.IP = ip;
        }
    }

    public class NetworkAdapters
    {
        public List<TInterface> AvailableIp;
        public int currentIp = -1;
        public List<MIPAddress> OccupiedIPEndPointList;

        public NetworkAdapters()
        {
            AvailableIp = new List<TInterface>();
            OccupiedIPEndPointList = new List<MIPAddress>();
            GetNetworkAdapters();
        }

        public void GetNetworkAdapters()
        {
            AvailableIp.Clear();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    foreach (UnicastIPAddressInformation addr in properties.UnicastAddresses)
                        if (addr.Address.GetAddressBytes().Length == 4)
                            AvailableIp.Add(new TInterface(adapter.Name, addr.Address.ToString()));
                }
            }
        }

        public bool updatedCurrentIp(string adapter, string address)
        {
            currentIp = -1;
            for (int i = 0; i < AvailableIp.Count; i++)
                if ((AvailableIp[i].Name == adapter) || (AvailableIp[i].IP == address))
                    currentIp = i;
            if (currentIp == -1)
                for (int i = 0; i < AvailableIp.Count; i++)
                    if (AvailableIp[i].IP == "127.0.0.1")
                        currentIp = i;
            return !((AvailableIp[currentIp].IP == address) && (AvailableIp[currentIp].Name == adapter));
        }

        public int getNextAvailablePort(int port, int maxPort)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            bool isAvailable;

            for (port+=1; port < maxPort; port++)
            {
              isAvailable = true;
                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == port)
                    {
                        isAvailable = false;
                        break;
                    }
                }
                if (isAvailable)
                return port;
            }
            return -1; //no ports available within the port to maxPort range
        }

        public bool IsIpPortAvailable(string ip, int port)
        {
            if (port < 0)
                return false;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (port == tcpi.LocalEndPoint.Port && ip == tcpi.LocalEndPoint.Address.ToString())
                    return false;
            }
            foreach (var occupiedEndPoint in OccupiedIPEndPointList)
            {
                if (ip == occupiedEndPoint.Address && port == occupiedEndPoint.Port)
                    return false;
            }
            foreach (var aIp in AvailableIp)
            {
                if (ip == aIp.IP)
                    return true;
            }
            return false;
        }

        public bool ReserveIpPort(string ip, int port)
        {
            if (!IsIpPortAvailable(ip, port))
                return false;
            OccupiedIPEndPointList.Add(new MIPAddress(ip, port));
            return true;
        }

        public int ReserveNextAvailablePort(string ip, int minPort, int maxPort)
        {
            for (int port = minPort; port <= maxPort; port++)
            {
                if (ReserveIpPort(ip, port))
                    return port;
            }
            return -1;
        }

    }

}