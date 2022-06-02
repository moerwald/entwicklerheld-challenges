<Query Kind="Statements" />



//HappySubnettingCsharp.InSameSubnet("192.168.0.10", "192.168.0.210", "255.255.255.0").Dump();
//HappySubnettingCsharp.CalculateSubnetInfo("192.168.0.129/25").Dump();
//HappySubnettingCsharp.CalculateSubnetInfo("192.168.0.1/25").Dump();
//HappySubnettingCsharp.CalculateEqualSizedSubnets("192.168.0.10/24", 2).Dump();
//HappySubnettingCsharp.CalculateEqualSizedSubnets("192.168.0.0/24", 2).Dump();
//HappySubnettingCsharp.CalculateEqualSizedSubnets("10.128.0.0/9", 7).Dump();
HappySubnettingCsharp.CalculateVariableSizedSubnets("192.168.0.0/23", new int[] {30, 15, 40, 40, 100, 10, 20 }).Dump();

public static class HappySubnettingCsharp
{
	public static bool InSameSubnet(string ipV4Addr1String, string ipV4Addr2String, string subnetMaskString)
	{
		var ip1 = IpAddressHelper.ToUInt(ipV4Addr1String);
		var ip2 = IpAddressHelper.ToUInt(ipV4Addr2String);
		var subnet = IpAddressHelper.ToUInt(subnetMaskString);

		var subnet1 = GetSubnetFromIp(ip1, subnet);
		var subnet2 = GetSubnetFromIp(ip2, subnet);

		return subnet1 == subnet2;
	}

	private static uint GetSubnetFromIp(uint ip, uint subnet) => ip & subnet;


	public static SubnetInfo CalculateSubnetInfo(string ipV4CIDRString)
	{
		// scenario 2
		ReadOnlySpan<char> nrOfSubnetBits = ipV4CIDRString.AsSpan();
		var slashIndex = nrOfSubnetBits.IndexOf('/');
		var bits = nrOfSubnetBits[(slashIndex + 1)..];
		var subnetBits = int.Parse(bits.ToString());
		var ipAddress = nrOfSubnetBits[0..slashIndex];

		var subNetMaskAsString = IpAddressHelper.FromSubnetBits(subnetBits);
		var netIdAsString = IpAddressHelper.GetNetIdString(ipAddress.ToString(), subNetMaskAsString);
		var firstHost = IpAddressHelper.GetHostIpAddressString(IpAddressHelper.ToUInt(netIdAsString), 1);

		var availableHosts = (uint)Math.Pow(2, 32 - subnetBits);
		var lastHost = IpAddressHelper.GetHostIpAddressString(IpAddressHelper.ToUInt(netIdAsString), availableHosts - 2);
		var broadCast = IpAddressHelper.GetHostIpAddressString(IpAddressHelper.ToUInt(netIdAsString), availableHosts - 1);

		return new SubnetInfo
		{
			NetId = netIdAsString,
			First = firstHost,
			Last = lastHost,
			Broadcast = broadCast,
			Subnetmask = subNetMaskAsString,
			Available = (int)availableHosts - 2 // Broadcast IP + GW IP
		};
	}


	public static List<SubnetInfo> CalculateEqualSizedSubnets(string ipV4CIDRString, int numberOfSubnets)
	{

		var (ipAddress, subnetBits) = IpAddressHelper.GetIpAndSubNetBits(ipV4CIDRString);
		var additionalSubNetBits = GetBitsForNumber(numberOfSubnets - 1);
		var newCidr = ipAddress.ToString() + "/" + (subnetBits + additionalSubNetBits).ToString();

		(ipAddress, subnetBits) = IpAddressHelper.GetIpAndSubNetBits(newCidr);

		var subNetMaskAsString = IpAddressHelper.FromSubnetBits(subnetBits);
		var netIdBytes = IpAddressHelper.GetNetIdBytes(ipAddress, subNetMaskAsString);

		var result = new List<SubnetInfo>();
		var hostBits = (32 - subnetBits);
		for (uint i = 0; i < numberOfSubnets; i++)
		{
			// Increment subnet ID
			var copyNetIdBytes = netIdBytes | i << hostBits;

			// Create new CIDR IP
			var localIpSubNetAddress = IpAddressHelper.ToIpAddressString(copyNetIdBytes) + "/" + (subnetBits).ToString();

			var subnetInfo = CalculateSubnetInfo(localIpSubNetAddress);
			subnetInfo.Number = (int)i + 1;

			result.Add(subnetInfo);
		}

		return result;

	}

	private static int GetBitsForNumber(int number)
	{
		var cnt = 0;
		while (number > 0)
		{
			cnt++;
			number = number >> 1;
		}

		return cnt;
	}

	public static List<SubnetInfo> CalculateVariableSizedSubnets(string ipV4CIDRString, int[] numberOfUserHosts)
	{
		// scenario 4
		// Your implementation should go here.

		/*
			
			for (var i = 0; i< numberOfUserHosts; i++){
		
			nrOfClientBits = GetNumberOfNeededClientBits(...);
			newCidr = CreateNewIpV4 (ipV4CIDRString, nrOfClientBits);
			netIdAsInt = GetNetId(newCidr)
			
			availableClients = 2^nrOfClientBits - 1
			
			first = netIdAsInt | 1;
			broadcast = netIdAsInt | availableClients;
			last = netIdAsInt | availableClients - 1;
			
			newNetIp = netIdAsInt | (availableClients + 1);
			ipV4CIDRSTring = GetIpFrom(newNEtIP);
			
			}
		*/

		var result = new List<SubnetInfo>();

		var ipAddressCidr = ipV4CIDRString;
		var index = 0;

		foreach (var numberOfRequestedClients in numberOfUserHosts.OrderByDescending(ouh => ouh))
		{
			var nrOfClientBits = GetBitsForNumber(numberOfRequestedClients + 2 /* GW + Broadcast*/ );

			// Create CDIR string with dynamic subnet bits
			var (ipAddress, subnetBits) = IpAddressHelper.GetIpAndSubNetBits(ipAddressCidr);
			var dynamicSubNetBits = 32 - subnetBits - nrOfClientBits;
			var newSubnetBits = dynamicSubNetBits + subnetBits;
			var newCidrIpAddress = ipAddress.ToString() + "/" + newSubnetBits.ToString();

			var subNetMaskAsString = IpAddressHelper.FromSubnetBits(newSubnetBits);
			var netIdBytes = IpAddressHelper.GetNetIdBytes(ipAddress, subNetMaskAsString);

			var avaiableClients = (uint)Math.Pow(2, nrOfClientBits) - 1;

			var firstClientAddress = IpAddressHelper.ToIpAddressString(netIdBytes | 1);
			var broadCastAddress = IpAddressHelper.ToIpAddressString(netIdBytes | avaiableClients);
			var lastClientAddress = IpAddressHelper.ToIpAddressString(netIdBytes | avaiableClients - 1);

			var netIdForNextSegmet = netIdBytes + avaiableClients + 1;
			var netIdAddress = IpAddressHelper.ToIpAddressString(netIdForNextSegmet);
			ipAddressCidr = netIdAddress  + "/" + subnetBits.ToString();

			index++;
			var subNetInfo = new SubnetInfo
			{
				Number = index,
				Available = (int)avaiableClients - 1,
				Requested = numberOfRequestedClients,
				First = firstClientAddress,
				Last = lastClientAddress,
				Broadcast = broadCastAddress,
				Subnetmask = IpAddressHelper.FromSubnetBits(newSubnetBits),
				NetId = ipAddress 
			};
			result.Add(subNetInfo);

		}

		return result;

	}
}

public static class IpAddressHelper
{
	public static uint ToUInt(string ipString)
	{
		uint ip = 0x00_00_00_00;
		var octets = ipString.Split('.');
		int octectIndex = 3;
		foreach (var octet in octets)
		{
			var octetParsed = byte.Parse(octet);

			ip |= (uint)octetParsed << (octectIndex * 8);
			octectIndex--;
		}

		return ip;
	}

	public static string GetHostIpAddressString(uint netId, uint hostId) =>
		ToIpAddressString(netId | hostId);

	public static uint GetNetIdBytes(string ipAddress, string subNetMaskAsString) =>
		(ToUInt(ipAddress) & ToUInt(subNetMaskAsString));

	public static string GetNetIdString(string ipAddress, string subNetMaskAsString) =>
		ToIpAddressString(GetNetIdBytes(ipAddress, subNetMaskAsString));

	public static string FromSubnetBits(int subnetBits)
	{
		var hostBitsValue = (uint)Math.Pow(2, 32 - subnetBits) - 1; // Adapt to null based, e.g. 0-255 
		var hostBitsZeroedOut = uint.MaxValue - hostBitsValue;

		return ToIpAddressString(uint.MaxValue & hostBitsZeroedOut);
	}

	public static string ToIpAddressString(uint ip)
	{
		var sb = new StringBuilder();
		var dots = 0;

		var bytes = BitConverter.GetBytes(ip);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}

		foreach (var byt in bytes)
		{
			sb.Append(byt.ToString());
			if (dots < 3)
			{
				sb.Append(".");
			}
			dots++;
		}

		return sb.ToString();
	}

	public static (string IpAddress, int SubNetBits) GetIpAndSubNetBits(string ipV4CIDRString)
	{

		ReadOnlySpan<char> nrOfSubnetBits = ipV4CIDRString.AsSpan();
		var slashIndex = nrOfSubnetBits.IndexOf('/');
		var bits = nrOfSubnetBits[(slashIndex + 1)..];
		var subnetBits = int.Parse(bits.ToString());
		var ipAddress = nrOfSubnetBits[0..slashIndex];

		return (ipAddress.ToString(), subnetBits);


	}

}

public class SubnetInfo
{
	public string NetId;
	public string First;
	public string Last;
	public string Broadcast;
	public string Subnetmask;
	public int Available;
	public int Number;
	public int Requested;

	public override bool Equals(object obj)
	{
		if (null == obj || !(obj is SubnetInfo info))
			return false;

		return ToString().Equals(info.ToString());
	}

	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}

	public override string ToString()
	{
		return
			(Number > 0 ? "Number: " + Number + "\n" : "")
			+ "NetId: " + (null != NetId ? NetId : "null") + "\n"
			+ "First: " + (null != First ? First : "null") + "\n"
			+ "Last: " + (null != Last ? Last : "null") + "\n"
			+ "Broadcast: " + (null != Broadcast ? Broadcast : "null") + "\n"
			+ (Available > 0 ? "Available: " + Available + "\n" : "")
			+ (Requested > 0 ? "Requested: " + Requested + "\n" : "");
	}
}