using UnityEngine;

namespace Permaverse.AO
{
	public class ANSIConverter : MonoBehaviour
	{
		// Function to convert ANSI escape codes to TextMeshPro rich text tags
		public static string ConvertANSIToTextMeshPro(string input, bool keepColors, bool removeNonPrintableChar = true)
		{
			string output = input;

			// Define ANSI escape codes and their corresponding TextMeshPro rich text tags
			string[] ansiCodes = { "[30m", "[31m", "[32m", "[33m", "[34m", "[35m", "[36m", "[37m", "[90m", "[0m" };
			string[] tmproTags = { "<color=#000000>", "<color=#FF0000>", "<color=#00FF00>", "<color=#FFFF00>", "<color=#0000FF>", "<color=#FF00FF>", "<color=#00FFFF>", "<color=#FFFFFF>", "<color=#808080>", "</color>" };

			// Replace ANSI escape codes with TextMeshPro rich text tags
			for (int i = 0; i < ansiCodes.Length; i++)
			{
				if (keepColors)
				{
					output = output.Replace(ansiCodes[i], tmproTags[i]);
				}
				else
				{
					output = output.Replace(ansiCodes[i], "");
				}
			}

			if (removeNonPrintableChar)
			{
				output = RemoveNonPrintableCharacters(output);
			}

			return output;
		}

		// Function to remove non-printable characters or escape sequences
		private static string RemoveNonPrintableCharacters(string input)
		{
			// Define regex pattern to match non-printable characters or escape sequences
			string pattern = @"[\x00-\x1F\x7F-\x9F]|\[\d*?[GJ]|\[2K";

			// Remove non-printable characters or escape sequences from the input string
			return System.Text.RegularExpressions.Regex.Replace(input, pattern, "");
		}

		// Example usage:
		//	string ansiText = "[31mRed text[0m and [36mCyan text[0m";
		//	string tmproText = ConvertANSIToTextMeshPro(ansiText);
		//	Debug.Log(tmproText); // Output the converted text to Unity console
	}
}