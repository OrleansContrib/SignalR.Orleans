using System.Data.HashFunction.xxHash;
using System.Text;

namespace SignalR.Orleans.Core;

internal static class StringExtensions
{
	private static readonly IxxHash HashFunction = xxHashFactory.Instance.Create();

	/// <summary>
	/// Get a consistent hashcode number based on a string.
	/// </summary>
	/// <param name="text">Value to generate hashcode for.</param>
	public static int ToHashCode(this string text)
	{
		var value = HashFunction.ComputeHash(Encoding.UTF8.GetBytes(text));
		return BitConverter.ToInt32(value.Hash, 0);
	}

	/// <summary>
	/// Get a consistent number between 0-<paramref name="maxValue"/> based on a string value.
	/// </summary>
	/// <param name="value">String value to generate consistent partition index.</param>
	/// <param name="maxValue">The exclusive upper bound of the number returned e.g. 12 (0-11)</param>
	public static int ToPartitionIndex(this string value, int maxValue)
	{
		var hashCode = value.ToHashCode();
		return Math.Abs(hashCode % maxValue);
	}
}
