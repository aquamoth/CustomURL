using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CustomURL
{
	static class Program
	{
		static void Main(string[] args)
		{
			try
			{
				var arguments = decodeSwitches(args);

				if (arguments.ShowHelp || arguments.Filename == null)
				{
					displayHelp();
				}
				else
				{
					startProcess(arguments.Filename, arguments.Args, arguments.Url);
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.ToString());
			}
		}

		private static void displayHelp()
		{
			var sb = new StringBuilder();
			sb.AppendLine("CustomURL.exe");
			sb.AppendLine("Copyright 2015 Mattias Åslund <mattias@trustfall.se>");
			sb.AppendLine();
			sb.AppendLine("CustomURL --url [protocol]://[url] [program path] [arguments]");
			sb.AppendLine();
			sb.AppendLine("   where program path and arguments can contain {0} for protocol and {1} for url.");
			sb.AppendLine();
			sb.AppendLine("Splits protocol and url parts from uri and executes another application.");
			sb.AppendLine("This converter is mainly useful when registering a custom url handler for browser protocols, such as rdp://");
			sb.AppendLine();
			sb.AppendLine("Example:");
			sb.AppendLine("   CustomURL --url rdp://10.0.0.1 mstsc.exe /v:{1}");
			sb.AppendLine();
			MessageBox.Show(sb.ToString(), "CustomURL.exe Help");
		}

		private static void startProcess(string filename, string[] arguments, string url)
		{
			var urlParts = split(url);
			var expandedFilename = expand(filename, urlParts);
			var expandedArguments = string.Format(string.Join(" ", arguments), urlParts.Protocol, urlParts.Url);

			Trace.TraceInformation("Running {0} {1}", expandedFilename, expandedArguments);
			Process.Start(expandedFilename, expandedArguments);
		}

		private static Arguments decodeSwitches(string[] args)
		{
			var arguments = new Arguments();

			int index = 0;
			while (index < args.Length)
			{
				var arg = args[index];
				if (!arg.StartsWith("-"))
					break;

				switch (arg)
				{
					case "--url":
						index++;
						if (index >= args.Length)
							throw new ArgumentException("Argument after --url is missing.");
						arguments.Url = args[index];
						Trace.TraceInformation("Url to strip: {0}", arguments.Url);
						break;
					case "--help":
						arguments.ShowHelp = true;
						Trace.TraceInformation("Displaying help.");
						break;
					default:
						throw new ArgumentException("Unsupported switch: " + arg);
				}
				index++;
			}

			if (index < args.Length)
			{
				arguments.Filename = args[index];
				index++;
			}

			arguments.Args = new string[args.Length - index];
			Array.Copy(args, index, arguments.Args, 0, arguments.Args.Length);

			return arguments;
		}

		private static string expand(string filename, UrlParts urlParts)
		{
			var expandedFilename = string.Format(Environment.ExpandEnvironmentVariables(filename), urlParts.Protocol, urlParts.Url);
			Trace.TraceInformation("Filename: {0}", expandedFilename);
			return expandedFilename;
		}

		private static UrlParts split(string urlArgument)
		{
			if (urlArgument == null)
				return new UrlParts { Protocol = "", Url = "" };

			var expression = @"^(\w+)://(.+?)(?:/(.*))?$";
			var matches = Regex.Match(urlArgument, expression);
			if (matches.Groups.Count != 4)
				throw new ArgumentException(string.Format("Url does not match '{0}'.", expression));
			
			var protocol = matches.Groups[1].Captures[0].Value;
			Trace.TraceInformation("Protocol: {0}", protocol);
	
			var url = matches.Groups[2].Captures[0].Value;
			Trace.TraceInformation("Url: {0}", url);

			return new UrlParts { Protocol = protocol, Url = url };
		}
	}

	class Arguments
	{
		public bool ShowHelp { get; set; }
		public string Url { get; set; }
		public string Filename { get; set; }
		public string[] Args { get; set; }
	}

	class UrlParts
	{
		public string Protocol { get; set; }
		public string Url { get; set; }
	}
}
