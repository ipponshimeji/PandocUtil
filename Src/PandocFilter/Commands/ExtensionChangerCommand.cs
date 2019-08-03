﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Utf8Json;
using PandocUtil.PandocFilter.Filters;

namespace PandocUtil.PandocFilter.Commands {
	public class ExtensionChangerCommand: FilteringCommand {
		#region data

		protected string FromFilePath { get; set; } = null;

		protected string ToFilePath { get; set; } = null;

		protected readonly Dictionary<string, string> ExtensionMap = new Dictionary<string, string>();

		protected bool RebaseOtherRelativeLinks { get; private set; } = false;

		#endregion


		#region constructors

		public ExtensionChangerCommand() {
		}

		#endregion


		#region overridables

		protected override void ProcessNormalArgument(Argument arg) {
			// argument checks
			Debug.Assert(arg.IsOption == false);

			switch (arg.Index) {
				case 0:
					this.FromFilePath = arg.Value;
					break;
				case 1:
					this.ToFilePath = arg.Value;
					break;
			}
		}

		protected override void ProcessOption(Argument arg) {
			// argument checks
			Debug.Assert(arg.IsOption);

			string name = arg.Name;
			if (AreSameOptionNames(name, "map")) {
				(string from, string to) SplitExtensions(string val) {
					int index = val.IndexOf(':');
					if (index < 0) {
						throw new ArgumentException("Its form must be \"<from ext>:<to ext>\"");    // TODO: message
					}
					string f = val.Substring(0, index);
					string t = val.Substring(index + 1);

					return (f, t);
				}

				(string from, string to) = SplitExtensions(arg.Value);
				this.ExtensionMap.Add(from, to);
			} else if (AreSameOptionNames(name, "RebaseOtherRelativeLinks")) {
				this.RebaseOtherRelativeLinks = true;
			}
		}

		protected override void OnExecuting() {
			// state checks
			if (string.IsNullOrEmpty(this.FromFilePath)) {
				throw new InvalidOperationException("The indispensable argument 'InputFilePath' is missing.");
			}
			if (string.IsNullOrEmpty(this.ToFilePath)) {
				throw new InvalidOperationException("The indispensable argument 'OutputFilePath' is missing.");
			}
		}

		protected override void Execute(Stream inputStream, Stream outputStream) {
			ExtensionChangingFilter filter = new ExtensionChangingFilter(this.FromFilePath, this.ToFilePath, false, this.ExtensionMap);

			// read input AST
			Dictionary<string, object> ast = JsonSerializer.Deserialize<Dictionary<string, object>>(inputStream);

			// modify the AST
			filter.Modify(ast);

			// write output AST
			JsonSerializer.Serialize(outputStream, ast);
		}

		protected override int OnExecuted(Exception error) {
			Console.Error.WriteLine(error.Message);
			return (error == null) ? SuccessExitCode : GeneralErrorExitCode;
		}

		#endregion
	}
}
