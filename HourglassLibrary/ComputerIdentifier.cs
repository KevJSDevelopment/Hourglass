using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

public class ComputerIdentifier
{
    public static string GetUniqueIdentifier()
    {
        string identifier = string.Empty;
        identifier += GetCPUId();
        identifier += GetBIOSId();
        identifier += GetDiskId();
        identifier += GetBaseboardId();

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
            return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 32);
        }
    }

    private static string GetCPUId()
    {
        string cpuInfo = string.Empty;
        ManagementClass mc = new ManagementClass("win32_processor");
        ManagementObjectCollection moc = mc.GetInstances();
        foreach (ManagementObject mo in moc)
        {
            cpuInfo = mo.Properties["processorID"].Value.ToString();
            break;
        }
        return cpuInfo;
    }

    private static string GetBIOSId()
    {
        string biosInfo = string.Empty;
        ManagementClass mc = new ManagementClass("win32_bios");
        ManagementObjectCollection moc = mc.GetInstances();
        foreach (ManagementObject mo in moc)
        {
            biosInfo = (string)mo["Manufacturer"];
            biosInfo += (string)mo["SMBIOSBIOSVersion"];
            biosInfo += (string)mo["IdentificationCode"];
            biosInfo += (string)mo["SerialNumber"];
            biosInfo += (string)mo["ReleaseDate"];
            biosInfo += (string)mo["Version"];
            break;
        }
        return biosInfo;
    }

    private static string GetDiskId()
    {
        string diskInfo = string.Empty;
        ManagementClass mc = new ManagementClass("Win32_DiskDrive");
        ManagementObjectCollection moc = mc.GetInstances();
        foreach (ManagementObject mo in moc)
        {
            diskInfo = (string)mo.Properties["Model"].Value;
            break;
        }
        return diskInfo;
    }

    private static string GetBaseboardId()
    {
        string baseboardInfo = string.Empty;
        ManagementClass mc = new ManagementClass("Win32_BaseBoard");
        ManagementObjectCollection moc = mc.GetInstances();
        foreach (ManagementObject mo in moc)
        {
            baseboardInfo = (string)mo.Properties["Manufacturer"].Value;
            baseboardInfo += (string)mo.Properties["Product"].Value;
            baseboardInfo += (string)mo.Properties["SerialNumber"].Value;
            break;
        }
        return baseboardInfo;
    }
}